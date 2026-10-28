using HMS.Modules.Billing.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Billing.Domain;

/// <summary>
/// A request to revert (cancel) an OP bill, raised by billing staff with a mandatory reason and
/// decided by a Super Admin (ADR-106). Approving it reverts the invoice — see
/// <see cref="Invoice.Revert"/>. Invoice number, patient and amounts are snapshotted at creation
/// like Invoice's own patient snapshot, so the approval list stays readable on its own.
/// Approve/Reject are only valid from Pending; InvoiceRevertService checks Status before calling
/// either (same convention as HR's LeaveRequest).
/// </summary>
internal class InvoiceRevertRequest : Entity
{
    public Guid InvoiceId { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public string PatientUhid { get; private set; } = null!;
    public decimal NetAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public string Reason { get; private set; } = null!;
    public InvoiceRevertRequestStatus Status { get; private set; }

    /// <summary>Usernames are snapshotted from the caller's token — Billing has no access to
    /// Identity's user directory (see HMS.ArchitectureTests' cross-module rules).</summary>
    public string? RequestedByName { get; private set; }

    public Guid? DecidedBy { get; private set; }
    public string? DecidedByName { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionNotes { get; private set; }

    // Required by EF Core materialization.
    private InvoiceRevertRequest()
    {
    }

    private InvoiceRevertRequest(Guid id, Invoice invoice, string reason, string? requestedByName, Guid? createdBy)
        : base(id, createdBy)
    {
        InvoiceId = invoice.Id;
        InvoiceNumber = invoice.InvoiceNumber;
        PatientId = invoice.PatientId;
        PatientName = invoice.PatientName;
        PatientUhid = invoice.PatientUhid;
        NetAmount = invoice.NetAmount;
        PaidAmount = invoice.Items.Where(i => i.PaymentStatus == PaymentStatus.Paid).Sum(i => i.Total);
        Reason = reason;
        Status = InvoiceRevertRequestStatus.Pending;
        RequestedByName = requestedByName;
    }

    public static InvoiceRevertRequest Create(Invoice invoice, string reason, string? requestedByName, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        return new InvoiceRevertRequest(
            Guid.CreateVersion7(),
            invoice,
            reason.Trim(),
            string.IsNullOrWhiteSpace(requestedByName) ? null : requestedByName.Trim(),
            createdBy);
    }

    public void Approve(Guid? actorId, string? actorName, string? notes) => Decide(InvoiceRevertRequestStatus.Approved, actorId, actorName, notes);

    public void Reject(Guid? actorId, string? actorName, string? notes) => Decide(InvoiceRevertRequestStatus.Rejected, actorId, actorName, notes);

    private void Decide(InvoiceRevertRequestStatus status, Guid? actorId, string? actorName, string? notes)
    {
        Status = status;
        DecidedBy = actorId;
        DecidedByName = string.IsNullOrWhiteSpace(actorName) ? null : actorName.Trim();
        DecidedAt = DateTime.UtcNow;
        DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkUpdated(actorId);
    }
}
