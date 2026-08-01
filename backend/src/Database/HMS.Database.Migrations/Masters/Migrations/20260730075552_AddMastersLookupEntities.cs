using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddMastersLookupEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    brand_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    manufacturer_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_manufacturers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "taxes",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tax_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    rate_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    is_inclusive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("pk_taxes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "units_of_measure",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uom_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uom_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    uom_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_base_unit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("pk_units_of_measure", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    warehouse_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_warehouses", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_brands_brand_code",
                schema: "masters",
                table: "brands",
                column: "brand_code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_manufacturers_manufacturer_code",
                schema: "masters",
                table: "manufacturers",
                column: "manufacturer_code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_taxes_tax_code",
                schema: "masters",
                table: "taxes",
                column: "tax_code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_units_of_measure_uom_code",
                schema: "masters",
                table: "units_of_measure",
                column: "uom_code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_warehouses_warehouse_code",
                schema: "masters",
                table: "warehouses",
                column: "warehouse_code",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brands",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "manufacturers",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "taxes",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "units_of_measure",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "masters");
        }
    }
}
