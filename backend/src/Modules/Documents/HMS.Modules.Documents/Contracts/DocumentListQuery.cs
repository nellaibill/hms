using HMS.Shared.Kernel;

namespace HMS.Modules.Documents.Contracts;

public enum DocumentStatusFilter
{
    All = 0,
    Active = 1,
    Archived = 2,
}

/// <summary>
/// Query shape for GET /api/v1/documents — mirrors every filter already implemented in the
/// existing frontend mock (frontend/web/src/features/documents/components/FilterBar.tsx), so
/// wiring the real API to that UI later is a drop-in swap rather than a redesign. Inherits
/// Page/PageSize/Sort/Search from <see cref="PagedRequest"/> per docs/ApiStandards.md §6.
/// </summary>
public class DocumentListQuery : PagedRequest
{
    public DocumentOwnerType? OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public DocumentStatusFilter Status { get; set; } = DocumentStatusFilter.All;
}
