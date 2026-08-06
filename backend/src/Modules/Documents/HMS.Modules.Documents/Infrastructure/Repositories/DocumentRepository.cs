using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Documents.Infrastructure.Repositories;

internal class DocumentRepository : IDocumentRepository
{
    private readonly DocumentsDbContext _dbContext;

    public DocumentRepository(DocumentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken)
        => await _dbContext.Documents.AddAsync(document, cancellationToken);

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Document> Items, int TotalCount)> GetPagedAsync(DocumentListQuery query, CancellationToken cancellationToken)
    {
        var documents = Filter(_dbContext.Documents.AsQueryable(), query);

        var totalCount = await documents.CountAsync(cancellationToken);

        var items = await documents
            .OrderByDescending(d => d.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<DocumentSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        return new DocumentSummaryResponse
        {
            Total = await _dbContext.Documents.CountAsync(cancellationToken),
            UploadedToday = await _dbContext.Documents.CountAsync(d => d.CreatedAt >= today, cancellationToken),
            Archived = await _dbContext.Documents.CountAsync(d => d.IsArchived, cancellationToken),
            StorageUsedBytes = await _dbContext.Documents.SumAsync(d => (long?)d.SizeBytes, cancellationToken) ?? 0,
        };
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Document> Filter(IQueryable<Document> documents, DocumentListQuery query)
    {
        if (query.OwnerType is not null)
        {
            documents = documents.Where(d => d.OwnerType == query.OwnerType);
        }

        if (query.OwnerId is not null)
        {
            documents = documents.Where(d => d.OwnerId == query.OwnerId);
        }

        if (query.DocumentType is not null)
        {
            documents = documents.Where(d => d.DocumentType == query.DocumentType);
        }

        if (query.UploadedByUserId is not null)
        {
            documents = documents.Where(d => d.UploadedByUserId == query.UploadedByUserId);
        }

        if (query.DateFrom is not null)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            documents = documents.Where(d => d.CreatedAt >= from);
        }

        if (query.DateTo is not null)
        {
            var to = query.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            documents = documents.Where(d => d.CreatedAt <= to);
        }

        documents = query.Status switch
        {
            DocumentStatusFilter.Active => documents.Where(d => !d.IsArchived),
            DocumentStatusFilter.Archived => documents.Where(d => d.IsArchived),
            _ => documents,
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            documents = documents.Where(d => EF.Functions.ILike(d.OriginalFileName, term));
        }

        return documents;
    }
}
