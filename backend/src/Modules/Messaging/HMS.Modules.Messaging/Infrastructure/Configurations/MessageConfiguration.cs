using HMS.Modules.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Messaging.Infrastructure.Configurations;

internal class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id).HasName("pk_messages");
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.ConversationId).HasColumnName("conversation_id").IsRequired();

        // No FK constraint to identity.users — see ConversationParticipantConfiguration's
        // identical reasoning for UserId.
        builder.Property(m => m.SenderId).HasColumnName("sender_id").IsRequired();

        builder.Property(m => m.Body).HasColumnName("body").HasMaxLength(4000).IsRequired();

        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        builder.Property(m => m.UpdatedBy).HasColumnName("updated_by");
        builder.Property(m => m.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");
        builder.Property(m => m.DeletedBy).HasColumnName("deleted_by");

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .HasConstraintName("fk_messages_conversations_conversation_id")
            .OnDelete(DeleteBehavior.Restrict);

        // The paged-history query: one conversation's messages, oldest-to-newest.
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt }).HasDatabaseName("ix_messages_conversation_created_at");
    }
}
