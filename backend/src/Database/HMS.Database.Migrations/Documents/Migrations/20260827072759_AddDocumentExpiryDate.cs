using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Documents.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentExpiryDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "expiry_date",
                schema: "documents",
                table: "documents",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_expiry_date",
                schema: "documents",
                table: "documents",
                column: "expiry_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_documents_expiry_date",
                schema: "documents",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "expiry_date",
                schema: "documents",
                table: "documents");
        }
    }
}
