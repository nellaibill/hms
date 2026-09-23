using HMS.Modules.OpdConsultation.Application.Abstractions;
using HMS.Modules.OpdConsultation.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.OpdConsultation.Infrastructure.Repositories;

internal class OpdConsultationRepository : IOpdConsultationRepository
{
    private readonly OpdConsultationDbContext _dbContext;

    public OpdConsultationRepository(OpdConsultationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OpdConsultationNote note, CancellationToken cancellationToken)
        => await _dbContext.OpdConsultationNotes.AddAsync(note, cancellationToken);

    public Task<OpdConsultationNote?> GetByConsultationIdAsync(Guid consultationId, CancellationToken cancellationToken)
        // Diagnoses/Investigations must be Included (not lazily loaded — this codebase never
        // enables lazy loading) both so GET responses carry the current lists and so
        // ReplaceDiagnoses/ReplaceInvestigations' in-memory Clear()/AddRange() is diffed
        // correctly by EF's change tracker on save.
        => _dbContext.OpdConsultationNotes
            .Include(x => x.Diagnoses)
            .Include(x => x.Investigations)
            .FirstOrDefaultAsync(x => x.ConsultationId == consultationId, cancellationToken);

    public async Task<IReadOnlyList<OpdConsultationNote>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
        => await _dbContext.OpdConsultationNotes
            .AsNoTracking()
            .Include(x => x.Diagnoses)
            .Include(x => x.Investigations)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
