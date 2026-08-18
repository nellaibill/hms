using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateSequence(
                name: "invoice_no_seq",
                schema: "billing");

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    patient_uhid = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_discount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
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
                    table.PrimaryKey("pk_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line_items",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    department_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    consultant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    service_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    discount_approved = table.Column<bool>(type: "boolean", nullable: false),
                    discount_approved_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
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
                    table.PrimaryKey("pk_invoice_line_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_line_items_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "billing",
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_line_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_invoice_line_items_invoice_line_item_id",
                        column: x => x.invoice_line_item_id,
                        principalSchema: "billing",
                        principalTable: "invoice_line_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "billing",
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_line_items_invoice_id",
                schema: "billing",
                table: "invoice_line_items",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_created_at",
                schema: "billing",
                table: "invoices",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_patient_id",
                schema: "billing",
                table: "invoices",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ux_invoices_invoice_number",
                schema: "billing",
                table: "invoices",
                column: "invoice_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_payments_invoice_id",
                schema: "billing",
                table: "payments",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_invoice_line_item_id",
                schema: "billing",
                table: "payments",
                column: "invoice_line_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice_line_items",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "billing");

            migrationBuilder.DropSequence(
                name: "invoice_no_seq",
                schema: "billing");
        }
    }
}
