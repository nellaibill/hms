using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HMS.Database.Migrations.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyViewCostAndDiscountApprovePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "permissions",
                columns: new[] { "Id", "Action", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "IsDeleted", "Key", "Label", "Module", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("a3c1d8f2-5e6b-47a0-9d3c-8f1b2e4a6c9d"), "view-cost", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 4, true, false, "pharmacy.view-cost", "View Cost Price", "pharmacy", null, null },
                    { new Guid("b6e4a1d3-7c9f-4f2a-8e5d-1a3c6f9b2d4e"), "discount-approve", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 6, true, false, "finance-billing.discount-approve", "Approve Discounts", "finance-billing", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("a3c1d8f2-5e6b-47a0-9d3c-8f1b2e4a6c9d"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("b6e4a1d3-7c9f-4f2a-8e5d-1a3c6f9b2d4e"));
        }
    }
}
