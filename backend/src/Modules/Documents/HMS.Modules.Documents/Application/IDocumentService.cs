using HMS.Modules.Documents.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Documents.Application;

/// <summary>
/// Public (not internal): DocumentsController — which ASP.NET Core requires to be public,
/// with a public constructor, for controller discovery and DI activation — takes this as a
/// constructor dependency. A public constructor cannot have an internal parameter type
/// (CS0051), so this interface is the module's deliberate, narrow seam between its public
/// HTTP boundary and its otherwise-internal Application/Domain/Infrastructure layers,
/// mirroring HMS.Modules.Patients.Application.IPatientService.
/// </summary>
public interface IDocumentService
{
    Task<Result<DocumentResponse>> UploadAsync(UploadDocumentRequest request, Stream content, string fileName, string contentType, long length, DocumentActor actor, CancellationToken cancellationToken);

    Task<Result<DocumentResponse>> GetByIdAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken);

    Task<PagedResult<DocumentResponse>> GetPagedAsync(DocumentListQuery query, DocumentActor actor, CancellationToken cancellationToken);

    Task<Result<DocumentSummaryResponse>> GetSummaryAsync(DocumentActor actor, CancellationToken cancellationToken);

    Task<Result<DocumentResponse>> ArchiveAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken);

    /// <summary>Resolves the readable content stream for a document, after the same access
    /// check as every other read — or a failure (not found, forbidden, or not yet available
    /// because it's still pending/quarantined scan).</summary>
    Task<Result<DocumentContent>> GetContentAsync(Guid id, DocumentActor actor, CancellationToken cancellationToken);
}

/// <summary>A resolved, readable document stream plus the metadata a controller needs to
/// build the HTTP response (content type, download file name).</summary>
public sealed record DocumentContent(Stream Content, string ContentType, string FileName) : IDisposable
{
    public void Dispose() => Content.Dispose();
}
