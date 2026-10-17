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
        VisitNumber = visit.VisitNumber,
        AppointmentTypeId = visit.AppointmentTypeId,
        EmergencyType = visit.EmergencyType,
        AdmissionType = visit.AdmissionType,
        ArNumber = visit.ArNumber,
        IsReferred = visit.IsReferred,
        ReferredBy = visit.ReferredBy,
        ReferralName = visit.ReferralName,
        ReferralMobile = visit.ReferralMobile,
        Category = visit.Category,
        Consultations = visit.Consultations.Select(c => c.ToResponse()).ToList(),
        CreatedAt = visit.CreatedAt,
    };

    public static VisitConsultationResponse ToResponse(this PatientVisitConsultation consultation) => new()
    {
        Id = consultation.Id,
        DepartmentId = consultation.DepartmentId,
        ConsultantId = consultation.ConsultantId,
        ConsultationTypeId = consultation.ConsultationTypeId,
        AppointmentTime = consultation.AppointmentTime,
        Status = consultation.Status,
    };
}
