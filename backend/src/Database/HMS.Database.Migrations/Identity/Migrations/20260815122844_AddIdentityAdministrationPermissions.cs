using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HMS.Database.Migrations.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAdministrationPermissions : Migration
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
                    { new Guid("6e2c1b3a-8f4d-4a2e-9c7b-1a5e3d8f6b2c"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 11, true, false, "identity-administration.view", "View", "identity-administration", null, null },
                    { new Guid("7f3d2c4b-9a5e-4b3f-8d6c-2b6f4e9a7c3d"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 11, true, false, "identity-administration.create", "Create", "identity-administration", null, null },
                    { new Guid("8a4e3d5c-0b6f-4c40-9e7d-3c7a5f0b8d4e"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 11, true, false, "identity-administration.edit", "Edit", "identity-administration", null, null },
                    { new Guid("9b5f4e6d-1c70-4d51-af8e-4d8b60c19e5f"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 11, true, false, "identity-administration.delete", "Delete", "identity-administration", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("6e2c1b3a-8f4d-4a2e-9c7b-1a5e3d8f6b2c"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("7f3d2c4b-9a5e-4b3f-8d6c-2b6f4e9a7c3d"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("8a4e3d5c-0b6f-4c40-9e7d-3c7a5f0b8d4e"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("9b5f4e6d-1c70-4d51-af8e-4d8b60c19e5f"));
        }
    }
}
