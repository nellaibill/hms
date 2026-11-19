using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLineRateOptionBedside : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_bedside",
                schema: "billing",
                table: "invoice_line_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "rate_option_id",
                schema: "billing",
                table: "invoice_line_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rate_option_name",
                schema: "billing",
                table: "invoice_line_items",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_bedside",
                schema: "billing",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "rate_option_id",
                schema: "billing",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "rate_option_name",
                schema: "billing",
                table: "invoice_line_items");
        }
    }
}
