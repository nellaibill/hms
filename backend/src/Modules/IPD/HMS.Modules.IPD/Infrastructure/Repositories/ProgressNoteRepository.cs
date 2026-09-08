using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class ProgressNoteRepository : IProgressNoteRepository
{
    private readonly IPDDbContext _dbContext;

    public ProgressNoteRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProgressNote note, CancellationToken cancellationToken)
        => await _dbContext.ProgressNotes.AddAsync(note, cancellationToken);

    public async Task<IReadOnlyList<ProgressNote>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.ProgressNotes
            .Where(p => p.AdmissionId == admissionId)
            .OrderByDescending(p => p.NoteDateTime)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
