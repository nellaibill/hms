using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.IPD.Migrations
{
    /// <inheritdoc />
    public partial class ConvertBedTypeToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pre-existing free-text bed_type values don't all match the new BedType enum's
            // member names (e.g. stray "A"/"a"/"b" test data) — remap anything outside the
            // fixed set to the closest safe default, Standard, before the column becomes enum-backed.
            migrationBuilder.Sql(
                """
                UPDATE ipd.beds
                SET bed_type = 'Standard'
                WHERE bed_type NOT IN ('Standard', 'Electric', 'ICU', 'SemiICU', 'Deluxe');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "bed_type",
                schema: "ipd",
                table: "beds",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "bed_type",
                schema: "ipd",
                table: "beds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
