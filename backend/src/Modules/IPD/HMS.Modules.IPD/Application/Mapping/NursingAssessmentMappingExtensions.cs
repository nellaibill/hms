using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class NursingAssessmentMappingExtensions
{
    public static NursingAssessmentResponse ToResponse(this NursingAssessment assessment) => new()
    {
        Id = assessment.Id,
        AdmissionId = assessment.AdmissionId,
        AssessedAt = assessment.AssessedAt,
        GeneralCondition = assessment.GeneralCondition,
        ConsciousnessLevel = assessment.ConsciousnessLevel,
        Mobility = assessment.Mobility,
        NutritionStatus = assessment.NutritionStatus,
        FallRisk = assessment.FallRisk,
        PressureSoreRisk = assessment.PressureSoreRisk,
        SkinCondition = assessment.SkinCondition,
        PainScore = assessment.PainScore,
        Notes = assessment.Notes,
        AssessedByUserId = assessment.AssessedByUserId,
        CreatedAt = assessment.CreatedAt,
    };
}
