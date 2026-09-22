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
    /// <summary>Reads the stored image document (after the usual document access check) and asks the
    /// configured vision model for a structured, unreviewed X-ray read.</summary>
    Task<Result<XrayAiAnalysisResponse>> AnalyzeDocumentAsync(Guid documentId, DocumentActor actor, CancellationToken cancellationToken);
}
