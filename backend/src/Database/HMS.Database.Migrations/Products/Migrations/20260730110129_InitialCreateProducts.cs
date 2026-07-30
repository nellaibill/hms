using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Products.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "products");

            migrationBuilder.CreateTable(
                name: "product_attributes",
                schema: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    attribute_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    data_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("pk_product_attributes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    generic_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_batch_tracked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_serialized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    reorder_level = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    min_stock_level = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    max_stock_level = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    mrp = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    cost_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    selling_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    hsn_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    volume = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_attribute_values",
                schema: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_value = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_product_attribute_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_attribute_values_product_attributes_attribute_id",
                        column: x => x.attribute_id,
                        principalSchema: "products",
                        principalTable: "product_attributes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_attribute_values_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "products",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_barcodes",
                schema: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    barcode_value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_product_barcodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_barcodes_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "products",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_batches",
                schema: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    manufacture_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("pk_product_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_batches_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "products",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                schema: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    image_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("pk_product_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_images_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "products",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_prices",
                schema: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("pk_product_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_prices_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "products",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_tax_mappings",
                schema: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_product_tax_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_tax_mappings_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "products",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_attribute_values_attribute_id_attribute_value",
                schema: "products",
                table: "product_attribute_values",
                columns: new[] { "attribute_id", "attribute_value" });

            migrationBuilder.CreateIndex(
                name: "ux_product_attribute_values_product_id_attribute_id",
                schema: "products",
                table: "product_attribute_values",
                columns: new[] { "product_id", "attribute_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_product_attributes_attribute_code",
                schema: "products",
                table: "product_attributes",
                column: "attribute_code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_product_barcodes_product_id",
                schema: "products",
                table: "product_barcodes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_barcodes_barcode_value",
                schema: "products",
                table: "product_barcodes",
                column: "barcode_value",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_product_batches_product_id_expiry_date",
                schema: "products",
                table: "product_batches",
                columns: new[] { "product_id", "expiry_date" });

            migrationBuilder.CreateIndex(
                name: "ux_product_batches_product_id_batch_no",
                schema: "products",
                table: "product_batches",
                columns: new[] { "product_id", "batch_no" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id",
                schema: "products",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_images_product_id_image_type_display_order",
                schema: "products",
                table: "product_images",
                columns: new[] { "product_id", "image_type", "display_order" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_product_prices_currency_id",
                schema: "products",
                table: "product_prices",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_prices_product_id_price_type_effective_from",
                schema: "products",
                table: "product_prices",
                columns: new[] { "product_id", "price_type", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ux_product_prices_product_id_price_type_currency_id_effective_from",
                schema: "products",
                table: "product_prices",
                columns: new[] { "product_id", "price_type", "currency_id", "effective_from" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_product_tax_mappings_tax_id",
                schema: "products",
                table: "product_tax_mappings",
                column: "tax_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_tax_mappings_product_id_tax_id_tax_type",
                schema: "products",
                table: "product_tax_mappings",
                columns: new[] { "product_id", "tax_id", "tax_type" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_products_base_uom_id",
                schema: "products",
                table: "products",
                column: "base_uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_brand_id",
                schema: "products",
                table: "products",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                schema: "products",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_group_id",
                schema: "products",
                table: "products",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_manufacturer_id",
                schema: "products",
                table: "products",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_sub_category_id",
                schema: "products",
                table: "products",
                column: "sub_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_uom_id",
                schema: "products",
                table: "products",
                column: "uom_id");

            migrationBuilder.CreateIndex(
                name: "ux_products_product_code",
                schema: "products",
                table: "products",
                column: "product_code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_products_sku",
                schema: "products",
                table: "products",
                column: "sku",
                unique: true,
                filter: "is_deleted = false");

            // Cross-schema foreign keys into masters.* — not generated automatically since
            // the target entities live in a different module/DbContext (HMS.Modules.Masters),
            // so EF has no navigation to infer them from. Added by hand per
            // docs/DatabaseArchitecture.md §7 ("a deliberate, reviewed decision"). Restrict
            // delete, matching every other cross-aggregate FK in this schema.
            migrationBuilder.AddForeignKey(
                name: "fk_products_masters_brands_brand_id",
                schema: "products",
                table: "products",
                column: "brand_id",
                principalSchema: "masters",
                principalTable: "brands",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_masters_manufacturers_manufacturer_id",
                schema: "products",
                table: "products",
                column: "manufacturer_id",
                principalSchema: "masters",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_masters_product_categories_category_id",
                schema: "products",
                table: "products",
                column: "category_id",
                principalSchema: "masters",
                principalTable: "product_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_masters_product_sub_categories_sub_category_id",
                schema: "products",
                table: "products",
                column: "sub_category_id",
                principalSchema: "masters",
                principalTable: "product_sub_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_masters_product_groups_group_id",
                schema: "products",
                table: "products",
                column: "group_id",
                principalSchema: "masters",
                principalTable: "product_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_masters_units_of_measure_uom_id",
                schema: "products",
                table: "products",
                column: "uom_id",
                principalSchema: "masters",
                principalTable: "units_of_measure",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_masters_units_of_measure_base_uom_id",
                schema: "products",
                table: "products",
                column: "base_uom_id",
                principalSchema: "masters",
                principalTable: "units_of_measure",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_prices_masters_currencies_currency_id",
                schema: "products",
                table: "product_prices",
                column: "currency_id",
                principalSchema: "masters",
                principalTable: "currencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_mappings_masters_taxes_tax_id",
                schema: "products",
                table: "product_tax_mappings",
                column: "tax_id",
                principalSchema: "masters",
                principalTable: "taxes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_products_masters_brands_brand_id", schema: "products", table: "products");
            migrationBuilder.DropForeignKey(name: "fk_products_masters_manufacturers_manufacturer_id", schema: "products", table: "products");
            migrationBuilder.DropForeignKey(name: "fk_products_masters_product_categories_category_id", schema: "products", table: "products");
            migrationBuilder.DropForeignKey(name: "fk_products_masters_product_sub_categories_sub_category_id", schema: "products", table: "products");
            migrationBuilder.DropForeignKey(name: "fk_products_masters_product_groups_group_id", schema: "products", table: "products");
            migrationBuilder.DropForeignKey(name: "fk_products_masters_units_of_measure_uom_id", schema: "products", table: "products");
            migrationBuilder.DropForeignKey(name: "fk_products_masters_units_of_measure_base_uom_id", schema: "products", table: "products");
            migrationBuilder.DropForeignKey(name: "fk_product_prices_masters_currencies_currency_id", schema: "products", table: "product_prices");
            migrationBuilder.DropForeignKey(name: "fk_product_tax_mappings_masters_taxes_tax_id", schema: "products", table: "product_tax_mappings");

            migrationBuilder.DropTable(
                name: "product_attribute_values",
                schema: "products");

            migrationBuilder.DropTable(
                name: "product_barcodes",
                schema: "products");

            migrationBuilder.DropTable(
                name: "product_batches",
                schema: "products");

            migrationBuilder.DropTable(
                name: "product_images",
                schema: "products");

            migrationBuilder.DropTable(
                name: "product_prices",
                schema: "products");

            migrationBuilder.DropTable(
                name: "product_tax_mappings",
                schema: "products");

            migrationBuilder.DropTable(
                name: "product_attributes",
                schema: "products");

            migrationBuilder.DropTable(
                name: "products",
                schema: "products");
        }
    }
}
