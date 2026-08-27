using HMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Notifications.Infrastructure;

/// <summary>
/// Owns the "notifications" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only
/// this module's own code constructs/migrates this context — no other module references it.
/// </summary>
public class NotificationsDbContext : DbContext
{
    public const string SchemaName = "notifications";

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    // Internal (not public): every entity here is an internal domain type, so a public
    // DbSet<T> property would be a CS0053 accessibility violation. The context itself stays
    // public (HMS.Api's Program.cs resolves it by type for the startup migration call), but
    // these DbSets are only ever queried from within this module's repositories.
    internal DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    internal DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    internal DbSet<Notification> Notifications => Set<Notification>();

    internal DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();

    internal DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
