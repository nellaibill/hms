using HMS.Modules.Sync.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>
/// Owns the "sync" schema in every hospital database (ADR-133). An Edge server uses the outbox
/// tables (outbox_events, outbox_streams, outbox_attempts, sync_state); a Cloud server uses the
/// receiver tables (inbox_events, replica_streams, conflicts). Both sets always exist so a
/// hospital database can move between roles (e.g. a cloud backup restored onto a new local
/// server) without a schema change. Migrated for every tenant unconditionally by
/// TenantMigrationService: it is infrastructure, not a feature a tenant can switch off.
/// </summary>
public class SyncDbContext : DbContext
{
    public const string SchemaName = "sync";

    public SyncDbContext(DbContextOptions<SyncDbContext> options) : base(options)
    {
    }

    internal DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    internal DbSet<OutboxStream> OutboxStreams => Set<OutboxStream>();
    internal DbSet<OutboxAttempt> OutboxAttempts => Set<OutboxAttempt>();
    internal DbSet<SyncState> SyncStates => Set<SyncState>();
    internal DbSet<InboxEvent> InboxEvents => Set<InboxEvent>();
    internal DbSet<ReplicaStream> ReplicaStreams => Set<ReplicaStream>();
    internal DbSet<SyncConflict> Conflicts => Set<SyncConflict>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SyncDbContext).Assembly,
            type => type.Namespace == typeof(Configurations.OutboxEventConfiguration).Namespace);
    }
}

/// <summary>
/// Owns the "sync_registry" schema in the platform database (hms_platform): the sync API keys a
/// Cloud server accepts. Kept apart from PlatformDbContext so the Platform module is untouched,
/// and named differently from the per-hospital "sync" schema because local development points
/// the legacy hospital at the same physical database as the platform.
/// </summary>
public class SyncRegistryDbContext : DbContext
{
    public const string SchemaName = "sync_registry";

    public SyncRegistryDbContext(DbContextOptions<SyncRegistryDbContext> options) : base(options)
    {
    }

    internal DbSet<SyncClient> Clients => Set<SyncClient>();
    internal DbSet<TenantSyncSettings> TenantSettings => Set<TenantSyncSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SyncRegistryDbContext).Assembly,
            type => type.Namespace == typeof(Configurations.Registry.SyncClientConfiguration).Namespace);
    }
}
