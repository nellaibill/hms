using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Pharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pharmacy_held_bills",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    item_count = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_held_bills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_sales",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_no = table.Column<string>(type: "text", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    patient_uhid = table.Column<string>(type: "text", nullable: true),
                    customer_phone = table.Column<string>(type: "text", nullable: true),
                    doctor_name = table.Column<string>(type: "text", nullable: true),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    indent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    gross_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    taxable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    round_off = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    returned_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    charged_to_admission = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_sales", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_sales_pharmacy_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_settings",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pharmacy_name = table.Column<string>(type: "text", nullable: true),
                    gstin = table.Column<string>(type: "text", nullable: true),
                    drug_licence_no = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    bill_footer = table.Column<string>(type: "text", nullable: true),
                    print_format = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    max_discount_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    expiry_alert_days = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_sale_lines",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    generic_name = table.Column<string>(type: "text", nullable: true),
                    hsn_code = table.Column<string>(type: "text", nullable: true),
                    schedule = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "text", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    discount_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gst_rate_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    taxable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    returned_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_sale_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_sale_lines_sale_id",
                        column: x => x.sale_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_sale_payments",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reference_no = table.Column<string>(type: "text", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_sale_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_sale_payments_sale_id",
                        column: x => x.sale_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_sale_returns",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_no = table.Column<string>(type: "text", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_no = table.Column<string>(type: "text", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    taxable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    return_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    refund_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    refund_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    restocked = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_pharmacy_sale_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_sale_returns_sale_id",
                        column: x => x.sale_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_sale_return_lines",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_return_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_sale_return_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_sale_return_lines_sale_return_id",
                        column: x => x.sale_return_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_sale_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_held_bills_store_id",
                schema: "pharmacy",
                table: "pharmacy_held_bills",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_lines_product_id",
                schema: "pharmacy",
                table: "pharmacy_sale_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_lines_sale_id",
                schema: "pharmacy",
                table: "pharmacy_sale_lines",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_lines_schedule",
                schema: "pharmacy",
                table: "pharmacy_sale_lines",
                column: "schedule");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_payments_paid_at",
                schema: "pharmacy",
                table: "pharmacy_sale_payments",
                column: "paid_at");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_payments_sale_id",
                schema: "pharmacy",
                table: "pharmacy_sale_payments",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_return_lines_sale_return_id",
                schema: "pharmacy",
                table: "pharmacy_sale_return_lines",
                column: "sale_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_returns_return_date",
                schema: "pharmacy",
                table: "pharmacy_sale_returns",
                column: "return_date");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_returns_sale_id",
                schema: "pharmacy",
                table: "pharmacy_sale_returns",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_sale_returns_return_no",
                schema: "pharmacy",
                table: "pharmacy_sale_returns",
                column: "return_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_admission_id",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_patient_id",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_sale_date",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "sale_date");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_sales_store_id",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_sales_bill_no",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "bill_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pharmacy_held_bills",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_sale_lines",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_sale_payments",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_sale_return_lines",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_settings",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_sale_returns",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_sales",
                schema: "pharmacy");
        }
    }
}
