using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.IPD.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalsAndProgressNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "progress_notes",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    clinical_condition = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    progress = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    diagnosis = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    assessment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    plan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_progress_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_progress_notes_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vitals_readings",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    temperature_f = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    pulse_rate = table.Column<int>(type: "integer", nullable: true),
                    respiratory_rate = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure_systolic = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure_diastolic = table.Column<int>(type: "integer", nullable: true),
                    spo2_percent = table.Column<int>(type: "integer", nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    height_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    pain_score = table.Column<int>(type: "integer", nullable: true),
                    blood_glucose_mg_dl = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_vitals_readings", x => x.id);
                    table.ForeignKey(
                        name: "fk_vitals_readings_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_progress_notes_admission_id",
                schema: "ipd",
                table: "progress_notes",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_vitals_readings_admission_id",
                schema: "ipd",
                table: "vitals_readings",
                column: "admission_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "progress_notes",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "vitals_readings",
                schema: "ipd");
        }
    }
}
