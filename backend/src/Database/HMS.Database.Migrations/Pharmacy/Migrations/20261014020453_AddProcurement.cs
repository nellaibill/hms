using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Pharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pharmacy_purchase_orders",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_no = table.Column<string>(type: "text", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_purchase_orders_pharmacy_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_purchase_returns",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_no = table.Column<string>(type: "text", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    credit_note_no = table.Column<string>(type: "text", nullable: true),
                    taxable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    round_off = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_purchase_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_purchase_returns_pharmacy_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_supplier_payments",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_no = table.Column<string>(type: "text", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference_no = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_pharmacy_supplier_payments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_purchase_order_lines",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    expected_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gst_rate_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_purchase_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_purchase_order_lines_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_purchase_return_lines",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_return_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "text", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gst_rate_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    taxable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_purchase_return_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_purchase_return_lines_purchase_return_id",
                        column: x => x.purchase_return_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_purchase_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_purchase_order_lines_product_id",
                schema: "pharmacy",
                table: "pharmacy_purchase_order_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_purchase_order_lines_purchase_order_id",
                schema: "pharmacy",
                table: "pharmacy_purchase_order_lines",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_purchase_orders_status",
                schema: "pharmacy",
                table: "pharmacy_purchase_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_purchase_orders_store_id",
                schema: "pharmacy",
                table: "pharmacy_purchase_orders",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_purchase_orders_supplier_id",
                schema: "pharmacy",
                table: "pharmacy_purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_purchase_orders_order_no",
                schema: "pharmacy",
                table: "pharmacy_purchase_orders",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_purchase_return_lines_purchase_return_id",
                schema: "pharmacy",
                table: "pharmacy_purchase_return_lines",
                column: "purchase_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_purchase_returns_return_date",
                schema: "pharmacy",
                table: "pharmacy_purchase_returns",
                column: "return_date");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_purchase_returns_store_id",
                schema: "pharmacy",
                table: "pharmacy_purchase_returns",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_purchase_returns_supplier_id",
                schema: "pharmacy",
                table: "pharmacy_purchase_returns",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_purchase_returns_return_no",
                schema: "pharmacy",
                table: "pharmacy_purchase_returns",
                column: "return_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_supplier_payments_supplier_id",
                schema: "pharmacy",
                table: "pharmacy_supplier_payments",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_supplier_payments_payment_no",
                schema: "pharmacy",
                table: "pharmacy_supplier_payments",
                column: "payment_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pharmacy_purchase_order_lines",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_purchase_return_lines",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_supplier_payments",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_purchase_orders",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_purchase_returns",
                schema: "pharmacy");
        }
    }
}
