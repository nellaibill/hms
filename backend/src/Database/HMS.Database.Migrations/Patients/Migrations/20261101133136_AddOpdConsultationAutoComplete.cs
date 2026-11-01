using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddOpdConsultationAutoComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "auto_completed_at",
                schema: "patients",
                table: "patient_visit_consultations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "auto_completed_from",
                schema: "patients",
                table: "patient_visit_consultations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_visit_consultations_status_appointment_time",
                schema: "patients",
                table: "patient_visit_consultations",
                columns: new[] { "status", "appointment_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patient_visit_consultations_status_appointment_time",
                schema: "patients",
                table: "patient_visit_consultations");

            migrationBuilder.DropColumn(
                name: "auto_completed_at",
                schema: "patients",
                table: "patient_visit_consultations");

            migrationBuilder.DropColumn(
                name: "auto_completed_from",
                schema: "patients",
                table: "patient_visit_consultations");
        }
    }
}
