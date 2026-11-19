using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLineBedsideAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "bedside_amount",
                schema: "billing",
                table: "invoice_line_items",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "bedside_doctor_charge",
                schema: "billing",
                table: "invoice_line_items",
                type: "numeric(12,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bedside_amount",
                schema: "billing",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "bedside_doctor_charge",
                schema: "billing",
                table: "invoice_line_items");
        }
    }
}
