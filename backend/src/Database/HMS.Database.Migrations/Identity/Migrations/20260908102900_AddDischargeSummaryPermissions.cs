using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HMS.Database.Migrations.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddDischargeSummaryPermissions : Migration
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
                    { new Guid("1d6e9f3a-2b4c-4a1d-8e5f-6c9b2a4d7f10"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 12, true, false, "discharge-summary.view", "View", "discharge-summary", null, null },
                    { new Guid("2e7fa04b-3c5d-4b2e-9f60-7dac3b5e8021"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 12, true, false, "discharge-summary.create", "Create", "discharge-summary", null, null },
                    { new Guid("3f80b15c-4d6e-4c3f-a071-8ebd4c6f9132"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 12, true, false, "discharge-summary.edit", "Edit", "discharge-summary", null, null },
                    { new Guid("4091c26d-5e7f-4d40-b182-9fce5d70a243"), "finalize", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 12, true, false, "discharge-summary.finalize", "Finalize", "discharge-summary", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("1d6e9f3a-2b4c-4a1d-8e5f-6c9b2a4d7f10"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e7fa04b-3c5d-4b2e-9f60-7dac3b5e8021"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("3f80b15c-4d6e-4c3f-a071-8ebd4c6f9132"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("4091c26d-5e7f-4d40-b182-9fce5d70a243"));
        }
    }
}
