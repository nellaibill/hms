using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "enabled_modules",
                schema: "platform",
                table: "tenants",
                type: "text",
                nullable: false,
                defaultValue: "patient-management,clinical-care,diagnostics,pharmacy,support-services,finance-billing,records-compliance,workforce-admin,engagement,reports-analytics,identity-administration");

            migrationBuilder.AddColumn<string>(
                name: "subscription_tier",
                schema: "platform",
                table: "tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Standard");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enabled_modules",
                schema: "platform",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "subscription_tier",
                schema: "platform",
                table: "tenants");
        }
    }
}
