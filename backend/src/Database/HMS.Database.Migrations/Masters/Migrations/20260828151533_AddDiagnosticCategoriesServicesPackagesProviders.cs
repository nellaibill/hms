using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosticCategoriesServicesPackagesProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diagnostic_categories",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnostic_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "diagnostic_packages",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnostic_packages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "diagnostic_providers",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contact_details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnostic_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "diagnostic_services",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_outsourced = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnostic_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "diagnostic_package_items",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnostic_package_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_diagnostic_package_items_package_id",
                        column: x => x.package_id,
                        principalSchema: "masters",
                        principalTable: "diagnostic_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_diagnostic_categories_code",
                schema: "masters",
                table: "diagnostic_categories",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_diagnostic_package_items_package_id",
                schema: "masters",
                table: "diagnostic_package_items",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_diagnostic_package_items_service_id",
                schema: "masters",
                table: "diagnostic_package_items",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ux_diagnostic_packages_code",
                schema: "masters",
                table: "diagnostic_packages",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_diagnostic_providers_code",
                schema: "masters",
                table: "diagnostic_providers",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_diagnostic_services_category_id",
                schema: "masters",
                table: "diagnostic_services",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ux_diagnostic_services_code",
                schema: "masters",
                table: "diagnostic_services",
                column: "code",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diagnostic_categories",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "diagnostic_package_items",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "diagnostic_providers",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "diagnostic_services",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "diagnostic_packages",
                schema: "masters");
        }
    }
}
