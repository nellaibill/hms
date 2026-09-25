using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Branding.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingLogoSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "compact_logo_path",
                schema: "branding",
                table: "branding_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "favicon_path",
                schema: "branding",
                table: "branding_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "login_logo_path",
                schema: "branding",
                table: "branding_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_display",
                schema: "branding",
                table: "branding_settings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "print_logo_path",
                schema: "branding",
                table: "branding_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "compact_logo_path",
                schema: "branding",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "favicon_path",
                schema: "branding",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "login_logo_path",
                schema: "branding",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "logo_display",
                schema: "branding",
                table: "branding_settings");

            migrationBuilder.DropColumn(
                name: "print_logo_path",
                schema: "branding",
                table: "branding_settings");
        }
    }
}
