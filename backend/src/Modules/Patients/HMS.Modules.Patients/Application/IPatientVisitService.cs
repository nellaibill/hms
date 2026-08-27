using HMS.Modules.Patients.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Public (not internal): PatientVisitsController — which ASP.NET Core requires to be public,
/// with a public constructor, for controller discovery and DI activation — takes this as a
/// constructor dependency. A public constructor cannot have an internal parameter type
/// (CS0051), so this interface is the module's deliberate, narrow seam between its public
/// HTTP boundary and its otherwise-internal Application/Domain/Infrastructure layers.
/// </summary>
public interface IPatientVisitService
{
    /// <summary>Records one registration/encounter event for an existing patient. All
    /// consultation lines supplied together share the returned VisitId.</summary>
    Task<Result<PatientVisitResponse>> CreateAsync(Guid patientId, CreatePatientVisitRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<PatientVisitResponse>> GetByIdAsync(Guid patientId, Guid visitId, CancellationToken cancellationToken);

    /// <summary>Every visit for the patient, newest first.</summary>
    Task<Result<IReadOnlyList<PatientVisitResponse>>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);
}
