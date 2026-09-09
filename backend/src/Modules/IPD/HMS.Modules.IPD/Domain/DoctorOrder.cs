using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A clinical order (Radiology/Procedure/Diet/Nursing/Blood/Consultation/Referral — see
/// DoctorOrderType's own doc comment for what's deliberately excluded and why) placed against
/// an Admission. Unlike VitalsReading/ProgressNote/NursingAssessment/NursingNote (append-only
/// logs), this is a genuinely mutable entity with a real lifecycle — Advance/Cancel mirror
/// LabOrder.GenerateReport/ReleaseReport's precondition-guard style: the caller
/// (DoctorOrderService) pre-checks the same condition to return a proper Result.Failure, this
/// method re-checks it as a genuine domain invariant. No navigation collection on Admission
/// itself, same convention as every other IPD child entity.
/// </summary>
internal class DoctorOrder : Entity
{
    public Guid AdmissionId { get; private set; }
    public DoctorOrderType OrderType { get; private set; }
    public string Description { get; private set; } = null!;
    public string? Instructions { get; private set; }
    public DateTime OrderedAt { get; private set; }
    public Guid? OrderedByUserId { get; private set; }
    public DoctorOrderStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // Required by EF Core materialization.
    private DoctorOrder()
    {
    }

    private DoctorOrder(
        Guid id,
        Guid admissionId,
        DoctorOrderType orderType,
        string description,
        string? instructions,
        DateTime orderedAt,
        Guid? orderedByUserId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        OrderType = orderType;
        Description = description;
        Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();
        OrderedAt = orderedAt;
        OrderedByUserId = orderedByUserId;
        Status = DoctorOrderStatus.Ordered;
    }

    public static DoctorOrder Create(
        Guid admissionId,
        DoctorOrderType orderType,
        string description,
        string? instructions,
        DateTime orderedAt,
        Guid? orderedByUserId,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(description, nameof(description));

        return new DoctorOrder(
            Guid.CreateVersion7(),
            admissionId,
            orderType,
            description.Trim(),
            instructions,
            orderedAt,
            orderedByUserId,
            createdBy);
    }

    /// <summary>Moves to the next state in the fixed Ordered -> Accepted -> InProgress ->
    /// Completed sequence. Caller (DoctorOrderService) is responsible for rejecting this call
    /// with a proper Result.Failure when already Completed/Cancelled.</summary>
    public void Advance(Guid? actorId)
    {
        Status = Status switch
        {
            DoctorOrderStatus.Ordered => DoctorOrderStatus.Accepted,
            DoctorOrderStatus.Accepted => DoctorOrderStatus.InProgress,
            DoctorOrderStatus.InProgress => DoctorOrderStatus.Completed,
            _ => throw new InvalidOperationException($"Cannot advance a doctor order that is already {Status}."),
        };

        if (Status == DoctorOrderStatus.Completed)
        {
            CompletedAt = DateTime.UtcNow;
        }

        MarkUpdated(actorId);
    }

    /// <summary>Cancellable from any non-terminal state. Caller (DoctorOrderService) is
    /// responsible for rejecting this call with a proper Result.Failure when already
    /// Completed/Cancelled.</summary>
    public void Cancel(string? reason, Guid? actorId)
    {
        if (Status is DoctorOrderStatus.Completed or DoctorOrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel a doctor order that is already {Status}.");
        }

        Status = DoctorOrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        MarkUpdated(actorId);
    }
}
