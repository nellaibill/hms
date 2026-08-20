using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Pharmacy.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatePharmacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pharmacy");

            migrationBuilder.CreateTable(
                name: "pharmacy_stock_balances",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
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
                    table.PrimaryKey("pk_pharmacy_stock_balances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_stock_transactions",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_pharmacy_stock_transactions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_stock_balances_product_batch",
                schema: "pharmacy",
                table: "pharmacy_stock_balances",
                columns: new[] { "product_id", "product_batch_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_transactions_patient_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_transactions_product_batch",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                columns: new[] { "product_id", "product_batch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_transactions_transaction_date",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                column: "transaction_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pharmacy_stock_balances",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_stock_transactions",
                schema: "pharmacy");
        }
    }
}
