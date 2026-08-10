using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentConsultantIdToPatientRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consultant",
                schema: "patients",
                table: "patient_registrations");

            migrationBuilder.DropColumn(
                name: "department",
                schema: "patients",
                table: "patient_registrations");

            migrationBuilder.AddColumn<Guid>(
                name: "consultant_id",
                schema: "patients",
                table: "patient_registrations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                schema: "patients",
                table: "patient_registrations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consultant_id",
                schema: "patients",
                table: "patient_registrations");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "patients",
                table: "patient_registrations");

            migrationBuilder.AddColumn<string>(
                name: "consultant",
                schema: "patients",
                table: "patient_registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "department",
                schema: "patients",
                table: "patient_registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
