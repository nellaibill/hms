using HMS.Modules.Sync.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Sync.Infrastructure.Configurations;

internal sealed class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("outbox_events");

        builder.HasKey(e => e.Id).HasName("pk_outbox_events");
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(e => e.StreamId).HasColumnName("stream_id").IsRequired();
        builder.Property(e => e.AggregateType).HasColumnName("aggregate_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.AggregateId).HasColumnName("aggregate_id").IsRequired();
        builder.Property(e => e.OperationType).HasColumnName("operation_type").HasMaxLength(20).IsRequired();
        builder.Property(e => e.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.PayloadSchemaVersion).HasColumnName("payload_schema_version").IsRequired();
        builder.Property(e => e.PayloadHash).HasColumnName("payload_hash").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ChangedFields).HasColumnName("changed_fields").HasColumnType("text[]").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.RetryCount).HasColumnName("retry_count").IsRequired().HasDefaultValue(0);
        builder.Property(e => e.Resolution).HasColumnName("resolution").HasMaxLength(20);
        builder.Property(e => e.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(500);
        builder.Property(e => e.ResolvedBy).HasColumnName("resolved_by");
        builder.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.LastAttemptAt).HasColumnName("last_attempt_at");
        builder.Property(e => e.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(e => e.SyncedAt).HasColumnName("synced_at");
        builder.Property(e => e.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
        builder.Property(e => e.CloudReceiptId).HasColumnName("cloud_receipt_id");
        builder.Property(e => e.CloudProcessedAt).HasColumnName("cloud_processed_at");
        builder.Property(e => e.ConflictSnapshot).HasColumnName("conflict_snapshot").HasColumnType("jsonb");

        builder.HasIndex(e => e.EventId).IsUnique().HasDatabaseName("ux_outbox_events_event_id");

        // Gap-free per-patient ordering is enforced by the database, not just by the allocator.
        builder.HasIndex(e => new { e.StreamId, e.SequenceNumber }).IsUnique().HasDatabaseName("ux_outbox_events_stream_id_sequence_number");

        builder.HasIndex(e => new { e.Status, e.NextRetryAt }).HasDatabaseName("ix_outbox_events_status_next_retry_at");

        // The sender only ever reads unsynced rows; synced rows (the vast majority) stay out of
        // this index.
        builder.HasIndex(e => new { e.StreamId, e.SequenceNumber })
            .HasDatabaseName("ix_outbox_events_unsynced")
            .HasFilter("status <> 'Synced'");

        // Retention cleanup.
        builder.HasIndex(e => e.SyncedAt)
            .HasDatabaseName("ix_outbox_events_synced_at")
            .HasFilter("status = 'Synced'");
    }
}

