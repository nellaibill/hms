using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantAppointmentSlotSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "appointment_slot_minutes",
                schema: "masters",
                table: "consultants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "patients_per_slot",
                schema: "masters",
                table: "consultants",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "appointment_slot_minutes",
                schema: "masters",
                table: "consultants");

            migrationBuilder.DropColumn(
                name: "patients_per_slot",
                schema: "masters",
                table: "consultants");
        }
    }
}
