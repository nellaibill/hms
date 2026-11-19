using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiologyDoctorShareRateOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "bedside_doctor_charge",
                schema: "masters",
                table: "diagnostic_services",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "bedside_price",
                schema: "masters",
                table: "diagnostic_services",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "doctor_charge",
                schema: "masters",
                table: "diagnostic_services",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "diagnostic_service_rate_options",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnostic_service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    doctor_charge = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    cost_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnostic_service_rate_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_diagnostic_service_rate_options_diagnostic_service_id",
                        column: x => x.diagnostic_service_id,
                        principalSchema: "masters",
                        principalTable: "diagnostic_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_diagnostic_service_rate_options_diagnostic_service_id",
                schema: "masters",
                table: "diagnostic_service_rate_options",
                column: "diagnostic_service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diagnostic_service_rate_options",
                schema: "masters");

            migrationBuilder.DropColumn(
                name: "bedside_doctor_charge",
                schema: "masters",
                table: "diagnostic_services");

            migrationBuilder.DropColumn(
                name: "bedside_price",
                schema: "masters",
                table: "diagnostic_services");

            migrationBuilder.DropColumn(
                name: "doctor_charge",
                schema: "masters",
                table: "diagnostic_services");
        }
    }
}
