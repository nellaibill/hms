using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCodeFromAppointmentTypeConsultantConsultationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_consultation_types_code",
                schema: "masters",
                table: "consultation_types");

            migrationBuilder.DropIndex(
                name: "ux_consultants_code",
                schema: "masters",
                table: "consultants");

            migrationBuilder.DropIndex(
                name: "ux_appointment_types_code",
                schema: "masters",
                table: "appointment_types");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "masters",
                table: "consultation_types");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "masters",
                table: "consultants");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "masters",
                table: "appointment_types");

            migrationBuilder.CreateIndex(
                name: "ux_consultation_types_name",
                schema: "masters",
                table: "consultation_types",
                column: "name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_appointment_types_name",
                schema: "masters",
                table: "appointment_types",
                column: "name",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_consultation_types_name",
                schema: "masters",
                table: "consultation_types");

            migrationBuilder.DropIndex(
                name: "ux_appointment_types_name",
                schema: "masters",
                table: "appointment_types");

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "masters",
                table: "consultation_types",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "masters",
                table: "consultants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "masters",
                table: "appointment_types",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_consultation_types_code",
                schema: "masters",
                table: "consultation_types",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_consultants_code",
                schema: "masters",
                table: "consultants",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_appointment_types_code",
                schema: "masters",
                table: "appointment_types",
                column: "code",
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
