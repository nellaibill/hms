using HMS.Modules.IPD.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Domain;

/// <summary>
/// A doctor's medication order (prescription) placed against an Admission. DrugName/Dose/
/// Route/Frequency stay free text — Pharmacy is direct-dispense only with no structured
/// Prescription entity to reference (same reasoning HMS.Modules.DischargeSummary.Domain.
/// DischargeMedication already documented; real Pharmacy integration — catalog-driven drug
/// selection, stock deduction on administration — remains an open gap, not solved here). A
/// simpler one-way lifecycle than DoctorOrder's (Active -> Discontinued only): Discontinue
/// mirrors DoctorOrder.Cancel's precondition-guard style. No navigation collection on
/// Admission itself, same convention as every other IPD child entity.
/// </summary>
internal class MedicationOrder : Entity
{
    public Guid AdmissionId { get; private set; }
    public string DrugName { get; private set; } = null!;
    public string Dose { get; private set; } = null!;
    public string Route { get; private set; } = null!;
    public string Frequency { get; private set; } = null!;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? Instructions { get; private set; }
    public DateTime OrderedAt { get; private set; }
    public Guid? OrderedByUserId { get; private set; }
    public MedicationOrderStatus Status { get; private set; }
    public DateTime? DiscontinuedAt { get; private set; }
    public string? DiscontinuedReason { get; private set; }

    // Required by EF Core materialization.
    private MedicationOrder()
    {
    }

    private MedicationOrder(
        Guid id,
        Guid admissionId,
        string drugName,
        string dose,
        string route,
        string frequency,
        DateTime startDate,
        DateTime? endDate,
        string? instructions,
        DateTime orderedAt,
        Guid? orderedByUserId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        AdmissionId = admissionId;
        DrugName = drugName;
        Dose = dose;
        Route = route;
        Frequency = frequency;
        StartDate = startDate;
        EndDate = endDate;
        Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();
        OrderedAt = orderedAt;
        OrderedByUserId = orderedByUserId;
        Status = MedicationOrderStatus.Active;
    }

    public static MedicationOrder Create(
        Guid admissionId,
        string drugName,
        string dose,
        string route,
        string frequency,
        DateTime startDate,
        DateTime? endDate,
        string? instructions,
        DateTime orderedAt,
        Guid? orderedByUserId,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(drugName, nameof(drugName));
        Guard.AgainstNullOrWhiteSpace(dose, nameof(dose));
        Guard.AgainstNullOrWhiteSpace(route, nameof(route));
        Guard.AgainstNullOrWhiteSpace(frequency, nameof(frequency));

        return new MedicationOrder(
            Guid.CreateVersion7(),
            admissionId,
            drugName.Trim(),
            dose.Trim(),
            route.Trim(),
            frequency.Trim(),
            startDate,
            endDate,
            instructions,
            orderedAt,
            orderedByUserId,
            createdBy);
    }

    /// <summary>One-way, from Active only. Caller (MedicationOrderService) is responsible for
    /// rejecting this call with a proper Result.Failure when already Discontinued.</summary>
    public void Discontinue(string? reason, Guid? actorId)
    {
        if (Status == MedicationOrderStatus.Discontinued)
        {
            throw new InvalidOperationException("This medication order has already been discontinued.");
        }

        Status = MedicationOrderStatus.Discontinued;
        DiscontinuedAt = DateTime.UtcNow;
        DiscontinuedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        MarkUpdated(actorId);
    }
}
