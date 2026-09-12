using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantTypeAndAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "available_days",
                schema: "masters",
                table: "consultants",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "consultant_type",
                schema: "masters",
                table: "consultants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "visit_end_time",
                schema: "masters",
                table: "consultants",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "visit_start_time",
                schema: "masters",
                table: "consultants",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_days",
                schema: "masters",
                table: "consultants");

            migrationBuilder.DropColumn(
                name: "consultant_type",
                schema: "masters",
                table: "consultants");

            migrationBuilder.DropColumn(
                name: "visit_end_time",
                schema: "masters",
                table: "consultants");

            migrationBuilder.DropColumn(
                name: "visit_start_time",
                schema: "masters",
                table: "consultants");
        }
    }
}
