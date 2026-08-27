using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Mapping;

internal static class PatientVisitMappingExtensions
{
    public static PatientVisitResponse ToResponse(this PatientVisit visit) => new()
    {
        VisitId = visit.Id,
        PatientId = visit.PatientId,
        VisitType = visit.VisitType,
        AppointmentTypeId = visit.AppointmentTypeId,
        Consultations = visit.Consultations.Select(c => c.ToResponse()).ToList(),
        CreatedAt = visit.CreatedAt,
    };

    public static VisitConsultationResponse ToResponse(this PatientVisitConsultation consultation) => new()
    {
        Id = consultation.Id,
        DepartmentId = consultation.DepartmentId,
        ConsultantId = consultation.ConsultantId,
        ConsultationTypeId = consultation.ConsultationTypeId,
    };
}
