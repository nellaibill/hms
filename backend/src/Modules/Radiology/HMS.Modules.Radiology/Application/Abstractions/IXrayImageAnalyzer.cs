using HMS.Shared.Kernel;

namespace HMS.Modules.Radiology.Application.Abstractions;

/// <summary>Sends one X-ray image to a vision-language model and returns its text reply. A
/// missing configuration or a failed call is always a Result.Failure — the caller is a
/// clinician waiting on the result, so it must never silently no-op.</summary>
internal interface IXrayImageAnalyzer
{
    Task<Result<XrayImageAnalysis>> AnalyzeAsync(byte[] image, string contentType, CancellationToken cancellationToken);
}

internal sealed record XrayImageAnalysis(string Text, string Model);
