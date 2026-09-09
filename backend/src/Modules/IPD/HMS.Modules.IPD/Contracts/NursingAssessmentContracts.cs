namespace HMS.Modules.IPD.Contracts;

public record CreateNursingAssessmentRequest
{
    public DateTime AssessedAt { get; init; }
    public string? GeneralCondition { get; init; }
    public string? ConsciousnessLevel { get; init; }
    public string? Mobility { get; init; }
    public string? NutritionStatus { get; init; }
    public string? FallRisk { get; init; }
    public string? PressureSoreRisk { get; init; }
    public string? SkinCondition { get; init; }
    public int? PainScore { get; init; }
    public string? Notes { get; init; }
    public Guid? AssessedByUserId { get; init; }
}

public record NursingAssessmentResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public DateTime AssessedAt { get; init; }
    public string? GeneralCondition { get; init; }
    public string? ConsciousnessLevel { get; init; }
    public string? Mobility { get; init; }
    public string? NutritionStatus { get; init; }
    public string? FallRisk { get; init; }
    public string? PressureSoreRisk { get; init; }
    public string? SkinCondition { get; init; }
    public int? PainScore { get; init; }
    public string? Notes { get; init; }
    public Guid? AssessedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
