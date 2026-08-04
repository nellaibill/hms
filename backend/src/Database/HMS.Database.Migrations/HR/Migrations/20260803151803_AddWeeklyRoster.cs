using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.HR.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyRoster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weekly_rosters",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    week_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    published_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_rosters", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_weekly_rosters_department_id",
                schema: "hr",
                table: "weekly_rosters",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_rosters_week_start_date",
                schema: "hr",
                table: "weekly_rosters",
                column: "week_start_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weekly_rosters",
                schema: "hr");
        }
    }
}
