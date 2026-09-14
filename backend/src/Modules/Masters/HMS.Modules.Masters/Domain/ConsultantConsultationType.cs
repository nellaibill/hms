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

    // Required by EF Core materialization.
    private ConsultantConsultationType()
    {
    }

    private ConsultantConsultationType(Guid id, Guid consultantId, Guid consultationTypeId)
    {
        Id = id;
        ConsultantId = consultantId;
        ConsultationTypeId = consultationTypeId;
    }

    public static ConsultantConsultationType Create(Guid consultantId, Guid consultationTypeId)
        => new(Guid.CreateVersion7(), consultantId, consultationTypeId);
}
