using HMS.Shared.Kernel;

namespace HMS.Modules.OpdConsultation.Domain;

/// <summary>
/// One prescribed medicine, always owned by exactly one OpdConsultationNote — a real, same-schema
/// DB foreign key, replaced wholesale on every save like diagnoses/investigations. Added for
/// regression report OPD-02: the consultation had no structured prescription at all, only the
/// free-text Plan of Management. DrugName is free text (the Pharmacy product catalog is empty on
/// fresh tenants and not every prescribed drug is stocked in-house); Dose/Frequency are free text
/// too, matching how prescriptions are actually written ("1-0-1", "SOS"), same as IPD's
/// MedicationOrder.
/// </summary>
internal class OpdConsultationPrescription
{
    public Guid Id { get; private set; }
    public Guid OpdConsultationNoteId { get; private set; }
    public string DrugName { get; private set; } = null!;
    public string? Dose { get; private set; }
    public string? Route { get; private set; }
    public string? Frequency { get; private set; }
    public int? DurationDays { get; private set; }
    public string? Instructions { get; private set; }

    // Required by EF Core materialization.
    private OpdConsultationPrescription()
    {
    }

    private OpdConsultationPrescription(Guid id, Guid opdConsultationNoteId, string drugName, string? dose, string? route, string? frequency, int? durationDays, string? instructions)
    {
        Id = id;
        OpdConsultationNoteId = opdConsultationNoteId;
        DrugName = drugName;
        Dose = dose;
        Route = route;
        Frequency = frequency;
        DurationDays = durationDays;
        Instructions = instructions;
    }

    public static OpdConsultationPrescription Create(Guid opdConsultationNoteId, string drugName, string? dose, string? route, string? frequency, int? durationDays, string? instructions)
    {
        Guard.AgainstNullOrWhiteSpace(drugName, nameof(drugName));
        return new(Guid.CreateVersion7(), opdConsultationNoteId, drugName.Trim(), Normalize(dose), Normalize(route), Normalize(frequency), durationDays, Normalize(instructions));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
