namespace HMS.Modules.Masters.Domain;

/// <summary>
/// One consultation type a <see cref="Consultant"/> offers — a genuine many-to-many child,
/// mirroring DiagnosticPackageItem's shape/reasoning (a tiny plain entity, not a full
/// Entity-with-audit-columns aggregate member). ConsultationTypeId is an app-level-only
/// reference (validated by ConsultantService before this is created, no DB foreign key) — same
/// convention DiagnosticPackageItem.ServiceId uses for its own cross-entity Masters reference.
/// ConsultantId, by contrast, is a real DB foreign key with cascade delete since it points at
/// this item's own aggregate root, not across an entity boundary.
/// </summary>
internal class ConsultantConsultationType
{
    public Guid Id { get; private set; }
    public Guid ConsultantId { get; private set; }
    public Guid ConsultationTypeId { get; private set; }

    /// <summary>What the hospital pays this specific consultant for this specific consultation
    /// type — distinct from ConsultationType.Amount (what the patient is billed, constant per
    /// type). Genuinely varies doctor to doctor for the same type, which is exactly why it lives
    /// here on the join row rather than on ConsultationType or Consultant alone. Nullable for the
    /// same reason ConsultationType.Amount is: a rate not yet decided shouldn't be forced to 0.</summary>
    public decimal? ConsultantCharge { get; private set; }

    // Required by EF Core materialization.
    private ConsultantConsultationType()
    {
    }

    private ConsultantConsultationType(Guid id, Guid consultantId, Guid consultationTypeId, decimal? consultantCharge)
    {
        Id = id;
        ConsultantId = consultantId;
        ConsultationTypeId = consultationTypeId;
        ConsultantCharge = consultantCharge;
    }

    public static ConsultantConsultationType Create(Guid consultantId, Guid consultationTypeId, decimal? consultantCharge)
    {
        if (consultantCharge is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consultantCharge), "Consultant charge cannot be negative.");
        }

        return new(Guid.CreateVersion7(), consultantId, consultationTypeId, consultantCharge);
    }
}
