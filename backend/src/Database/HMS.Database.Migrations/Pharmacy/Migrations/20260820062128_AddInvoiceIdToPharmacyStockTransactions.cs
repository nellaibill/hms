using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Pharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceIdToPharmacyStockTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "invoice_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "invoice_id",
                schema: "pharmacy",
                table: "pharmacy_stock_transactions");
        }
    }
}
