using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantImportedPatientCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "imported_patient_capacity",
                schema: "platform",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 40000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "imported_patient_capacity",
                schema: "platform",
                table: "tenants");
        }
    }
}
