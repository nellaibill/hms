using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace HMS.Modules.Documents.Infrastructure.Repositories;

internal class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly DocumentsDbContext _dbContext;

    public DocumentChunkRepository(DocumentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ReplaceForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        // One transaction: a reader never sees the source half-indexed, and a failed insert
        // leaves the previous chunks in place rather than none.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await DeleteForSourceAsync(sourceType, sourceId, cancellationToken);
        await _dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public Task DeleteForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, CancellationToken cancellationToken)
        => _dbContext.DocumentChunks
            .Where(c => c.SourceType == sourceType && c.SourceId == sourceId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentChunk>> GetForSourceAsync(DocumentChunkSourceType sourceType, Guid sourceId, CancellationToken cancellationToken)
        => await _dbContext.DocumentChunks
            .AsNoTracking()
            .Where(c => c.SourceType == sourceType && c.SourceId == sourceId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(Guid Id, DateTime CreatedAt)>> GetUnindexedDocumentsAsync(IReadOnlyCollection<string> contentTypes, DateTime? createdAfter, int limit, CancellationToken cancellationToken)
    {
        var query = _dbContext.Documents
            .Where(d => d.Status == DocumentStatus.Available && contentTypes.Contains(d.ContentType))
            .Where(d => !_dbContext.DocumentChunks.Any(c => c.SourceType == DocumentChunkSourceType.Document && c.SourceId == d.Id));

        if (createdAfter is not null)
        {
            query = query.Where(d => d.CreatedAt > createdAfter.Value);
        }

        var rows = await query
            .OrderBy(d => d.CreatedAt)
            .Select(d => new { d.Id, d.CreatedAt })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Id, r.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetUnembeddedAsync(string model, int limit, CancellationToken cancellationToken)
        => await _dbContext.DocumentChunks
            .Where(c => c.Embedding == null || c.EmbeddingModel != model)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.ChunkIndex)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ChunkSearchHit>> SearchAsync(Vector query, string model, DocumentOwnerType? ownerType, Guid? ownerId, int limit, CancellationToken cancellationToken)
    {
        var chunks = _dbContext.DocumentChunks.AsNoTracking()
            .Where(c => c.Embedding != null && c.EmbeddingModel == model);

        if (ownerType is not null)
        {
            chunks = chunks.Where(c => c.OwnerType == ownerType.Value);
        }

        if (ownerId is not null)
        {
            chunks = chunks.Where(c => c.OwnerId == ownerId.Value);
        }

        // The join also applies Documents' soft-delete query filter, as a second guard on top
        // of chunks being hard-deleted with their document.
        var rows = await chunks
            .Join(_dbContext.Documents, c => c.SourceId, d => d.Id, (c, d) => new { Chunk = c, d.OriginalFileName })
            .OrderBy(r => r.Chunk.Embedding!.CosineDistance(query))
            .Select(r => new { r.Chunk, r.OriginalFileName, Distance = r.Chunk.Embedding!.CosineDistance(query) })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ChunkSearchHit(r.Chunk, r.OriginalFileName, r.Distance)).ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
