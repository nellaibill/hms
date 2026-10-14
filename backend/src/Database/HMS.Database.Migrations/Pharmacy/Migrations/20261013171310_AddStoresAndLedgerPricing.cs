using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Pharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddStoresAndLedgerPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_pharmacy_stock_balances_product_batch",
                schema: "pharmacy",
                table: "pharmacy_stock_balances");

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_balances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "pharmacy_stores",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    store_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("pk_pharmacy_stores", x => x.id);
                });

            // Hand-written (everything else here is dotnet-ef-generated). Every tenant starts
            // with one default store, and all stock recorded before stores existed is assigned
            // to it — which is where it physically was, since there was only ever one place.
            // This has to run before the foreign keys below are added: the generated columns
            // above arrive holding an all-zero id that references no store. The placeholder
            // default is then dropped so a row can never again be saved without a real store.
            migrationBuilder.Sql(
                """
                INSERT INTO pharmacy.pharmacy_stores (id, code, name, store_type, is_default, is_active, created_at, is_deleted)
                VALUES (gen_random_uuid(), 'MAIN', 'Main Pharmacy', 'Pharmacy', true, true, now(), false);

                UPDATE pharmacy.pharmacy_stock_balances
                SET store_id = (SELECT id FROM pharmacy.pharmacy_stores WHERE code = 'MAIN');

                UPDATE pharmacy.pharmacy_stock_transactions
                SET store_id = (SELECT id FROM pharmacy.pharmacy_stores WHERE code = 'MAIN');

                ALTER TABLE pharmacy.pharmacy_stock_balances ALTER COLUMN store_id DROP DEFAULT;
                ALTER TABLE pharmacy.pharmacy_stock_transactions ALTER COLUMN store_id DROP DEFAULT;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_transactions_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_stock_balances_product_id",
                schema: "pharmacy",
                table: "pharmacy_stock_balances",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_stock_balances_store_product_batch",
                schema: "pharmacy",
                table: "pharmacy_stock_balances",
                columns: new[] { "store_id", "product_id", "product_batch_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_stores_code",
                schema: "pharmacy",
                table: "pharmacy_stores",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_pharmacy_stock_balances_pharmacy_stores_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_balances",
                column: "store_id",
                principalSchema: "pharmacy",
                principalTable: "pharmacy_stores",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pharmacy_stock_transactions_pharmacy_stores_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                column: "store_id",
                principalSchema: "pharmacy",
                principalTable: "pharmacy_stores",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pharmacy_stock_balances_pharmacy_stores_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_pharmacy_stock_transactions_pharmacy_stores_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropTable(
                name: "pharmacy_stores",
                schema: "pharmacy");

            migrationBuilder.DropIndex(
                name: "ix_pharmacy_stock_transactions_store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropIndex(
                name: "ix_pharmacy_stock_balances_product_id",
                schema: "pharmacy",
                table: "pharmacy_stock_balances");

            migrationBuilder.DropIndex(
                name: "ux_pharmacy_stock_balances_store_product_batch",
                schema: "pharmacy",
                table: "pharmacy_stock_balances");

            migrationBuilder.DropColumn(
                name: "store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropColumn(
                name: "unit_price",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");

            migrationBuilder.DropColumn(
                name: "store_id",
                schema: "pharmacy",
                table: "pharmacy_stock_balances");

            migrationBuilder.CreateIndex(
                name: "ux_pharmacy_stock_balances_product_batch",
                schema: "pharmacy",
                table: "pharmacy_stock_balances",
                columns: new[] { "product_id", "product_batch_id" },
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
