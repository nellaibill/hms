using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Patients.Infrastructure.Repositories;

internal class PatientImportRepository : IPatientImportRepository
{
    private readonly PatientsDbContext _dbContext;

    public PatientImportRepository(PatientsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddBatchAsync(PatientImportBatch batch, CancellationToken cancellationToken)
        => await _dbContext.PatientImportBatches.AddAsync(batch, cancellationToken);

    public Task<PatientImportBatch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.PatientImportBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<PatientImportBatch> Items, int TotalCount)> GetBatchesPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.PatientImportBatches.OrderByDescending(b => b.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddRowsAsync(IEnumerable<PatientImportRow> rows, CancellationToken cancellationToken)
        => await _dbContext.PatientImportRows.AddRangeAsync(rows, cancellationToken);

    public async Task<(IReadOnlyList<PatientImportRow> Items, int TotalCount)> GetRowsPagedAsync(Guid batchId, ImportRowStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.PatientImportRows.Where(r => r.BatchId == batchId);
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        query = query.OrderBy(r => r.RowNumber);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<PatientImportRow>> GetAllRowsByStatusAsync(Guid batchId, ImportRowStatus status, CancellationToken cancellationToken)
        => await _dbContext.PatientImportRows
            .Where(r => r.BatchId == batchId && r.Status == status)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetValidRowIdsAsync(Guid batchId, CancellationToken cancellationToken)
        => await _dbContext.PatientImportRows
            .Where(r => r.BatchId == batchId && r.Status == ImportRowStatus.Valid)
            .OrderBy(r => r.RowNumber)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

    public Task<PatientImportRow?> GetRowByIdAsync(Guid rowId, CancellationToken cancellationToken)
        => _dbContext.PatientImportRows.FirstOrDefaultAsync(r => r.Id == rowId, cancellationToken);

    public void ClearTracking() => _dbContext.ChangeTracker.Clear();

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
