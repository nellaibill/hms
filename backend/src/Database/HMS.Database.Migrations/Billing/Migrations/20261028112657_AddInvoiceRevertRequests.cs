using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceRevertRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_reverted",
                schema: "billing",
                table: "invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "invoice_revert_requests",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    patient_uhid = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_invoice_revert_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_revert_requests_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "billing",
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_revert_requests_invoice_id",
                schema: "billing",
                table: "invoice_revert_requests",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_revert_requests_status_created_at",
                schema: "billing",
                table: "invoice_revert_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_invoice_revert_requests_invoice_id_pending",
                schema: "billing",
                table: "invoice_revert_requests",
                column: "invoice_id",
                unique: true,
                filter: "status = 'Pending' AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_revert_requests",
                schema: "billing");

            migrationBuilder.DropColumn(
                name: "is_reverted",
                schema: "billing",
                table: "invoices");
        }
    }
}
