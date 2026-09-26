using System.Security.Cryptography;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Application.Indexing;
using HMS.Modules.Documents.Application.Mapping;
using HMS.Modules.Documents.Application.Security;
using HMS.Modules.Documents.Application.Validation;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Documents.Application;

/// <summary>
/// Orchestrates every Documents use case (US-1, US-3 through US-8). Mirrors
/// HMS.Modules.Patients.Application.PatientService's shape: file constraints are checked
/// directly here rather than via FluentValidation (matching PatientService.UploadPhotoAsync),
/// since IFormFile-derived stream/length/content-type inputs aren't naturally expressed as
/// FluentValidation rules on a DTO.
/// </summary>
internal class DocumentService : IDocumentService
{
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    ];

    private const int MaxBackfillBatch = 500;

    private readonly IDocumentRepository _repository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IDocumentIndexer _indexer;
    private readonly IDocumentFileStorage _fileStorage;
    private readonly IDocumentAccessPolicy _accessPolicy;
    private readonly IDocumentScanQueue _scanQueue;
    private readonly IReadOnlyDictionary<DocumentOwnerType, IDocumentOwnerExistenceChecker> _ownerCheckers;
    private readonly ITenantContext _tenantContext;
    private readonly long _maxFileSizeBytes;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository repository,
        IDocumentChunkRepository chunkRepository,
        IDocumentIndexer indexer,
        IDocumentFileStorage fileStorage,
        IDocumentAccessPolicy accessPolicy,
        IDocumentScanQueue scanQueue,
        IEnumerable<IDocumentOwnerExistenceChecker> ownerCheckers,
        ITenantContext tenantContext,
        IConfiguration configuration,
        ILogger<DocumentService> logger)
    {
        _repository = repository;
        _chunkRepository = chunkRepository;
        _indexer = indexer;
        _fileStorage = fileStorage;
        _accessPolicy = accessPolicy;
        _scanQueue = scanQueue;
        _ownerCheckers = ownerCheckers.ToDictionary(c => c.OwnerType);
        _tenantContext = tenantContext;
        _maxFileSizeBytes = configuration.GetValue("Documents:MaxFileSizeMb", 10) * 1024L * 1024L;
        _logger = logger;
    }

    public async Task<Result<DocumentResponse>> UploadAsync(UploadDocumentRequest request, Stream content, string fileName, string contentType, long length, DocumentActor actor, CancellationToken cancellationToken)
    {
        if (!_accessPolicy.CanWrite(actor, request.OwnerType))
        {
            return Result<DocumentResponse>.Failure(DocumentErrorCodes.Forbidden, "You do not have permission to upload documents for this record type.");
        }

        if (length <= 0 || length > _maxFileSizeBytes)
        {
            return Result<DocumentResponse>.Failure(DocumentErrorCodes.InvalidFile, $"File must be between 1 byte and {_maxFileSizeBytes / (1024 * 1024)}MB.");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            return Result<DocumentResponse>.Failure(DocumentErrorCodes.InvalidFile, "Unsupported file format.");
        }

        if (!await FileSignatureValidator.MatchesDeclaredContentTypeAsync(content, contentType, cancellationToken))
        {
            return Result<DocumentResponse>.Failure(DocumentErrorCodes.InvalidFile, "The file's content does not match its declared type.");
        }

        if (_ownerCheckers.TryGetValue(request.OwnerType, out var checker))
        {
            if (!await checker.ExistsAsync(request.OwnerId, cancellationToken))
            {
                return Result<DocumentResponse>.Failure(DocumentErrorCodes.OwnerNotFound, $"No {request.OwnerType} record was found for id '{request.OwnerId}'.");
            }
        }
        else
        {
            // See IDocumentOwnerExistenceChecker's remarks: this owner type has no backend
            // module yet, so existence can't be validated. Logged, not silently accepted.
            _logger.LogWarning("No owner-existence checker registered for {OwnerType}; upload against owner {OwnerId} was not existence-validated.", request.OwnerType, request.OwnerId);
        }

        var documentId = Guid.CreateVersion7();
        var saved = await _fileStorage.SaveAsync(documentId, fileName, content, cancellationToken);

        var document = Document.Create(
            request.OwnerType,
            request.OwnerId,
            request.DocumentType,
            request.Classification ?? DocumentClassification.Internal,
            saved.StorageKey,
            fileName,
            contentType,
            length,
            saved.ChecksumSha256,
            actor.UserId,
            request.ExpiryDate);

        await _repository.AddAsync(document, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // The scan queue is drained by a background service with no HTTP request of its own —
        // it can't resolve a tenant the way this call's own request scope just did, so the
        // tenant it needs to reach the right database is captured here and carried through the
        // queue item instead (see ScanQueueItem's own doc comment for what breaks without this).
        if (!_tenantContext.IsResolved || _tenantContext.TenantId is null || _tenantContext.ConnectionString is null)
        {
            throw new InvalidOperationException("DocumentService.UploadAsync was called without a tenant having been established for this request.");
        }
        await _scanQueue.EnqueueAsync(new ScanQueueItem(document.Id, _tenantContext.TenantId.Value, _tenantContext.ConnectionString), cancellationToken);

        _logger.LogInformation("Document {DocumentId} uploaded for {OwnerType} {OwnerId} by {UserId}", document.Id, document.OwnerType, document.OwnerId, actor.UserId);

        return Result<DocumentResponse>.Success(document.ToResponse());
    }

    public async Task<Result<DocumentResponse>> GetByIdAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return Result<DocumentResponse>.Failure(DocumentErrorCodes.NotFound, $"Document '{id}' was not found.");
        }

        if (!_accessPolicy.CanRead(actor, document.OwnerType, document.Classification))
        {
            // Deliberately the same NotFound code as a missing row: confirming a document
            // exists for an owner type the caller can't see would itself leak information.
            return Result<DocumentResponse>.Failure(DocumentErrorCodes.NotFound, $"Document '{id}' was not found.");
        }

        return Result<DocumentResponse>.Success(document.ToResponse());
    }

    public async Task<PagedResult<DocumentResponse>> GetPagedAsync(DocumentListQuery query, DocumentActor actor, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);

        var visible = items.Where(d => _accessPolicy.CanRead(actor, d.OwnerType, d.Classification)).ToList();

        // Filtering post-query by the access policy (rather than pushing the role→owner-type
        // map into SQL) keeps DocumentAccessPolicy the single source of truth for "what can
        // this caller see," at the cost of TotalCount/paging being computed pre-filter — an
        // acceptable MVP trade documented in docs/modules/Documents/DocumentManagement.md,
        // since every registered role today maps to a small, fixed owner-type set.
        var mapped = visible.Select(d => d.ToResponse()).ToList();

        return new PagedResult<DocumentResponse>(mapped, query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<DocumentSummaryResponse>> GetSummaryAsync(DocumentActor actor, CancellationToken cancellationToken)
    {
        // Summary is intentionally unfiltered by access policy (matches the original mock's
        // "repository-wide" framing in US-7) — restricting it to only the caller's visible
        // owner types is a reasonable follow-up once dashboards are role-specific.
        var summary = await _repository.GetSummaryAsync(cancellationToken);
        return Result<DocumentSummaryResponse>.Success(summary);
    }

    public async Task<Result<DocumentResponse>> ArchiveAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return Result<DocumentResponse>.Failure(DocumentErrorCodes.NotFound, $"Document '{id}' was not found.");
        }

        if (!_accessPolicy.CanWrite(actor, document.OwnerType))
        {
            return Result<DocumentResponse>.Failure(DocumentErrorCodes.Forbidden, "You do not have permission to archive documents for this record type.");
        }

        document.Archive(actor.UserId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} archived by {UserId}", document.Id, actor.UserId);

        return Result<DocumentResponse>.Success(document.ToResponse());
    }

    public async Task<Result> DeleteAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return Result.Failure(DocumentErrorCodes.NotFound, $"Document '{id}' was not found.");
        }

        if (!_accessPolicy.CanWrite(actor, document.OwnerType))
        {
            return Result.Failure(DocumentErrorCodes.Forbidden, "You do not have permission to delete documents for this record type.");
        }

        // Soft delete only (US-6) — the file on disk is intentionally left in place. A hospital
        // compliance audit needs to be able to answer "what was here before it was deleted,"
        // which a physical file removal would foreclose; a retention/purge job is future work
        // (see docs/modules/Documents/DocumentManagement.md's Future Enhancements).
        document.SoftDelete(actor.UserId);
        await _repository.SaveChangesAsync(cancellationToken);

        // Unlike the document row and file, its RAG chunks are removed outright: a deleted
        // document's text must never come back in a search result.
        await _chunkRepository.DeleteForSourceAsync(DocumentChunkSourceType.Document, document.Id, cancellationToken);

        _logger.LogInformation("Document {DocumentId} soft-deleted by {UserId}", document.Id, actor.UserId);

        return Result.Success();
    }

    public async Task<Result<DocumentContent>> GetContentAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return Result<DocumentContent>.Failure(DocumentErrorCodes.NotFound, $"Document '{id}' was not found.");
        }

        if (!_accessPolicy.CanRead(actor, document.OwnerType, document.Classification))
        {
            return Result<DocumentContent>.Failure(DocumentErrorCodes.NotFound, $"Document '{id}' was not found.");
        }

        if (document.Status != DocumentStatus.Available)
        {
            // Covers both Pending (still being scanned) and Quarantined (failed scan) — the
            // caller sees the same "not available yet" outcome either way; only the KPI
            // summary and admin tooling need to distinguish the two (US-9).
            return Result<DocumentContent>.Failure(DocumentErrorCodes.NotAvailable, "This document's content is not available for download yet.");
        }

        var stream = await _fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);

        _logger.LogInformation("Document {DocumentId} content downloaded by {UserId}", document.Id, actor.UserId);

        return Result<DocumentContent>.Success(new DocumentContent(stream, document.ContentType, document.OriginalFileName));
    }

    public Task<int> GetExpiringDocumentCountAsync(DocumentOwnerType ownerType, int withinDays, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return _repository.GetExpiringCountAsync(ownerType, today, today.AddDays(withinDays), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<DocumentChunkResponse>>> GetChunksAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);
        if (document is null || !_accessPolicy.CanRead(actor, document.OwnerType, document.Classification))
        {
            return Result<IReadOnlyList<DocumentChunkResponse>>.Failure(DocumentErrorCodes.NotFound, $"Document '{id}' was not found.");
        }

        var chunks = await _chunkRepository.GetForSourceAsync(DocumentChunkSourceType.Document, id, cancellationToken);
        return Result<IReadOnlyList<DocumentChunkResponse>>.Success(chunks.Select(c => c.ToResponse()).ToList());
    }

    public async Task<Result<DocumentIndexResponse>> ReindexAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return Result<DocumentIndexResponse>.Failure(DocumentErrorCodes.NotFound, $"Document '{id}' was not found.");
        }

        if (!_accessPolicy.CanWrite(actor, document.OwnerType))
        {
            return Result<DocumentIndexResponse>.Failure(DocumentErrorCodes.Forbidden, "You do not have permission to reindex documents for this record type.");
        }

        var result = await _indexer.IndexAsync(document, cancellationToken);
        return Result<DocumentIndexResponse>.Success(new DocumentIndexResponse
        {
            DocumentId = id,
            ChunkCount = result.ChunkCount,
            SkipReason = result.SkipReason?.ToString(),
        });
    }

    public async Task<Result<DocumentIndexBackfillResponse>> BackfillIndexAsync(DateTime? createdAfter, int limit, DocumentActor actor, CancellationToken cancellationToken)
    {
        if (!_accessPolicy.CanManageIndex(actor))
        {
            return Result<DocumentIndexBackfillResponse>.Failure(DocumentErrorCodes.Forbidden, "Only administrators can run the document index backfill.");
        }

        var batchSize = Math.Clamp(limit, 1, MaxBackfillBatch);
        var pending = await _chunkRepository.GetUnindexedDocumentsAsync(_indexer.IndexableContentTypes, createdAfter, batchSize, cancellationToken);

        int indexed = 0, chunksCreated = 0, skipped = 0, failed = 0;
        foreach (var (documentId, _) in pending)
        {
            try
            {
                var document = await _repository.GetByIdAsync(documentId, cancellationToken);
                if (document is null)
                {
                    skipped++;
                    continue;
                }

                var result = await _indexer.IndexAsync(document, cancellationToken);
                if (result.ChunkCount > 0)
                {
                    indexed++;
                    chunksCreated += result.ChunkCount;
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One unreadable/corrupt file mustn't stop the rest of the batch.
                failed++;
                _logger.LogError(ex, "Backfill failed to index document {DocumentId}.", documentId);
            }
        }

        _logger.LogInformation("Document index backfill by {UserId}: {Processed} processed, {Indexed} indexed, {Skipped} skipped, {Failed} failed.", actor.UserId, pending.Count, indexed, skipped, failed);

        return Result<DocumentIndexBackfillResponse>.Success(new DocumentIndexBackfillResponse
        {
            Processed = pending.Count,
            Indexed = indexed,
            ChunksCreated = chunksCreated,
            Skipped = skipped,
            Failed = failed,
            NextCreatedAfter = pending.Count == batchSize ? pending[^1].CreatedAt : null,
        });
    }
}
