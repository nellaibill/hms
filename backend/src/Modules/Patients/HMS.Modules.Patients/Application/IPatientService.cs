using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Public (not internal): this is the one Application-layer type PatientsController — which
/// ASP.NET Core requires to be public, with a public constructor, for controller discovery
/// and DI activation — takes as a constructor dependency. A public constructor cannot have
/// an internal parameter type (CS0051), so this interface is the module's deliberate,
/// narrow seam between its public HTTP boundary and its otherwise-internal
/// Application/Domain/Infrastructure layers, mirroring HMS.Modules.Identity.IUserService.
/// </summary>
public interface IPatientService
{
    Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PatientResponse>> UpdateAsync(Guid id, UpdatePatientRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PatientResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PatientResponse>> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken);

    Task<Result<PatientResponse>> UploadPhotoAsync(Guid id, Stream content, string fileName, string contentType, long length, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PatientResponse>> UploadIdProofAsync(Guid id, IdProofType idProofType, Stream content, string fileName, string contentType, long length, Guid? actorId, CancellationToken cancellationToken);
}
