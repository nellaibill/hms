using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class AdmissionRepository : IAdmissionRepository
{
    private readonly IPDDbContext _dbContext;

    public AdmissionRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Admission admission, CancellationToken cancellationToken)
        => await _dbContext.Admissions.AddAsync(admission, cancellationToken);

    public Task<Admission?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Admissions.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Admission?> GetActiveByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
        => _dbContext.Admissions.FirstOrDefaultAsync(
            a => a.PatientId == patientId && a.Status == AdmissionStatus.Admitted,
            cancellationToken);

    public async Task<(IReadOnlyList<Admission> Items, int TotalCount)> GetPagedAsync(AdmissionListQuery query, CancellationToken cancellationToken)
    {
        var admissions = _dbContext.Admissions.AsQueryable();

        if (query.Status.HasValue)
        {
            admissions = admissions.Where(a => a.Status == query.Status.Value);
        }

        if (query.WardId.HasValue)
        {
            admissions = admissions.Where(a => a.WardId == query.WardId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            admissions = admissions.Where(a => a.DepartmentId == query.DepartmentId.Value);
        }

        if (query.ConsultantId.HasValue)
        {
            admissions = admissions.Where(a => a.ConsultantId == query.ConsultantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            admissions = admissions.Where(a => EF.Functions.ILike(a.AdmissionNumber, term));
        }

        admissions = ApplySort(admissions, query.Sort);

        var totalCount = await admissions.CountAsync(cancellationToken);
        var items = await admissions.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountByStatusAsync(AdmissionStatus status, CancellationToken cancellationToken)
        => _dbContext.Admissions.CountAsync(a => a.Status == status, cancellationToken);

    public Task<int> CountAdmittedTodayAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        return _dbContext.Admissions.CountAsync(
            a => a.AdmissionDateTime >= today && a.AdmissionDateTime < tomorrow,
            cancellationToken);
    }

    public Task<int> CountDischargedTodayAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        return _dbContext.Admissions.CountAsync(
            a => a.DischargeDateTime != null && a.DischargeDateTime >= today && a.DischargeDateTime < tomorrow,
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Admission> ApplySort(IQueryable<Admission> admissions, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return admissions.OrderByDescending(a => a.AdmissionDateTime);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "admissionnumber" => descending ? admissions.OrderByDescending(a => a.AdmissionNumber) : admissions.OrderBy(a => a.AdmissionNumber),
            "updatedat" => descending ? admissions.OrderByDescending(a => a.UpdatedAt) : admissions.OrderBy(a => a.UpdatedAt),
            _ => descending ? admissions.OrderByDescending(a => a.AdmissionDateTime) : admissions.OrderBy(a => a.AdmissionDateTime),
        };
    }
}
