using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantSecondVisitingSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "visit_end_time_2",
                schema: "masters",
                table: "consultants",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "visit_start_time_2",
                schema: "masters",
                table: "consultants",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "visit_end_time_2",
                schema: "masters",
                table: "consultants");

            migrationBuilder.DropColumn(
                name: "visit_start_time_2",
                schema: "masters",
                table: "consultants");
        }
    }
}
