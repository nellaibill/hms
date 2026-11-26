using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <summary>
    /// ADR-153: each bill line keeps its cost per unit (and a scan's doctor's share) from billing
    /// time, so a later Masters edit can't rewrite past days' reports. Bills made before this get
    /// a one-off fill from Masters as it is now, by the same rules the reports use (LineCostRules
    /// / billingCalculations.ts's resolveItemCostPrice). A line Masters has no cost for stays
    /// empty, and reports keep reading Masters for it, as before.
    /// </summary>
    public partial class AddInvoiceLineUnitCost : Migration
    {
        /// <summary>
        /// The fill reads the masters schema, which TenantMigrationService always migrates before
        /// billing. It does nothing on a hospital with no bills (every new hospital), and it is
        /// skipped, with a notice, rather than failing the migration if those masters tables or
        /// columns ever look different: the columns are still added, and reports read Masters for
        /// the lines it skipped.
        /// </summary>
        private const string FillFromCurrentMastersSql = """
            DO $fill$
            BEGIN
                IF to_regclass('masters.consultants') IS NULL
                   OR NOT EXISTS (SELECT 1 FROM billing.invoice_line_items WHERE unit_cost IS NULL) THEN
                    RETURN;
                END IF;

                BEGIN
                    -- Consultation: the billed consultant's share for that consultation type (0 is a real share).
                    UPDATE billing.invoice_line_items li
                    SET unit_cost = cct.consultant_charge
                    FROM masters.consultant_consultation_types cct
                    JOIN masters.consultants c ON c.id = cct.consultant_id AND NOT c.is_deleted
                    WHERE li.billing_type = 'Consultation'
                      AND li.unit_cost IS NULL
                      AND cct.consultant_charge IS NOT NULL
                      AND cct.consultant_id::text = lower(trim(li.billed_consultant_id))
                      AND cct.consultation_type_id::text = lower(trim(li.service_id));

                    -- Radiology: doctor's share + running cost of the rate billed (the rate option's when it
                    -- still exists), plus the bedside doctor's share; unknown when neither part is costed.
                    UPDATE billing.invoice_line_items li
                    SET unit_cost = x.doctor_share + x.running_cost,
                        unit_doctor_share = x.doctor_share
                    FROM (
                        SELECT l.id,
                               COALESCE(o.doctor_charge, ds.doctor_charge) AS rate_doctor_share,
                               COALESCE(o.doctor_charge, ds.doctor_charge)
                                   + CASE WHEN l.is_bedside THEN COALESCE(l.bedside_doctor_charge, ds.bedside_doctor_charge, 0) ELSE 0 END AS doctor_share,
                               COALESCE(o.cost_price, ds.cost_price) AS running_cost
                        FROM billing.invoice_line_items l
                        JOIN masters.diagnostic_services ds ON ds.id::text = lower(trim(l.service_id)) AND NOT ds.is_deleted
                        LEFT JOIN masters.diagnostic_service_rate_options o ON o.id = l.rate_option_id AND o.diagnostic_service_id = ds.id
                        WHERE l.billing_type = 'Radiology' AND l.unit_cost IS NULL
                    ) x
                    WHERE li.id = x.id AND (x.rate_doctor_share > 0 OR x.running_cost > 0);

                    -- Laboratory test: its running cost (0 = not yet costed).
                    UPDATE billing.invoice_line_items li
                    SET unit_cost = ds.cost_price
                    FROM masters.diagnostic_services ds
                    WHERE li.billing_type = 'Laboratory'
                      AND li.package_id IS NULL
                      AND li.unit_cost IS NULL
                      AND ds.id::text = lower(trim(li.service_id))
                      AND NOT ds.is_deleted
                      AND ds.cost_price > 0;

                    -- Laboratory package: its own running cost, else its tests' total when every test has one.
                    UPDATE billing.invoice_line_items li
                    SET unit_cost = p.cost
                    FROM (
                        SELECT dp.id,
                               CASE WHEN dp.cost_price > 0 THEN dp.cost_price
                                    ELSE (SELECT CASE WHEN COUNT(*) > 0 AND COUNT(*) = COUNT(ds.id) FILTER (WHERE ds.cost_price > 0)
                                                      THEN SUM(ds.cost_price) END
                                          FROM masters.diagnostic_package_items pi
                                          LEFT JOIN masters.diagnostic_services ds ON ds.id = pi.service_id AND NOT ds.is_deleted
                                          WHERE pi.package_id = dp.id)
                               END AS cost
                        FROM masters.diagnostic_packages dp
                        WHERE NOT dp.is_deleted
                    ) p
                    WHERE li.billing_type = 'Laboratory'
                      AND li.package_id = p.id
                      AND li.unit_cost IS NULL
                      AND p.cost IS NOT NULL;

                    -- Procedure, injection, file: the test's cost price (0 = not yet costed).
                    UPDATE billing.invoice_line_items li
                    SET unit_cost = dt.cost_price
                    FROM masters.diagnostic_tests dt
                    WHERE li.billing_type IN ('Procedure', 'Injection', 'File')
                      AND li.unit_cost IS NULL
                      AND dt.id::text = lower(trim(li.service_id))
                      AND NOT dt.is_deleted
                      AND dt.cost_price > 0;
                EXCEPTION
                    WHEN undefined_table OR undefined_column THEN
                        RAISE NOTICE 'ADR-153: skipped filling bill line costs from Masters (%); reports read Masters for those lines.', SQLERRM;
                END;
            END
            $fill$;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                schema: "billing",
                table: "invoice_line_items",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_doctor_share",
                schema: "billing",
                table: "invoice_line_items",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.Sql(FillFromCurrentMastersSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unit_cost",
                schema: "billing",
                table: "invoice_line_items");

            migrationBuilder.DropColumn(
                name: "unit_doctor_share",
                schema: "billing",
                table: "invoice_line_items");
        }
    }
}
