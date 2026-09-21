using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Radiology.Application.Abstractions;
using HMS.Modules.Radiology.Contracts;
using HMS.Modules.Radiology.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Radiology.Application;

internal sealed class RadiologyAiService : IRadiologyAiService
{
    /// <summary>Hosted vision endpoints reject large request bodies; a stored X-ray beyond this is
    /// better re-exported smaller than silently truncated.</summary>
    internal const long MaxImageBytes = 8 * 1024 * 1024;

    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    /// <summary>LoginTypes (the stable closed set on the JWT — see DocumentActor) allowed to sign off
    /// an AI draft: the clinicians who read images, plus the two administrator roles.</summary>
    private static readonly HashSet<string> ReviewerLoginTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "doctor",
        "radiologist",
        "admin",
        "superAdmin",
    };

    private readonly IDocumentService _documents;
    private readonly IXrayImageAnalyzer _analyzer;
    private readonly IXrayAiAnalysisRepository _repository;
    private readonly ILogger<RadiologyAiService> _logger;

    public RadiologyAiService(
        IDocumentService documents,
        IXrayImageAnalyzer analyzer,
        IXrayAiAnalysisRepository repository,
        ILogger<RadiologyAiService> logger)
    {
        _documents = documents;
        _analyzer = analyzer;
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<XrayAiAnalysisResponse>> AnalyzeDocumentAsync(Guid documentId, DocumentActor actor, CancellationToken cancellationToken)
    {
        var documentResult = await _documents.GetByIdAsync(documentId, actor, cancellationToken);
        if (!documentResult.IsSuccess)
        {
            return Result<XrayAiAnalysisResponse>.Failure(documentResult.ErrorCode!, documentResult.Error!);
        }

        var document = documentResult.Value!;
        if (document.OwnerType != DocumentOwnerType.Patient)
        {
            return Result<XrayAiAnalysisResponse>.Failure(
                RadiologyAiErrorCodes.NotPatientDocument,
                "Only images stored in a patient's documents can be analyzed.");
        }

        var contentResult = await _documents.GetContentAsync(documentId, actor, cancellationToken);
        if (!contentResult.IsSuccess)
        {
            return Result<XrayAiAnalysisResponse>.Failure(contentResult.ErrorCode!, contentResult.Error!);
        }

        using var content = contentResult.Value!;

        if (!SupportedContentTypes.Contains(content.ContentType))
        {
            return Result<XrayAiAnalysisResponse>.Failure(
                RadiologyAiErrorCodes.UnsupportedImage,
                "Only JPEG, PNG or WebP images can be analyzed. DICOM and PDF files aren't supported yet.");
        }

        var image = await ReadBoundedAsync(content.Content, cancellationToken);
        if (image is null)
        {
            return Result<XrayAiAnalysisResponse>.Failure(
                RadiologyAiErrorCodes.ImageTooLarge,
                $"The image is larger than {MaxImageBytes / (1024 * 1024)} MB. Upload a smaller export to analyze it.");
        }

        // Only the pixel data leaves the server: no file name, patient id or document id is sent.
        var analysis = await _analyzer.AnalyzeAsync(image, content.ContentType, cancellationToken);
        if (!analysis.IsSuccess)
        {
            return Result<XrayAiAnalysisResponse>.Failure(analysis.ErrorCode!, analysis.Error!);
        }

        var saved = XrayAiAnalysis.Create(document.OwnerId, documentId, analysis.Value!.Text, analysis.Value.Model, actor.UserId);
        await _repository.AddAsync(saved, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AI X-ray analysis {AnalysisId} saved for document {DocumentId} by {UserId}", saved.Id, documentId, actor.UserId);

        return Result<XrayAiAnalysisResponse>.Success(ToResponse(saved));
    }

    public async Task<Result<IReadOnlyList<XrayAiAnalysisResponse>>> GetPatientAnalysesAsync(Guid patientId, DocumentActor actor, CancellationToken cancellationToken)
    {
        // Visibility comes from the Documents module's own access policy: an analysis is only listed
        // if the caller can also see the image document it belongs to.
        var documents = await _documents.GetPagedAsync(
            new DocumentListQuery { OwnerType = DocumentOwnerType.Patient, OwnerId = patientId, PageSize = PagedRequest.MaxPageSize },
            actor,
            cancellationToken);

        var documentIds = documents.Items.Select(d => d.Id).ToList();
        if (documentIds.Count == 0)
        {
            return Result<IReadOnlyList<XrayAiAnalysisResponse>>.Success([]);
        }

        var analyses = await _repository.ListByDocumentIdsAsync(documentIds, cancellationToken);
        return Result<IReadOnlyList<XrayAiAnalysisResponse>>.Success(analyses.Select(ToResponse).ToList());
    }

    public async Task<Result<XrayAiAnalysisResponse>> MarkReviewedAsync(Guid analysisId, DocumentActor actor, CancellationToken cancellationToken)
    {
        if (actor.LoginType is null || !ReviewerLoginTypes.Contains(actor.LoginType))
        {
            return Result<XrayAiAnalysisResponse>.Failure(
                RadiologyAiErrorCodes.ReviewForbidden,
                "Only a doctor or radiologist can mark an AI analysis as reviewed.");
        }

        var analysis = await _repository.GetByIdAsync(analysisId, cancellationToken);
        if (analysis is null)
        {
            return Result<XrayAiAnalysisResponse>.Failure(RadiologyAiErrorCodes.AnalysisNotFound, $"Analysis '{analysisId}' was not found.");
        }

        // Same "can't see the image, can't see the analysis" rule as the list.
        var document = await _documents.GetByIdAsync(analysis.DocumentId, actor, cancellationToken);
        if (!document.IsSuccess)
        {
            return Result<XrayAiAnalysisResponse>.Failure(RadiologyAiErrorCodes.AnalysisNotFound, $"Analysis '{analysisId}' was not found.");
        }

        analysis.MarkReviewed(actor.UserId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AI X-ray analysis {AnalysisId} reviewed by {UserId}", analysisId, actor.UserId);

        return Result<XrayAiAnalysisResponse>.Success(ToResponse(analysis));
    }

    private static XrayAiAnalysisResponse ToResponse(XrayAiAnalysis analysis) => new()
    {
        Id = analysis.Id,
        PatientId = analysis.PatientId,
        DocumentId = analysis.DocumentId,
        Analysis = analysis.Analysis,
        Model = analysis.Model,
        GeneratedAtUtc = analysis.CreatedAt,
        GeneratedByUserId = analysis.CreatedBy,
        IsReviewed = analysis.IsReviewed,
        ReviewedByUserId = analysis.ReviewedByUserId,
        ReviewedAtUtc = analysis.ReviewedAtUtc,
        Disclaimer = XrayAnalysisPrompt.Disclaimer,
    };

    /// <summary>Reads at most <see cref="MaxImageBytes"/>; null when the stream is longer.</summary>
    private static async Task<byte[]?> ReadBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxImageBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
