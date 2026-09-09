using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLineItemBilledConsultantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billed_consultant_id",
                schema: "billing",
                table: "invoice_line_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billed_consultant_id",
                schema: "billing",
                table: "invoice_line_items");
        }
    }
}
