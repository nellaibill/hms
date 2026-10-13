using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitNumberAndAdmissionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "ip_number_seq",
                schema: "patients");

            migrationBuilder.CreateSequence(
                name: "op_number_seq",
                schema: "patients");

            migrationBuilder.AddColumn<string>(
                name: "admission_type",
                schema: "patients",
                table: "patient_visits",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ar_number",
                schema: "patients",
                table: "patient_visits",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                schema: "patients",
                table: "patient_visits",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_referred",
                schema: "patients",
                table: "patient_visits",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "referred_by",
                schema: "patients",
                table: "patient_visits",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visit_number",
                schema: "patients",
                table: "patient_visits",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_patient_visits_visit_number",
                schema: "patients",
                table: "patient_visits",
                column: "visit_number",
                unique: true,
                filter: "visit_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_patient_visits_visit_number",
                schema: "patients",
                table: "patient_visits");

            migrationBuilder.DropColumn(
                name: "admission_type",
                schema: "patients",
                table: "patient_visits");

            migrationBuilder.DropColumn(
                name: "ar_number",
                schema: "patients",
                table: "patient_visits");

            migrationBuilder.DropColumn(
                name: "category",
                schema: "patients",
                table: "patient_visits");

            migrationBuilder.DropColumn(
                name: "is_referred",
                schema: "patients",
                table: "patient_visits");

            migrationBuilder.DropColumn(
                name: "referred_by",
                schema: "patients",
                table: "patient_visits");

            migrationBuilder.DropColumn(
                name: "visit_number",
                schema: "patients",
                table: "patient_visits");

            migrationBuilder.DropSequence(
                name: "ip_number_seq",
                schema: "patients");

            migrationBuilder.DropSequence(
                name: "op_number_seq",
                schema: "patients");
        }
    }
}
