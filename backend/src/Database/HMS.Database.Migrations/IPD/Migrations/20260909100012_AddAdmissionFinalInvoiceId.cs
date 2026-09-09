using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.IPD.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionFinalInvoiceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "final_invoice_id",
                schema: "ipd",
                table: "admissions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "final_invoice_id",
                schema: "ipd",
                table: "admissions");
        }
    }
}
