using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.IPD.Migrations
{
    /// <inheritdoc />
    public partial class AddNursingAssessmentsAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nursing_assessments",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    general_condition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    consciousness_level = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    mobility = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    nutrition_status = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fall_risk = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pressure_sore_risk = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    skin_condition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pain_score = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    assessed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_nursing_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_nursing_assessments_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nursing_notes",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    intervention = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    patient_response = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_nursing_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_nursing_notes_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_nursing_assessments_admission_id",
                schema: "ipd",
                table: "nursing_assessments",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_nursing_notes_admission_id",
                schema: "ipd",
                table: "nursing_notes",
                column: "admission_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nursing_assessments",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "nursing_notes",
                schema: "ipd");
        }
    }
}
