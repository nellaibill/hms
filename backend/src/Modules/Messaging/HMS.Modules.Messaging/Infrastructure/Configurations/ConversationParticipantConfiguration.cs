using HMS.Modules.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Messaging.Infrastructure.Configurations;

internal class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("conversation_participants");

        builder.HasKey(p => p.Id).HasName("pk_conversation_participants");
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.ConversationId).HasColumnName("conversation_id").IsRequired();

        // No FK constraint to identity.users — cross-schema references are a deliberate,
        // reviewed exception (docs/DatabaseArchitecture.md §7), not a default; mirrors
        // HMS.Modules.Notifications' identical treatment of UserId.
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(p => p.JoinedAt).HasColumnName("joined_at").IsRequired();
        builder.Property(p => p.LastReadAt).HasColumnName("last_read_at");

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(p => p.ConversationId)
            .HasConstraintName("fk_conversation_participants_conversations_conversation_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Membership is also the authorization boundary (see ConversationParticipant's doc
        // comment), so this pair must be unique — the same user can't join twice.
        builder.HasIndex(p => new { p.ConversationId, p.UserId })
            .IsUnique()
            .HasDatabaseName("ux_conversation_participants_conversation_user")
            .HasFilter("is_deleted = false");

        // "My conversations" — every list of conversations a user belongs to.
        builder.HasIndex(p => p.UserId).HasDatabaseName("ix_conversation_participants_user_id");
    }
}
