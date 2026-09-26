using HMS.Modules.Documents.Contracts;

namespace HMS.Modules.Documents.Application.Security;

internal interface IDocumentAccessPolicy
{
    bool CanRead(DocumentActor actor, DocumentOwnerType ownerType, DocumentClassification classification);

    bool CanWrite(DocumentActor actor, DocumentOwnerType ownerType);

    /// <summary>Repository-wide RAG index maintenance (the backfill) — crosses every owner
    /// type at once, so only the bypass roles.</summary>
    bool CanManageIndex(DocumentActor actor);
}
