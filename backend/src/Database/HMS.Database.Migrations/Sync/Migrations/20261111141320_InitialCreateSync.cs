using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Sync.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sync");

            migrationBuilder.CreateTable(
                name: "conflicts",
                schema: "sync",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    local_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    cloud_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_event_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conflicts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_events",
                schema: "sync",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    operation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resolution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_node_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_events", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_attempts",
                schema: "sync",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_no = table.Column<int>(type: "integer", nullable: false),
                    worker_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    http_status = table.Column<int>(type: "integer", nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_events",
                schema: "sync",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    payload_schema_version = table.Column<int>(type: "integer", nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    changed_fields = table.Column<string[]>(type: "text[]", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    resolution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cloud_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cloud_processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    conflict_snapshot = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_streams",
                schema: "sync",
                columns: table => new
                {
                    stream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false),
                    last_synced_sequence = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lease_owner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lease_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_streams", x => x.stream_id);
                });

            migrationBuilder.CreateTable(
                name: "replica_streams",
                schema: "sync",
                columns: table => new
                {
                    stream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_applied_sequence = table.Column<long>(type: "bigint", nullable: false),
                    last_applied_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_epoch = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_version_stamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_replica_streams", x => x.stream_id);
                });

            migrationBuilder.CreateTable(
                name: "sync_state",
                schema: "sync",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    outbox_epoch = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    paused_by = table.Column<Guid>(type: "uuid", nullable: true),
                    paused_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    auth_failed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cloud_reachable = table.Column<bool>(type: "boolean", nullable: true),
                    last_probe_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_probe_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_probe_latency_ms = table.Column<int>(type: "integer", nullable: true),
                    last_successful_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_state", x => x.id);
                    table.CheckConstraint("ck_sync_state_singleton", "id = 1");
                });

            migrationBuilder.CreateIndex(
                name: "ix_conflicts_event_id",
                schema: "sync",
                table: "conflicts",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_conflicts_stream_id_status",
                schema: "sync",
                table: "conflicts",
                columns: new[] { "stream_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inbox_events_stream_id_sequence_number",
                schema: "sync",
                table: "inbox_events",
                columns: new[] { "stream_id", "sequence_number" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_attempts_event_id",
                schema: "sync",
                table: "outbox_attempts",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_attempts_finished_at",
                schema: "sync",
                table: "outbox_attempts",
                column: "finished_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_status_next_retry_at",
                schema: "sync",
                table: "outbox_events",
                columns: new[] { "status", "next_retry_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_synced_at",
                schema: "sync",
                table: "outbox_events",
                column: "synced_at",
                filter: "status = 'Synced'");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_unsynced",
                schema: "sync",
                table: "outbox_events",
                columns: new[] { "stream_id", "sequence_number" },
                unique: true,
                filter: "status <> 'Synced'");

            migrationBuilder.CreateIndex(
                name: "ux_outbox_events_event_id",
                schema: "sync",
                table: "outbox_events",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_streams_claimable",
                schema: "sync",
                table: "outbox_streams",
                columns: new[] { "next_attempt_at", "stream_id" },
                filter: "last_synced_sequence < last_sequence");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_streams_status",
                schema: "sync",
                table: "outbox_streams",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conflicts",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "inbox_events",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "outbox_attempts",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "outbox_events",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "outbox_streams",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "replica_streams",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "sync_state",
                schema: "sync");
        }
    }
}
