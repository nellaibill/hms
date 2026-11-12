using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.SyncRegistry.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateSyncRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sync_registry");

            migrationBuilder.CreateTable(
                name: "sync_clients",
                schema: "sync_registry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_settings",
                schema: "sync_registry",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    patients_owned_by_edge = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_settings", x => x.tenant_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sync_clients_tenant_id",
                schema: "sync_registry",
                table: "sync_clients",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_sync_clients_key_hash",
                schema: "sync_registry",
                table: "sync_clients",
                column: "key_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_clients",
                schema: "sync_registry");

            migrationBuilder.DropTable(
                name: "tenant_settings",
                schema: "sync_registry");
        }
    }
}
