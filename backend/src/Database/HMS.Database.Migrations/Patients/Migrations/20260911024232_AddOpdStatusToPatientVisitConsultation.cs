using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddOpdStatusToPatientVisitConsultation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "appointment_time",
                schema: "patients",
                table: "patient_visit_consultations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "patients",
                table: "patient_visit_consultations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_patient_visit_consultations_appointment_time",
                schema: "patients",
                table: "patient_visit_consultations",
                column: "appointment_time");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patient_visit_consultations_appointment_time",
                schema: "patients",
                table: "patient_visit_consultations");

            migrationBuilder.DropColumn(
                name: "appointment_time",
                schema: "patients",
                table: "patient_visit_consultations");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "patients",
                table: "patient_visit_consultations");
        }
    }
}
