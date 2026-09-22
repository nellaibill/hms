namespace HMS.Modules.Radiology.Contracts;

/// <summary>An AI-generated, unreviewed read of one stored X-ray image. Never a diagnosis —
/// <see cref="Disclaimer"/> must be shown next to <see cref="Analysis"/> wherever it's displayed.</summary>
public record XrayAiAnalysisResponse
{
    public Guid DocumentId { get; init; }

    /// <summary>The model's reply in the fixed section layout (ANATOMICAL REGION: … through
    /// CLINICAL REVIEW:), as free text — the UI splits it on those headings.</summary>
    public string Analysis { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public DateTime GeneratedAtUtc { get; init; }

    public string Disclaimer { get; init; } = string.Empty;
}
