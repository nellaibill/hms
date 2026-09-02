using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Public (not internal): PatientsController — which ASP.NET Core requires to be public,
/// with a public constructor, for controller discovery and DI activation — takes this as a
/// constructor dependency. A public constructor cannot have an internal parameter type
/// (CS0051), so this interface is the module's deliberate, narrow seam between its public
/// HTTP boundary and its otherwise-internal Application/Domain/Infrastructure layers.
/// </summary>
public interface IPatientService
{
    /// <summary>uhidOverride: normally left null so the standard sequence (40001+) assigns the
    /// UHID, exactly as manual registration always has. Bulk import supplies a pre-generated
    /// one instead (drawn from the reserved 1-40000 imported-patient range) — see
    /// PatientImportCommitBackgroundService — so every other invariant this method enforces
    /// (duplicate detection, the transaction, address/allergy/emergency-contact creation)
    /// still applies identically regardless of where the patient came from.</summary>
    Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request, Guid? actorId, CancellationToken cancellationToken, string? uhidOverride = null);

    Task<Result<PatientResponse>> UpdateAsync(Guid id, UpdatePatientRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PatientResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<PatientResponse>> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken);

    /// <summary>Adds one allergy row ("Add another Allergy") and returns the updated patient.</summary>
    Task<Result<PatientResponse>> AddAllergyAsync(Guid patientId, AddAllergyRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PatientResponse>> RemoveAllergyAsync(Guid patientId, Guid allergyId, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Adds one emergency contact ("Add another Emergency Contact") and returns the
    /// updated patient.</summary>
    Task<Result<PatientResponse>> AddEmergencyContactAsync(Guid patientId, AddEmergencyContactRequest request, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Fails with PatientErrorCodes.CannotRemoveLastEmergencyContact if this would
    /// leave the patient with zero — every patient must have at least one.</summary>
    Task<Result<PatientResponse>> RemoveEmergencyContactAsync(Guid patientId, Guid emergencyContactId, Guid? actorId, CancellationToken cancellationToken);
}
