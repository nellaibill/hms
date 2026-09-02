using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientManagementImportPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "permissions",
                columns: new[] { "Id", "Action", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "IsDeleted", "Key", "Label", "Module", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("f1a4c9e2-7b3d-4e8a-9c1f-2d6b8e4a0c7f"), "import", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 1, true, false, "patient-management.import", "Bulk Import", "patient-management", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("f1a4c9e2-7b3d-4e8a-9c1f-2d6b8e4a0c7f"));
        }
    }
}
