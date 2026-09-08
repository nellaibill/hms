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
        PreparedByUserId = summary.PreparedByUserId,
        CheckedByUserId = summary.CheckedByUserId,
        ConsultantApprovedByUserId = summary.ConsultantApprovedByUserId,
        FinalizedAt = summary.FinalizedAt,
        FinalizedByUserId = summary.FinalizedByUserId,
        CreatedAt = summary.CreatedAt,
        UpdatedAt = summary.UpdatedAt,
    };
}
