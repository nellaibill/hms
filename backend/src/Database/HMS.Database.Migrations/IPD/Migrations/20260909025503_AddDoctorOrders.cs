using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.IPD.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "doctor_orders",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ordered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ordered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_doctor_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_doctor_orders_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_doctor_orders_admission_id",
                schema: "ipd",
                table: "doctor_orders",
                column: "admission_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "doctor_orders",
                schema: "ipd");
        }
    }
}
