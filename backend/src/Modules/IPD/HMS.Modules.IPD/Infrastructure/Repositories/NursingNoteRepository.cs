using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class NursingNoteRepository : INursingNoteRepository
{
    private readonly IPDDbContext _dbContext;

    public NursingNoteRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NursingNote note, CancellationToken cancellationToken)
        => await _dbContext.NursingNotes.AddAsync(note, cancellationToken);

    public async Task<IReadOnlyList<NursingNote>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.NursingNotes
            .Where(n => n.AdmissionId == admissionId)
            .OrderByDescending(n => n.NoteDateTime)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
