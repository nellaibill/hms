using HMS.Modules.Documents.Contracts;

namespace HMS.Modules.Documents.Application.Security;

internal interface IDocumentAccessPolicy
{
    bool CanRead(DocumentActor actor, DocumentOwnerType ownerType, DocumentClassification classification);

    bool CanWrite(DocumentActor actor, DocumentOwnerType ownerType);
}
