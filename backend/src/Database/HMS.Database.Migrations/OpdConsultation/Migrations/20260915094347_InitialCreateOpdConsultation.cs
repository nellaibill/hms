using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.OpdConsultation.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateOpdConsultation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "opd_consultation");

            migrationBuilder.CreateTable(
                name: "opd_consultation_notes",
                schema: "opd_consultation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    consultation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    height_cm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    pulse_rate = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    temperature_f = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    spo2_percent = table.Column<int>(type: "integer", nullable: true),
                    presenting_complaints = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    clinical_history = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    examination_findings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    plan_of_management = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    review_date = table.Column<DateOnly>(type: "date", nullable: true),
                    follow_up_instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    emergency_review_instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    referral_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    referral_consultant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    referral_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_opd_consultation_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "opd_consultation_diagnoses",
                schema: "opd_consultation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    opd_consultation_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnosis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opd_consultation_diagnoses", x => x.id);
                    table.ForeignKey(
                        name: "fk_opd_consultation_diagnoses_note_id",
                        column: x => x.opd_consultation_note_id,
                        principalSchema: "opd_consultation",
                        principalTable: "opd_consultation_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opd_consultation_investigations",
                schema: "opd_consultation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    opd_consultation_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    department = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opd_consultation_investigations", x => x.id);
                    table.ForeignKey(
                        name: "fk_opd_consultation_investigations_note_id",
                        column: x => x.opd_consultation_note_id,
                        principalSchema: "opd_consultation",
                        principalTable: "opd_consultation_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_opd_consultation_diagnoses_diagnosis_id",
                schema: "opd_consultation",
                table: "opd_consultation_diagnoses",
                column: "diagnosis_id");

            migrationBuilder.CreateIndex(
                name: "ix_opd_consultation_diagnoses_note_id",
                schema: "opd_consultation",
                table: "opd_consultation_diagnoses",
                column: "opd_consultation_note_id");

            migrationBuilder.CreateIndex(
                name: "ix_opd_consultation_investigations_note_id",
                schema: "opd_consultation",
                table: "opd_consultation_investigations",
                column: "opd_consultation_note_id");

            migrationBuilder.CreateIndex(
                name: "ix_opd_consultation_notes_patient_id",
                schema: "opd_consultation",
                table: "opd_consultation_notes",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ux_opd_consultation_notes_consultation_id",
                schema: "opd_consultation",
                table: "opd_consultation_notes",
                column: "consultation_id",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opd_consultation_diagnoses",
                schema: "opd_consultation");

            migrationBuilder.DropTable(
                name: "opd_consultation_investigations",
                schema: "opd_consultation");

            migrationBuilder.DropTable(
                name: "opd_consultation_notes",
                schema: "opd_consultation");
        }
    }
}
