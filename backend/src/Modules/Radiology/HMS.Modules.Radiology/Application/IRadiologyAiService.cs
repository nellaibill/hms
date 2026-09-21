using HMS.Modules.Documents.Application;
using HMS.Modules.Radiology.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Radiology.Application;

/// <summary>
/// Public (not internal) for the same reason as HMS.Modules.Documents.Application.IDocumentService:
/// RadiologyAiController is public with a public constructor, so its dependency can't be internal (CS0051).
/// </summary>
public interface IRadiologyAiService
{
    /// <summary>Reads a patient's stored image (after the usual document access check), asks the
    /// configured vision model for a structured X-ray read, and saves it to the patient's record as
    /// an unreviewed draft.</summary>
    Task<Result<XrayAiAnalysisResponse>> AnalyzeDocumentAsync(Guid documentId, DocumentActor actor, CancellationToken cancellationToken);

    /// <summary>Every saved analysis of the images this caller can see for the patient, newest first.</summary>
    Task<Result<IReadOnlyList<XrayAiAnalysisResponse>>> GetPatientAnalysesAsync(Guid patientId, DocumentActor actor, CancellationToken cancellationToken);

    /// <summary>A doctor/radiologist confirms they have read a saved AI draft. Idempotent.</summary>
    Task<Result<XrayAiAnalysisResponse>> MarkReviewedAsync(Guid analysisId, DocumentActor actor, CancellationToken cancellationToken);
}
