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

            // The two AddColumn calls above stamp every pre-existing row with a placeholder
            // (year-1 appointment_time, empty status) just to satisfy the new NOT NULL
            // constraints — neither is a value the application ever produces itself (Create
            // always sets a real AppointmentTime and defaults Status to Waiting), and an empty
            // string doesn't match any OpdConsultationStatus name, so leaving it as-is would
            // both hide every pre-existing consultation from any OPD date-range query (its
            // appointment_time can never fall in a real range) and throw when EF tries to
            // convert the empty status string back to the enum on read. Backfill both to real,
            // sensible values: the parent visit's own CreatedAt (the closest real timestamp to
            // when that consultation actually happened) and Completed (a past encounter has, by
            // definition, already happened — not sitting in a live Waiting/CheckedIn queue).
            // Safe to re-run: idempotent, only ever touches rows still holding the placeholder.
            migrationBuilder.Sql(
                """
                UPDATE patients.patient_visit_consultations c
                SET appointment_time = v.created_at
                FROM patients.patient_visits v
                WHERE c.visit_id = v.visit_id
                  AND c.appointment_time < '1900-01-01'::timestamptz;

                UPDATE patients.patient_visit_consultations
                SET status = 'Completed'
                WHERE status = '';
                """);
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
