using HMS.Modules.Documents.Application;
using HMS.Modules.Radiology.Application.Abstractions;
using HMS.Modules.Radiology.Contracts;
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

    private readonly IDocumentService _documents;
    private readonly IXrayImageAnalyzer _analyzer;
    private readonly ILogger<RadiologyAiService> _logger;

    public RadiologyAiService(IDocumentService documents, IXrayImageAnalyzer analyzer, ILogger<RadiologyAiService> logger)
    {
        _documents = documents;
        _analyzer = analyzer;
        _logger = logger;
    }

    public async Task<Result<XrayAiAnalysisResponse>> AnalyzeDocumentAsync(Guid documentId, DocumentActor actor, CancellationToken cancellationToken)
    {
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

        _logger.LogInformation("AI X-ray analysis generated for document {DocumentId} by {UserId}", documentId, actor.UserId);

        return Result<XrayAiAnalysisResponse>.Success(new XrayAiAnalysisResponse
        {
            DocumentId = documentId,
            Analysis = analysis.Value!.Text,
            Model = analysis.Value.Model,
            GeneratedAtUtc = DateTime.UtcNow,
            Disclaimer = XrayAnalysisPrompt.Disclaimer,
        });
    }

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
