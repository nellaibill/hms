using HMS.Modules.Laboratory.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Laboratory.Domain;

/// <summary>
/// One append-only audit/history entry on a LabOrderItem — doubles as both "sample status
/// history" and general audit trail, avoiding two parallel history mechanisms. Every one of
/// LabOrderItem's mutators appends exactly one matching event; this is what backs the "Audit
/// History" tab and the "rejection history" requirement. No mutators here beyond the static
/// factory — append-only, no edits, no deletes.
/// </summary>
internal class LabOrderItemEvent : Entity
{
    public Guid LabOrderItemId { get; private set; }
    public LabOrderItemEventType EventType { get; private set; }
    public Guid? ActorId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? Remarks { get; private set; }

    // Required by EF Core materialization.
    private LabOrderItemEvent()
    {
    }

    private LabOrderItemEvent(Guid id, Guid labOrderItemId, LabOrderItemEventType eventType, Guid? actorId, string? remarks)
        : base(id, actorId)
    {
        LabOrderItemId = labOrderItemId;
        EventType = eventType;
        ActorId = actorId;
        OccurredAt = DateTime.UtcNow;
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
    }

    public static LabOrderItemEvent Create(Guid labOrderItemId, LabOrderItemEventType eventType, Guid? actorId, string? remarks)
        => new(Guid.CreateVersion7(), labOrderItemId, eventType, actorId, remarks);
}
