using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.HR.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftSwapRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_swap_requests",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_to_staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_shift_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_shift_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_shift_swap_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_requests_current_shift_assignment_id",
                schema: "hr",
                table: "shift_swap_requests",
                column: "current_shift_assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_requests_requested_by_staff_id",
                schema: "hr",
                table: "shift_swap_requests",
                column: "requested_by_staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_requests_requested_shift_assignment_id",
                schema: "hr",
                table: "shift_swap_requests",
                column: "requested_shift_assignment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shift_swap_requests",
                schema: "hr");
        }
    }
}
