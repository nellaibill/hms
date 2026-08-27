using HMS.Modules.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Messaging.Infrastructure;

/// <summary>
/// Owns the "messaging" PostgreSQL schema. Per docs/DatabaseArchitecture.md §1, only this
/// module's own code constructs/migrates this context — no other module references it.
/// </summary>
public class MessagingDbContext : DbContext
{
    public const string SchemaName = "messaging";

    public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options)
    {
    }

    // Internal (not public): every entity here is an internal domain type, so a public
    // DbSet<T> property would be a CS0053 accessibility violation. The context itself stays
    // public (HMS.Api's Program.cs resolves it by type for the startup migration call), but
    // these DbSets are only ever queried from within this module's repositories.
    internal DbSet<Conversation> Conversations => Set<Conversation>();

    internal DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();

    internal DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessagingDbContext).Assembly);
    }
}
