using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Pharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddStockControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pharmacy_stock_adjustments",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment_no = table.Column<string>(type: "text", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stock_count_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_pharmacy_stock_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_stock_adjustments_pharmacy_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_stock_counts",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    count_no = table.Column<string>(type: "text", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    adjustment_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_pharmacy_stock_counts", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_stock_counts_pharmacy_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_stock_transfers",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_no = table.Column<string>(type: "text", nullable: false),
                    from_store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_stock_transfers", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_stock_transfers_from_store_id",
                        column: x => x.from_store_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pharmacy_stock_transfers_to_store_id",
                        column: x => x.to_store_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_stock_adjustment_lines",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "text", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("pk_pharmacy_stock_adjustment_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_stock_adjustment_lines_adjustment_id",
                        column: x => x.adjustment_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stock_adjustments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_stock_count_lines",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_count_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "text", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    book_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    counted_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("pk_pharmacy_stock_count_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_stock_count_lines_stock_count_id",
                        column: x => x.stock_count_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stock_counts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_stock_transfer_lines",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "text", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_stock_transfer_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_stock_transfer_lines_transfer_id",
                        column: x => x.transfer_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_stock_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_adjustment_lines_adjustment_id",
                schema: "pharmacy",
                table: "pharmacy_stock_adjustment_lines",
                column: "adjustment_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_adjustments_adjustment_date",
                schema: "pharmacy",
                table: "pharmacy_stock_adjustments",
                column: "adjustment_date");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_stock_adjustments_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_adjustments",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_stock_adjustments_adjustment_no",
                schema: "pharmacy",
                table: "pharmacy_stock_adjustments",
                column: "adjustment_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_count_lines_stock_count_id",
                schema: "pharmacy",
                table: "pharmacy_stock_count_lines",
                column: "stock_count_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_counts_store_status",
                schema: "pharmacy",
                table: "pharmacy_stock_counts",
                columns: new[] { "store_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_stock_counts_count_no",
                schema: "pharmacy",
                table: "pharmacy_stock_counts",
                column: "count_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_transfer_lines_transfer_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transfer_lines",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_stock_transfers_from_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transfers",
                column: "from_store_id");

            migrationBuilder.CreateIndex(
                name: "IX_pharmacy_stock_transfers_to_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transfers",
                column: "to_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_transfers_transfer_date",
                schema: "pharmacy",
                table: "pharmacy_stock_transfers",
                column: "transfer_date");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_stock_transfers_transfer_no",
                schema: "pharmacy",
                table: "pharmacy_stock_transfers",
                column: "transfer_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pharmacy_stock_adjustment_lines",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_stock_count_lines",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_stock_transfer_lines",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_stock_adjustments",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_stock_counts",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_stock_transfers",
                schema: "pharmacy");
        }
    }
}
