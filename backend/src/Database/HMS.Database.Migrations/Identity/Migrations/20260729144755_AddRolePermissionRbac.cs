using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HMS.Database.Migrations.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissionRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "identity",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "permissions",
                columns: new[] { "Id", "Action", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "IsDeleted", "Key", "Label", "Module", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("113bf692-b2a7-4d56-9a20-e196bbab0cb6"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 8, true, false, "workforce-admin.create", "Create", "workforce-admin", null, null },
                    { new Guid("1cfc6580-0e9c-4055-b7de-3e9efcfdc306"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 4, true, false, "pharmacy.create", "Create", "pharmacy", null, null },
                    { new Guid("1ef728c0-3cd0-46cb-b4d8-fa12101a2509"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 3, true, false, "diagnostics.view", "View", "diagnostics", null, null },
                    { new Guid("200db0f5-cb8b-4ec6-bb4a-e380744dc7be"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 7, true, false, "records-compliance.delete", "Delete", "records-compliance", null, null },
                    { new Guid("21ff0f71-fae5-4a0b-a7f8-ff08be7ff7bc"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 2, true, false, "clinical-care.edit", "Edit", "clinical-care", null, null },
                    { new Guid("2542c897-7242-4423-9547-f7accb795a66"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 6, true, false, "finance-billing.edit", "Edit", "finance-billing", null, null },
                    { new Guid("256f8ba6-c0bb-45ab-a9ad-018a4bfaa5ee"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 9, true, false, "engagement.delete", "Delete", "engagement", null, null },
                    { new Guid("27964465-0e51-4494-8330-49fc7e66b084"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 5, true, false, "support-services.create", "Create", "support-services", null, null },
                    { new Guid("2be079ff-aa89-4c47-bbda-cc8778209472"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 2, true, false, "clinical-care.view", "View", "clinical-care", null, null },
                    { new Guid("3bd21acb-54fe-44a4-80db-7b7d79768114"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 6, true, false, "finance-billing.delete", "Delete", "finance-billing", null, null },
                    { new Guid("3dca5a94-952c-427b-919b-206a9183bda5"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 10, true, false, "reports-analytics.edit", "Edit", "reports-analytics", null, null },
                    { new Guid("466d8793-d6b3-4e44-a20b-6688faef4f9f"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 2, true, false, "clinical-care.delete", "Delete", "clinical-care", null, null },
                    { new Guid("46eb9271-252f-428e-bdbd-756c36d39203"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 9, true, false, "engagement.create", "Create", "engagement", null, null },
                    { new Guid("47f91d93-a7ef-4156-966f-756b4142ba52"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 5, true, false, "support-services.delete", "Delete", "support-services", null, null },
                    { new Guid("4852ff8b-e34b-42fc-8beb-944d4f0143d8"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 9, true, false, "engagement.edit", "Edit", "engagement", null, null },
                    { new Guid("50adeda0-6191-4777-9430-6c4b359eff9d"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 1, true, false, "patient-management.edit", "Edit", "patient-management", null, null },
                    { new Guid("54ae37a9-186d-466a-b5df-70b7c68a7679"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 10, true, false, "reports-analytics.delete", "Delete", "reports-analytics", null, null },
                    { new Guid("734f5ff4-48cf-4c78-8614-0c017a38eec6"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 7, true, false, "records-compliance.edit", "Edit", "records-compliance", null, null },
                    { new Guid("7d114d49-dd3f-422e-b920-8f42c105595f"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 8, true, false, "workforce-admin.view", "View", "workforce-admin", null, null },
                    { new Guid("7dfefecb-3b08-4652-91d6-cf6de25ce057"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 4, true, false, "pharmacy.delete", "Delete", "pharmacy", null, null },
                    { new Guid("7e6ef2e4-94d5-4741-9a73-cd9ce5a6d69f"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 8, true, false, "workforce-admin.delete", "Delete", "workforce-admin", null, null },
                    { new Guid("8273002e-4bf7-4a05-84e5-3680154420e4"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 3, true, false, "diagnostics.delete", "Delete", "diagnostics", null, null },
                    { new Guid("844e1456-87a8-4dca-8469-bb19117c7cb4"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 7, true, false, "records-compliance.view", "View", "records-compliance", null, null },
                    { new Guid("8509d991-b281-4259-bdb2-89ed75ec2f0d"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 3, true, false, "diagnostics.edit", "Edit", "diagnostics", null, null },
                    { new Guid("8cb7e9d1-1c22-44b0-9874-3d21e61e0e59"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 6, true, false, "finance-billing.view", "View", "finance-billing", null, null },
                    { new Guid("93d8f89c-edef-4580-8e0d-4419a9b1e0f7"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 6, true, false, "finance-billing.create", "Create", "finance-billing", null, null },
                    { new Guid("9558c7d8-d90e-44cc-b361-4f897230103a"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 10, true, false, "reports-analytics.view", "View", "reports-analytics", null, null },
                    { new Guid("b7bd020c-bf4f-4e36-a501-b4f68dd1a096"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 10, true, false, "reports-analytics.create", "Create", "reports-analytics", null, null },
                    { new Guid("bb83e3bc-a4c0-42a8-a15e-3603132d08b6"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 1, true, false, "patient-management.create", "Create", "patient-management", null, null },
                    { new Guid("bdb7a1ff-cfa7-4475-927d-ab389b0a06a8"), "delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 1, true, false, "patient-management.delete", "Delete", "patient-management", null, null },
                    { new Guid("d0ad8c77-4d81-4773-9155-7c217e0877e6"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 3, true, false, "diagnostics.create", "Create", "diagnostics", null, null },
                    { new Guid("d90b82d1-e86d-44ae-a85d-64c4eef8cbae"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 8, true, false, "workforce-admin.edit", "Edit", "workforce-admin", null, null },
                    { new Guid("db3b17d7-811d-422a-a221-bb65a5037676"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 1, true, false, "patient-management.view", "View", "patient-management", null, null },
                    { new Guid("e1f0e24d-bdb1-48f1-a37d-bdfda4055886"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 7, true, false, "records-compliance.create", "Create", "records-compliance", null, null },
                    { new Guid("e40d8eaf-ed39-4f76-b6f9-055db294d653"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 5, true, false, "support-services.view", "View", "support-services", null, null },
                    { new Guid("e71663b9-79ae-40ad-bf94-b8f402898658"), "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 2, true, false, "clinical-care.create", "Create", "clinical-care", null, null },
                    { new Guid("ec1f3291-ebb5-4b79-abfe-bf6a8702f895"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 9, true, false, "engagement.view", "View", "engagement", null, null },
                    { new Guid("ef1023c8-e81a-4354-8994-6808089c4d2c"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 4, true, false, "pharmacy.edit", "Edit", "pharmacy", null, null },
                    { new Guid("f477dc55-4359-4489-9d4c-b59003284609"), "edit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 5, true, false, "support-services.edit", "Edit", "support-services", null, null },
                    { new Guid("fbeecdff-38a9-42b1-98f2-c477d4c1af31"), "view", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 4, true, false, "pharmacy.view", "View", "pharmacy", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Key",
                schema: "identity",
                table: "permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Module_Action",
                schema: "identity",
                table: "permissions",
                columns: new[] { "Module", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionId",
                schema: "identity",
                table: "role_permissions",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "identity");
        }
    }
}
