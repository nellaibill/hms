using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Pharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptsAndDocumentSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "adjustment_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "goods_receipt_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "indent_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "purchase_order_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "purchase_return_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "sale_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "sale_return_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "stock_count_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "supplier_payment_no_seq",
                schema: "pharmacy");

            migrationBuilder.CreateSequence(
                name: "transfer_no_seq",
                schema: "pharmacy");

            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_no",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "pharmacy_goods_receipts",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_no = table.Column<string>(type: "text", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: false),
                    supplier_invoice_no = table.Column<string>(type: "text", nullable: false),
                    supplier_invoice_date = table.Column<DateOnly>(type: "date", nullable: true),
                    received_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_inter_state = table.Column<bool>(type: "boolean", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    gross_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    taxable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    other_charges = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_goods_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_goods_receipts_pharmacy_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_goods_receipt_lines",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    hsn_code = table.Column<string>(type: "text", nullable: true),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "text", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    units_per_pack = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    free_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    purchase_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    mrp = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    selling_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    discount_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gst_rate_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    taxable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    returned_units = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_goods_receipt_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_goods_receipt_lines_goods_receipt_id",
                        column: x => x.goods_receipt_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_goods_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_transactions_source_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_goods_receipt_lines_goods_receipt_id",
                schema: "pharmacy",
                table: "pharmacy_goods_receipt_lines",
                column: "goods_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_goods_receipt_lines_product_id",
                schema: "pharmacy",
                table: "pharmacy_goods_receipt_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_goods_receipts_received_date",
                schema: "pharmacy",
                table: "pharmacy_goods_receipts",
                column: "received_date");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_goods_receipts_store_id",
                schema: "pharmacy",
                table: "pharmacy_goods_receipts",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_goods_receipts_supplier_invoice",
                schema: "pharmacy",
                table: "pharmacy_goods_receipts",
                columns: new[] { "supplier_id", "supplier_invoice_no" });

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_goods_receipts_receipt_no",
                schema: "pharmacy",
                table: "pharmacy_goods_receipts",
                column: "receipt_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pharmacy_goods_receipt_lines",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_goods_receipts",
                schema: "pharmacy");

            migrationBuilder.DropIndex(
                name: "ix_pharmacy_stock_transactions_source_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropColumn(
                name: "source_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropColumn(
                name: "source_no",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropColumn(
                name: "source_type",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropSequence(
                name: "adjustment_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "goods_receipt_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "indent_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "purchase_order_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "purchase_return_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "sale_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "sale_return_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "stock_count_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "supplier_payment_no_seq",
                schema: "pharmacy");

            migrationBuilder.DropSequence(
                name: "transfer_no_seq",
                schema: "pharmacy");
        }
    }
}
