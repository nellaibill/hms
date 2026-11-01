using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDiscountRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "discount_status",
                schema: "billing",
                table: "invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "invoice_discount_requests",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    patient_uhid = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_discount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
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
                    table.PrimaryKey("pk_invoice_discount_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_discount_requests_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "billing",
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_discount_request_lines",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    department_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    consultant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    service_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
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
                    table.PrimaryKey("pk_invoice_discount_request_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_discount_request_lines_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "billing",
                        principalTable: "invoice_discount_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_discount_request_lines_request_id",
                schema: "billing",
                table: "invoice_discount_request_lines",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_discount_requests_invoice_id",
                schema: "billing",
                table: "invoice_discount_requests",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_discount_requests_status_created_at",
                schema: "billing",
                table: "invoice_discount_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_invoice_discount_requests_invoice_id_pending",
                schema: "billing",
                table: "invoice_discount_requests",
                column: "invoice_id",
                unique: true,
                filter: "status = 'Pending' AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_discount_request_lines",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice_discount_requests",
                schema: "billing");

            migrationBuilder.DropColumn(
                name: "discount_status",
                schema: "billing",
                table: "invoices");
        }
    }
}
