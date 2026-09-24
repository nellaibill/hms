using HMS.Shared.Kernel;

namespace HMS.Modules.Radiology.Domain;

/// <summary>
/// One saved AI read of a patient's stored X-ray image. The document itself lives in the
/// Documents module (linked here by <see cref="DocumentId"/>, an app-level reference with no
/// database foreign key, like every other cross-module reference); this row is the AI's
/// unreviewed draft plus the clinician sign-off on it. A re-analysis creates a new row, so the
/// history of what the model said is kept rather than overwritten.
/// </summary>
internal class XrayAiAnalysis : Entity
{
    public Guid PatientId { get; private set; }
    public Guid DocumentId { get; private set; }

    /// <summary>The model's reply in the fixed section layout, exactly as returned.</summary>
    public string Analysis { get; private set; } = null!;

    public string Model { get; private set; } = null!;

    public bool IsReviewed { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }

    // Required by EF Core materialization.
    private XrayAiAnalysis()
    {
    }

    private XrayAiAnalysis(Guid id, Guid patientId, Guid documentId, string analysis, string model, Guid? createdBy)
        : base(id, createdBy)
    {
        PatientId = patientId;
        DocumentId = documentId;
        Analysis = analysis;
        Model = model;
    }

    public static XrayAiAnalysis Create(Guid patientId, Guid documentId, string analysis, string model, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(analysis, nameof(analysis));
        Guard.AgainstNullOrWhiteSpace(model, nameof(model));

        return new XrayAiAnalysis(Guid.CreateVersion7(), patientId, documentId, analysis, model.Trim(), createdBy);
    }

    /// <summary>A clinician confirms they have read the AI draft. Idempotent — the first reviewer
    /// and time are kept.</summary>
    public void MarkReviewed(Guid? reviewerId)
    {
        if (IsReviewed)
        {
            return;
        }

        IsReviewed = true;
        ReviewedByUserId = reviewerId;
        ReviewedAtUtc = DateTime.UtcNow;
        MarkUpdated(reviewerId);
    }
}
