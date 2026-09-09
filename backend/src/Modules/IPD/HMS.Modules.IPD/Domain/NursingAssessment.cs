using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A single timestamped nursing assessment recorded against an Admission. Append-only —
/// mirrors VitalsReading/AdmissionCharge's own precedent (no Update/Delete): a correction is
/// a new assessment, not an edit to clinical history. No navigation collection on Admission
/// itself.
/// </summary>
internal class NursingAssessment : Entity
{
    public Guid AdmissionId { get; private set; }

    /// <summary>When the assessment was performed — distinct from CreatedAt.</summary>
    public DateTime AssessedAt { get; private set; }

    public string? GeneralCondition { get; private set; }
    public string? ConsciousnessLevel { get; private set; }
    public string? Mobility { get; private set; }
    public string? NutritionStatus { get; private set; }
    public string? FallRisk { get; private set; }
    public string? PressureSoreRisk { get; private set; }
    public string? SkinCondition { get; private set; }
    public int? PainScore { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Opaque reference into identity.users — same no-FK convention as Discharge
    /// Summary's sign-off fields.</summary>
    public Guid? AssessedByUserId { get; private set; }

    // Required by EF Core materialization.
    private NursingAssessment()
    {
    }

    private NursingAssessment(
        Guid id,
        Guid admissionId,
        DateTime assessedAt,
        string? generalCondition,
        string? consciousnessLevel,
        string? mobility,
        string? nutritionStatus,
        string? fallRisk,
        string? pressureSoreRisk,
        string? skinCondition,
        int? painScore,
        string? notes,
        Guid? assessedByUserId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        AssessedAt = assessedAt;
        GeneralCondition = Normalize(generalCondition);
        ConsciousnessLevel = Normalize(consciousnessLevel);
        Mobility = Normalize(mobility);
        NutritionStatus = Normalize(nutritionStatus);
        FallRisk = Normalize(fallRisk);
        PressureSoreRisk = Normalize(pressureSoreRisk);
        SkinCondition = Normalize(skinCondition);
        PainScore = painScore;
        Notes = Normalize(notes);
        AssessedByUserId = assessedByUserId;
    }

    public static NursingAssessment Create(
        Guid admissionId,
        DateTime assessedAt,
        string? generalCondition,
        string? consciousnessLevel,
        string? mobility,
        string? nutritionStatus,
        string? fallRisk,
        string? pressureSoreRisk,
        string? skinCondition,
        int? painScore,
        string? notes,
        Guid? assessedByUserId,
        Guid? createdBy)
    {
        return new NursingAssessment(
            Guid.CreateVersion7(),
            admissionId,
            assessedAt,
            generalCondition,
            consciousnessLevel,
            mobility,
            nutritionStatus,
            fallRisk,
            pressureSoreRisk,
            skinCondition,
            painScore,
            notes,
            assessedByUserId,
            createdBy);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
