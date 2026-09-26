using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using Microsoft.EntityFrameworkCore;

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
}
