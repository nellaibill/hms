using HMS.Modules.DischargeSummary.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.DischargeSummary.Domain;

/// <summary>
/// One discharge medication line, always owned by exactly one DischargeSummary — a real,
/// same-schema DB foreign key (unlike AdmissionId/PatientId on the parent, which are
/// cross-module app-level Guids). DrugName is free text, not a Products/Masters catalog
/// reference — Pharmacy has no structured Prescription entity with dose/route/frequency
/// fields to source this from (confirmed at plan time), so this mirrors what's actually
/// printed on a discharge summary. The full list is replaced wholesale on every Update
/// (DischargeSummary.ReplaceMedications) — no separate per-line CRUD endpoints, matching the
/// approved plan's MVP scope.
/// </summary>
internal class DischargeMedication : Entity
{
    public Guid DischargeSummaryId { get; private set; }
    public int SortOrder { get; private set; }
    public string DrugName { get; private set; } = null!;
    public string Dose { get; private set; } = null!;
    public string Route { get; private set; } = null!;
    public decimal MorningQty { get; private set; }
    public decimal NoonQty { get; private set; }
    public decimal EveningQty { get; private set; }
    public decimal NightQty { get; private set; }
    public int DurationDays { get; private set; }
    public FoodInstruction FoodInstruction { get; private set; }

    // Required by EF Core materialization.
    private DischargeMedication()
    {
    }

    private DischargeMedication(
        Guid id,
        Guid dischargeSummaryId,
        int sortOrder,
        string drugName,
        string dose,
        string route,
        decimal morningQty,
        decimal noonQty,
        decimal eveningQty,
        decimal nightQty,
        int durationDays,
        FoodInstruction foodInstruction,
        Guid? createdBy)
        : base(id, createdBy)
    {
        DischargeSummaryId = dischargeSummaryId;
        SortOrder = sortOrder;
        DrugName = drugName;
        Dose = dose;
        Route = route;
        MorningQty = morningQty;
        NoonQty = noonQty;
        EveningQty = eveningQty;
        NightQty = nightQty;
        DurationDays = durationDays;
        FoodInstruction = foodInstruction;
    }

    public static DischargeMedication Create(
        Guid dischargeSummaryId,
        int sortOrder,
        string drugName,
        string dose,
        string route,
        decimal morningQty,
        decimal noonQty,
        decimal eveningQty,
        decimal nightQty,
        int durationDays,
        FoodInstruction foodInstruction,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(drugName, nameof(drugName));
        Guard.AgainstNullOrWhiteSpace(dose, nameof(dose));
        Guard.AgainstNullOrWhiteSpace(route, nameof(route));

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4.
        return new DischargeMedication(
            Guid.CreateVersion7(),
            dischargeSummaryId,
            sortOrder,
            drugName.Trim(),
            dose.Trim(),
            route.Trim(),
            morningQty,
            noonQty,
            eveningQty,
            nightQty,
            durationDays,
            foodInstruction,
            createdBy);
    }
}
