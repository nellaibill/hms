using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class NursingAssessmentRepository : INursingAssessmentRepository
{
    private readonly IPDDbContext _dbContext;

    public NursingAssessmentRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NursingAssessment assessment, CancellationToken cancellationToken)
        => await _dbContext.NursingAssessments.AddAsync(assessment, cancellationToken);

    public async Task<IReadOnlyList<NursingAssessment>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.NursingAssessments
            .Where(n => n.AdmissionId == admissionId)
            .OrderByDescending(n => n.AssessedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
