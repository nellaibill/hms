namespace HMS.Modules.Documents.Contracts;

/// <summary>
/// The form-field portion of a POST /api/v1/documents request — the file itself arrives as a
/// separate <c>IFormFile</c> controller-action parameter, mirroring
/// HMS.Modules.Patients.Endpoints.PatientsController.UploadIdProof's
/// <c>(IFormFile file, [FromForm] IdProofType idProofType)</c> shape.
/// </summary>
public record UploadDocumentRequest
{
    public DocumentOwnerType OwnerType { get; init; }
    public Guid OwnerId { get; init; }
    public DocumentType DocumentType { get; init; }

    /// <summary>Defaults to <see cref="DocumentClassification.Internal"/> when not supplied —
    /// see <see cref="DocumentClassification"/> for why this is independent of owner type.</summary>
    public DocumentClassification? Classification { get; init; }

    /// <summary>Optional — when the document itself has a renewal/expiry date (e.g. a Staff
    /// ID proof or certification). Null for every owner type with no notion of expiry; see
    /// Domain.Document.ExpiryDate's own remarks.</summary>
    public DateOnly? ExpiryDate { get; init; }
}