internal sealed class OutboxStreamConfiguration : IEntityTypeConfiguration<OutboxStream>
{
    public void Configure(EntityTypeBuilder<OutboxStream> builder)
    {
        builder.ToTable("outbox_streams");

        builder.HasKey(s => s.StreamId).HasName("pk_outbox_streams");
        builder.Property(s => s.StreamId).HasColumnName("stream_id").ValueGeneratedNever();
        builder.Property(s => s.AggregateType).HasColumnName("aggregate_type").HasMaxLength(50).IsRequired();
        builder.Property(s => s.LastSequence).HasColumnName("last_sequence").IsRequired();
        builder.Property(s => s.LastSyncedSequence).HasColumnName("last_synced_sequence").IsRequired().HasDefaultValue(0L);
        builder.Property(s => s.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(s => s.RetryCount).HasColumnName("retry_count").IsRequired().HasDefaultValue(0);
        builder.Property(s => s.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(s => s.LastAttemptAt).HasColumnName("last_attempt_at");
        builder.Property(s => s.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(100);
        builder.Property(s => s.LeaseOwner).HasColumnName("lease_owner").HasMaxLength(100);
        builder.Property(s => s.LeaseExpiresAt).HasColumnName("lease_expires_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()");

        // The claim query's candidate set: streams that still have unsynced events.
        builder.HasIndex(s => new { s.NextAttemptAt, s.StreamId })
            .HasDatabaseName("ix_outbox_streams_claimable")
            .HasFilter("last_synced_sequence < last_sequence");

        builder.HasIndex(s => s.Status).HasDatabaseName("ix_outbox_streams_status");
    }
}

internal sealed class OutboxAttemptConfiguration : IEntityTypeConfiguration<OutboxAttempt>
{
    public void Configure(EntityTypeBuilder<OutboxAttempt> builder)
    {
        builder.ToTable("outbox_attempts");

        builder.HasKey(a => a.Id).HasName("pk_outbox_attempts");
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(a => a.StreamId).HasColumnName("stream_id").IsRequired();
        builder.Property(a => a.AttemptNo).HasColumnName("attempt_no").IsRequired();
        builder.Property(a => a.WorkerId).HasColumnName("worker_id").HasMaxLength(100).IsRequired();
        builder.Property(a => a.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(a => a.FinishedAt).HasColumnName("finished_at").IsRequired();
        builder.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        builder.Property(a => a.HttpStatus).HasColumnName("http_status");
        builder.Property(a => a.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(a => a.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
        builder.Property(a => a.DurationMs).HasColumnName("duration_ms").IsRequired();

        builder.HasIndex(a => a.EventId).HasDatabaseName("ix_outbox_attempts_event_id");
        builder.HasIndex(a => a.FinishedAt).HasDatabaseName("ix_outbox_attempts_finished_at");
    }
}

internal sealed class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> builder)
    {
        builder.ToTable("sync_state", t => t.HasCheckConstraint("ck_sync_state_singleton", "id = 1"));

        builder.HasKey(s => s.Id).HasName("pk_sync_state");
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.OutboxEpoch).HasColumnName("outbox_epoch").IsRequired().HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.IsPaused).HasColumnName("is_paused").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.PausedBy).HasColumnName("paused_by");
        builder.Property(s => s.PausedAt).HasColumnName("paused_at");
        builder.Property(s => s.AuthFailed).HasColumnName("auth_failed").IsRequired().HasDefaultValue(false);
        builder.Property(s => s.CloudReachable).HasColumnName("cloud_reachable");
        builder.Property(s => s.LastProbeAt).HasColumnName("last_probe_at");
        builder.Property(s => s.LastProbeError).HasColumnName("last_probe_error").HasMaxLength(500);
        builder.Property(s => s.LastProbeLatencyMs).HasColumnName("last_probe_latency_ms");
        builder.Property(s => s.LastSuccessfulSyncAt).HasColumnName("last_successful_sync_at");
        builder.Property(s => s.LastRunAt).HasColumnName("last_run_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()");
    }
}

internal sealed class InboxEventConfiguration : IEntityTypeConfiguration<InboxEvent>
{
    public void Configure(EntityTypeBuilder<InboxEvent> builder)
    {
        builder.ToTable("inbox_events");

        builder.HasKey(e => e.EventId).HasName("pk_inbox_events");
        builder.Property(e => e.EventId).HasColumnName("event_id").ValueGeneratedNever();
        builder.Property(e => e.StreamId).HasColumnName("stream_id").IsRequired();
        builder.Property(e => e.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(e => e.AggregateType).HasColumnName("aggregate_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.OperationType).HasColumnName("operation_type").HasMaxLength(20).IsRequired();
        builder.Property(e => e.PayloadHash).HasColumnName("payload_hash").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Resolution).HasColumnName("resolution").HasMaxLength(20);
        builder.Property(e => e.ReceiptId).HasColumnName("receipt_id").IsRequired();
        builder.Property(e => e.SourceNodeId).HasColumnName("source_node_id").HasMaxLength(100);
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at").IsRequired();

        builder.HasIndex(e => new { e.StreamId, e.SequenceNumber }).HasDatabaseName("ix_inbox_events_stream_id_sequence_number");
    }
}

internal sealed class ReplicaStreamConfiguration : IEntityTypeConfiguration<ReplicaStream>
{
    public void Configure(EntityTypeBuilder<ReplicaStream> builder)
    {
        builder.ToTable("replica_streams");

        builder.HasKey(s => s.StreamId).HasName("pk_replica_streams");
        builder.Property(s => s.StreamId).HasColumnName("stream_id").ValueGeneratedNever();
        builder.Property(s => s.AggregateType).HasColumnName("aggregate_type").HasMaxLength(50).IsRequired();
        builder.Property(s => s.LastAppliedSequence).HasColumnName("last_applied_sequence").IsRequired();
        builder.Property(s => s.LastAppliedEventId).HasColumnName("last_applied_event_id").IsRequired();
        builder.Property(s => s.SourceEpoch).HasColumnName("source_epoch");
        builder.Property(s => s.AppliedVersionStamp).HasColumnName("applied_version_stamp").HasMaxLength(64);
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}

internal sealed class SyncConflictConfiguration : IEntityTypeConfiguration<SyncConflict>
{
    public void Configure(EntityTypeBuilder<SyncConflict> builder)
    {
        builder.ToTable("conflicts");

        builder.HasKey(c => c.Id).HasName("pk_conflicts");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.StreamId).HasColumnName("stream_id").IsRequired();
        builder.Property(c => c.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(c => c.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(c => c.LocalSnapshot).HasColumnName("local_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(c => c.CloudSnapshot).HasColumnName("cloud_snapshot").HasColumnType("jsonb");
        builder.Property(c => c.DetectedAt).HasColumnName("detected_at").IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(c => c.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(c => c.ResolutionEventId).HasColumnName("resolution_event_id");

        builder.HasIndex(c => new { c.StreamId, c.Status }).HasDatabaseName("ix_conflicts_stream_id_status");
        builder.HasIndex(c => c.EventId).HasDatabaseName("ix_conflicts_event_id");
    }
}
