using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): NursingNotesController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes this
/// as a constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051).
/// </summary>
public interface INursingNoteService
{
    Task<Result<NursingNoteResponse>> CreateAsync(Guid admissionId, CreateNursingNoteRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<NursingNoteResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken);
}

internal class NursingNoteService : INursingNoteService
{
    private readonly INursingNoteRepository _repository;
    private readonly IAdmissionRepository _admissionRepository;

    public NursingNoteService(INursingNoteRepository repository, IAdmissionRepository admissionRepository)
    {
        _repository = repository;
        _admissionRepository = admissionRepository;
    }

    public async Task<Result<NursingNoteResponse>> CreateAsync(Guid admissionId, CreateNursingNoteRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<NursingNoteResponse>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var note = NursingNote.Create(
            admissionId,
            request.NoteDateTime,
            request.Shift,
            request.Observation,
            request.Intervention,
            request.PatientResponse,
            request.Remarks,
            request.RecordedByUserId,
            actorId);

        await _repository.AddAsync(note, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<NursingNoteResponse>.Success(note.ToResponse());
    }

    public async Task<Result<IReadOnlyList<NursingNoteResponse>>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
    {
        if (await _admissionRepository.GetByIdAsync(admissionId, cancellationToken) is null)
        {
            return Result<IReadOnlyList<NursingNoteResponse>>.Failure(IPDErrorCodes.NotFound, $"Admission '{admissionId}' was not found.");
        }

        var notes = await _repository.GetByAdmissionIdAsync(admissionId, cancellationToken);
        return Result<IReadOnlyList<NursingNoteResponse>>.Success(notes.Select(n => n.ToResponse()).ToList());
    }
}
