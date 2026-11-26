using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListSheetOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                schema: "masters",
                table: "diagnostic_tests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sample_type",
                schema: "masters",
                table: "diagnostic_services",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                schema: "masters",
                table: "diagnostic_services",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                schema: "masters",
                table: "diagnostic_packages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "masters",
                table: "diagnostic_tests");

            migrationBuilder.DropColumn(
                name: "sample_type",
                schema: "masters",
                table: "diagnostic_services");

            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "masters",
                table: "diagnostic_services");

            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "masters",
                table: "diagnostic_packages");
        }
    }
}
