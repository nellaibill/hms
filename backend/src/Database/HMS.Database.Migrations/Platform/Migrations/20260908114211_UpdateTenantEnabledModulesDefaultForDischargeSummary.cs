using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Platform.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantEnabledModulesDefaultForDischargeSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "enabled_modules",
                schema: "platform",
                table: "tenants",
                type: "text",
                nullable: false,
                defaultValue: "patient-management,clinical-care,diagnostics,pharmacy,support-services,finance-billing,records-compliance,workforce-admin,engagement,reports-analytics,identity-administration,discharge-summary",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "patient-management,clinical-care,diagnostics,pharmacy,support-services,finance-billing,records-compliance,workforce-admin,engagement,reports-analytics,identity-administration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "enabled_modules",
                schema: "platform",
                table: "tenants",
                type: "text",
                nullable: false,
                defaultValue: "patient-management,clinical-care,diagnostics,pharmacy,support-services,finance-billing,records-compliance,workforce-admin,engagement,reports-analytics,identity-administration",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "patient-management,clinical-care,diagnostics,pharmacy,support-services,finance-billing,records-compliance,workforce-admin,engagement,reports-analytics,identity-administration,discharge-summary");
        }
    }
}
