using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.IPD.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionVisitId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "visit_id",
                schema: "ipd",
                table: "admissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_admissions_visit_id",
                schema: "ipd",
                table: "admissions",
                column: "visit_id",
                unique: true,
                filter: "visit_id IS NOT NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_admissions_visit_id",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.DropColumn(
                name: "visit_id",
                schema: "ipd",
                table: "admissions");
        }
    }
}
