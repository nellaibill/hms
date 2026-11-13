using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <summary>
    /// ADR-134: saves each bill's encounter type (OP/IP/Emergency/DayCare/Observation) so
    /// Accounts and Finance can list OP and IP bills apart. Existing bills are filled in here:
    /// first from their visit (patients.patient_visits, same database), then IPD final bills,
    /// which have no visit of their own, as IP. Bills with neither stay null and are counted under
    /// OP. The visit step is skipped when the patients table isn't there yet (a brand-new
    /// database migrated Billing first), since there are no bills to fill in then.
    /// </summary>
    public partial class AddInvoiceEncounterType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "encounter_type",
                schema: "billing",
                table: "invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_encounter_type",
                schema: "billing",
                table: "invoices",
                column: "encounter_type");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('patients.patient_visits') IS NOT NULL THEN
                        UPDATE billing.invoices i
                        SET encounter_type = v.visit_type
                        FROM patients.patient_visits v
                        WHERE v.visit_id = i.visit_id
                          AND i.encounter_type IS NULL;
                    END IF;
                END $$;

                UPDATE billing.invoices i
                SET encounter_type = 'IP'
                WHERE i.encounter_type IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM billing.invoice_line_items li
                      WHERE li.invoice_id = i.id
                        AND li.billing_type = 'InpatientCharge');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_encounter_type",
                schema: "billing",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "encounter_type",
                schema: "billing",
                table: "invoices");
        }
    }
}
