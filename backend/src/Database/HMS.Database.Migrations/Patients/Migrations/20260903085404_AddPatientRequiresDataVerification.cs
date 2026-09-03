using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientRequiresDataVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requires_data_verification",
                schema: "patients",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_patients_requires_data_verification",
                schema: "patients",
                table: "patients",
                column: "requires_data_verification",
                filter: "requires_data_verification = true");

            // Retroactively flags every patient ever created via bulk import (including a
            // batch already committed before this migration existed) — patient_import_rows
            // already links each Created row to the patient it produced, so this needs no
            // separate backfill data source. Safe to re-run: idempotent, only ever sets true.
            migrationBuilder.Sql(
                """
                UPDATE patients.patients
                SET requires_data_verification = true
                WHERE id IN (
                    SELECT created_patient_id
                    FROM patients.patient_import_rows
                    WHERE created_patient_id IS NOT NULL
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patients_requires_data_verification",
                schema: "patients",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "requires_data_verification",
                schema: "patients",
                table: "patients");
        }
    }
}
