using HMS.Modules.DischargeSummary.Contracts;

namespace HMS.Modules.DischargeSummary.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at MVP scale — see docs/DecisionLog.md ADR-003.
/// </summary>
internal static class DischargeSummaryMappingExtensions
{
    public static DischargeSummaryResponse ToResponse(this Domain.DischargeSummary summary) => new()
    {
        Id = summary.Id,
        AdmissionId = summary.AdmissionId,
        PatientId = summary.PatientId,
        Status = summary.Status,
        FinalDiagnosis = summary.FinalDiagnosis,
        ChiefComplaints = summary.ChiefComplaints,
        HistoryOfPresentingIllness = summary.HistoryOfPresentingIllness,
        PastMedicalHistory = summary.PastMedicalHistory,
        PastSurgicalHistory = summary.PastSurgicalHistory,
        FamilyHistory = summary.FamilyHistory,
        PersonalHistory = summary.PersonalHistory,
        GeneralExamination = summary.GeneralExamination,
        CvsFindings = summary.CvsFindings,
        RsFindings = summary.RsFindings,
        PaFindings = summary.PaFindings,
        CnsFindings = summary.CnsFindings,
        LocalExamination = summary.LocalExamination,
        Gait = summary.Gait,
        HeightCm = summary.HeightCm,
        WeightKg = summary.WeightKg,
        PulseRate = summary.PulseRate,
        RespiratoryRate = summary.RespiratoryRate,
        TemperatureF = summary.TemperatureF,
        SpO2Percent = summary.SpO2Percent,
        BloodPressure = summary.BloodPressure,
        CourseInHospital = summary.CourseInHospital,
        ProcedureName = summary.ProcedureName,
        ProcedureDateTime = summary.ProcedureDateTime,
        PrimarySurgeon = summary.PrimarySurgeon,
        AssistantSurgeons = summary.AssistantSurgeons,
        Anaesthetist = summary.Anaesthetist,
        Anaesthesia = summary.Anaesthesia,
        SurgicalPosition = summary.SurgicalPosition,
        IntraOperativeFindings = summary.IntraOperativeFindings,
        OperativeNotes = summary.OperativeNotes,
        Diet = summary.Diet,
        WoundCare = summary.WoundCare,
        Activity = summary.Activity,
        Physiotherapy = summary.Physiotherapy,
        ReviewInstructions = summary.ReviewInstructions,
        EmergencyInstructions = summary.EmergencyInstructions,
        ConditionAtDischarge = summary.ConditionAtDischarge,
        PreparedByUserId = summary.PreparedByUserId,
        CheckedByUserId = summary.CheckedByUserId,
        ConsultantApprovedByUserId = summary.ConsultantApprovedByUserId,
        FinalizedAt = summary.FinalizedAt,
        FinalizedByUserId = summary.FinalizedByUserId,
        CreatedAt = summary.CreatedAt,
        UpdatedAt = summary.UpdatedAt,
    };
}
