using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.DischargeSummary.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateDischargeSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "discharge_summary");

            migrationBuilder.CreateTable(
                name: "discharge_summaries",
                schema: "discharge_summary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    final_diagnosis = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    prepared_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    checked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consultant_approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finalized_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discharge_summaries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discharge_summaries_patient_id",
                schema: "discharge_summary",
                table: "discharge_summaries",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ux_discharge_summaries_admission_id",
                schema: "discharge_summary",
                table: "discharge_summaries",
                column: "admission_id",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discharge_summaries",
                schema: "discharge_summary");
        }
    }
}
