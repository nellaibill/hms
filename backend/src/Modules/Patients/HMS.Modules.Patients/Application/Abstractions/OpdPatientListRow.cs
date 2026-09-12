using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>
/// One joined PatientVisitConsultation+PatientVisit+Patient row, as returned by
/// IPatientVisitRepository.GetOpdPatientListPagedAsync — the repository's own shape (plain
/// scalar fields, not the domain entities themselves) since the query behind it is a
/// SelectMany/join projection, not an aggregate load. OpdQueryService maps this into the
/// public OpdPatientListItem, resolving Department/Consultant/AppointmentType display names
/// along the way.
/// </summary>
internal sealed record OpdPatientListRow(
    Guid ConsultationId,
    Guid VisitId,
    Guid PatientId,
    string Uhid,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    DateTime AppointmentTime,
    Guid? AppointmentTypeId,
    Guid DepartmentId,
    Guid ConsultantId,
    OpdConsultationStatus Status);
