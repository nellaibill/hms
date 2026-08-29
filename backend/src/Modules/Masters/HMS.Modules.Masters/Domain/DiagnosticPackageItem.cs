namespace HMS.Modules.Masters.Domain;

/// <summary>
/// One test on a <see cref="DiagnosticPackage"/> — a genuine 1:many child (a package bundles
/// several tests), not a flattened field, same shape as PatientVisitConsultation on
/// PatientVisit. ServiceId is an app-level reference into DiagnosticService — validated by
/// DiagnosticPackageService before this is created, not enforced by a database foreign key
/// (cross-entity references inside Masters still stay app-level-only, same convention
/// DiagnosticService itself uses for CategoryId/ProviderId). PackageId, by contrast, is a real
/// DB foreign key with cascade delete — it points at this item's own aggregate root, not
/// across a module/entity boundary.
/// </summary>
internal class DiagnosticPackageItem
{
    public Guid Id { get; private set; }
    public Guid PackageId { get; private set; }
    public Guid ServiceId { get; private set; }

    // Required by EF Core materialization.
    private DiagnosticPackageItem()
    {
    }

    private DiagnosticPackageItem(Guid id, Guid packageId, Guid serviceId)
    {
        Id = id;
        PackageId = packageId;
        ServiceId = serviceId;
    }

    public static DiagnosticPackageItem Create(Guid packageId, Guid serviceId)
        => new(Guid.CreateVersion7(), packageId, serviceId);
}
