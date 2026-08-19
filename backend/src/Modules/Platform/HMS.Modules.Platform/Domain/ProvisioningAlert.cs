namespace HMS.Modules.Platform.Domain;

/// <summary>
/// A durable record of a tenant-provisioning failure whose rollback (DROP DATABASE) also
/// failed — HMS Security Hardening: previously this only produced a log line ("manual
/// cleanup required"), so an orphaned tenant database could silently sit there with nothing
/// surfacing it to a human. Recording it here makes it show up in the Platform dashboard's
/// stat tiles (see PlatformDashboardService.GetStatsAsync) instead. Deliberately not an
/// <see cref="HMS.Shared.Kernel.Entity"/> — this is an operational record, not a business
/// aggregate with audit/soft-delete semantics, and (like IdempotencyRecord) has no
/// createdBy actor — it's raised by the system during a failure, not by a caller.
/// </summary>
internal sealed class ProvisioningAlert
{
    public Guid Id { get; private set; }
    public string DatabaseName { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    // Required by EF Core materialization.
    private ProvisioningAlert()
    {
    }

    private ProvisioningAlert(Guid id, string databaseName, string message)
    {
        Id = id;
        DatabaseName = databaseName;
        Message = message;
        CreatedAt = DateTime.UtcNow;
    }

    public static ProvisioningAlert Raise(string databaseName, string message) =>
        new(Guid.CreateVersion7(), databaseName, message);
}
