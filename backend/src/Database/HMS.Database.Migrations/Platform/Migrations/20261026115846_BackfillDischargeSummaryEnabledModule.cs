using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Platform.Migrations
{
    /// <inheritdoc />
    public partial class BackfillDischargeSummaryEnabledModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // UpdateTenantEnabledModulesDefaultForDischargeSummary only changed the column
            // default, so every hospital registered before it kept the 11-module list. Login
            // strips permissions for modules missing from enabled_modules, so those hospitals'
            // Super Admins got 403 on discharge-summary.* even with the role fully synced.
            // Only hospitals that had every pre-existing module (i.e. were never restricted by a
            // Platform Admin) get it; a restricted hospital is left for a Platform Admin to decide.
            migrationBuilder.Sql(
                """
                UPDATE platform.tenants
                SET enabled_modules = enabled_modules || ',discharge-summary'
                WHERE string_to_array(enabled_modules, ',') @> ARRAY[
                        'patient-management', 'clinical-care', 'diagnostics', 'pharmacy',
                        'support-services', 'finance-billing', 'records-compliance',
                        'workforce-admin', 'engagement', 'reports-analytics',
                        'identity-administration']
                  AND NOT ('discharge-summary' = ANY (string_to_array(enabled_modules, ',')));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty: removing the module again can't tell backfilled hospitals
            // from ones that already had it, and would bring the 403s back.
        }
    }
}
