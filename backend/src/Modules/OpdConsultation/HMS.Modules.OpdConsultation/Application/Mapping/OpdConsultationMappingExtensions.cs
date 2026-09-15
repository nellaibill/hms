using HMS.Modules.OpdConsultation.Contracts;
using HMS.Modules.OpdConsultation.Domain;

namespace HMS.Modules.OpdConsultation.Application.Mapping;

internal static class OpdConsultationMappingExtensions
{
    public static OpdConsultationNoteResponse ToResponse(this OpdConsultationNote note) => new()
    {
        Id = note.Id,
        ConsultationId = note.ConsultationId,
        Status = note.Status,
        HeightCm = note.HeightCm,
        WeightKg = note.WeightKg,
        PulseRate = note.PulseRate,
        BloodPressure = note.BloodPressure,
        TemperatureF = note.TemperatureF,
        SpO2Percent = note.SpO2Percent,
        PresentingComplaints = note.PresentingComplaints,
        ClinicalHistory = note.ClinicalHistory,
        ExaminationFindings = note.ExaminationFindings,
        Diagnoses = note.Diagnoses.Select(d => new OpdConsultationDiagnosisResponse { Id = d.Id, DiagnosisId = d.DiagnosisId, Type = d.Type }).ToList(),
        Investigations = note.Investigations
            .Select(i => new OpdConsultationInvestigationResponse { Id = i.Id, Name = i.Name, Department = i.Department, Priority = i.Priority })
            .ToList(),
        PlanOfManagement = note.PlanOfManagement,
        ReviewDate = note.ReviewDate,
        FollowUpInstructions = note.FollowUpInstructions,
        EmergencyReviewInstructions = note.EmergencyReviewInstructions,
        ReferralDepartmentId = note.ReferralDepartmentId,
        ReferralConsultantId = note.ReferralConsultantId,
        ReferralReason = note.ReferralReason,
        CreatedAt = note.CreatedAt,
        UpdatedAt = note.UpdatedAt,
    };
}
