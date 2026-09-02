using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientImportBatchesAndRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "patient_import_batches",
                schema: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    valid_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    invalid_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    commit_failed_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    committed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    committed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_patient_import_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patient_import_rows",
                schema: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    raw_data = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    errors = table.Column<string>(type: "jsonb", nullable: true),
                    mapped_request = table.Column<string>(type: "jsonb", nullable: true),
                    created_patient_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_patient_import_rows", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_import_rows_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "patients",
                        principalTable: "patient_import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patient_import_batches_created_at",
                schema: "patients",
                table: "patient_import_batches",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_patient_import_rows_batch_id_status",
                schema: "patients",
                table: "patient_import_rows",
                columns: new[] { "batch_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_import_rows",
                schema: "patients");

            migrationBuilder.DropTable(
                name: "patient_import_batches",
                schema: "patients");
        }
    }
}
