using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Products.Migrations
{
    /// <inheritdoc />
    public partial class AddDrugProfileAndBatchPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dosage_form",
                schema: "products",
                table: "products",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "gst_rate_percent",
                schema: "products",
                table: "products",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rack_location",
                schema: "products",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "schedule",
                schema: "products",
                table: "products",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "storage_condition",
                schema: "products",
                table: "products",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "RoomTemperature");

            migrationBuilder.AddColumn<string>(
                name: "strength",
                schema: "products",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "units_per_pack",
                schema: "products",
                table: "products",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "mrp",
                schema: "products",
                table: "product_batches",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "purchase_rate",
                schema: "products",
                table: "product_batches",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "selling_rate",
                schema: "products",
                table: "product_batches",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dosage_form",
                schema: "products",
                table: "products");

            migrationBuilder.DropColumn(
                name: "gst_rate_percent",
                schema: "products",
                table: "products");

            migrationBuilder.DropColumn(
                name: "rack_location",
                schema: "products",
                table: "products");

            migrationBuilder.DropColumn(
                name: "schedule",
                schema: "products",
                table: "products");

            migrationBuilder.DropColumn(
                name: "storage_condition",
                schema: "products",
                table: "products");

            migrationBuilder.DropColumn(
                name: "strength",
                schema: "products",
                table: "products");

            migrationBuilder.DropColumn(
                name: "units_per_pack",
                schema: "products",
                table: "products");

            migrationBuilder.DropColumn(
                name: "mrp",
                schema: "products",
                table: "product_batches");

            migrationBuilder.DropColumn(
                name: "purchase_rate",
                schema: "products",
                table: "product_batches");

            migrationBuilder.DropColumn(
                name: "selling_rate",
                schema: "products",
                table: "product_batches");
        }
    }
}
