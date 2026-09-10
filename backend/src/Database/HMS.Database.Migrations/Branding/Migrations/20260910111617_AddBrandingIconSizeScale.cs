using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Branding.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingIconSizeScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "icon_size_scale",
                schema: "branding",
                table: "branding_settings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "md");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "icon_size_scale",
                schema: "branding",
                table: "branding_settings");
        }
    }
}
