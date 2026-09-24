using HMS.Modules.OpdConsultation.Contracts;
using HMS.Modules.OpdConsultation.Domain;

namespace HMS.Modules.OpdConsultation.Application.Mapping;

internal static class OpdConsultationMappingExtensions
{
    /// <param name="diagnosisLabels">Diagnosis id -> (Name, IcdCode), resolved from Masters by
    /// OpdConsultationService. Optional: a missing entry just leaves DiagnosisName/IcdCode null.</param>
    public static OpdConsultationNoteResponse ToResponse(this OpdConsultationNote note, IReadOnlyDictionary<Guid, (string Name, string? IcdCode)>? diagnosisLabels = null) => new()
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
        Diagnoses = note.Diagnoses
            .Select(d =>
            {
                var label = diagnosisLabels is not null && diagnosisLabels.TryGetValue(d.DiagnosisId, out var found) ? found : ((string Name, string? IcdCode)?)null;
                return new OpdConsultationDiagnosisResponse { Id = d.Id, DiagnosisId = d.DiagnosisId, Type = d.Type, DiagnosisName = label?.Name, IcdCode = label?.IcdCode };
            })
            .ToList(),
        Investigations = note.Investigations
            .Select(i => new OpdConsultationInvestigationResponse { Id = i.Id, Name = i.Name, Department = i.Department, Priority = i.Priority, ServiceId = i.ServiceId })
            .ToList(),
        Prescriptions = note.Prescriptions
            .Select(p => new OpdConsultationPrescriptionResponse
            {
                Id = p.Id,
                DrugName = p.DrugName,
                Dose = p.Dose,
                Route = p.Route,
                Frequency = p.Frequency,
                DurationDays = p.DurationDays,
                Instructions = p.Instructions,
            })
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
