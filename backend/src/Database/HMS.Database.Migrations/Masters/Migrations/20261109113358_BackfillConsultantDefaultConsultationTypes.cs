using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <summary>
    /// ADR-132: gives every existing consultant the two consultation types a new consultant now
    /// starts with ("Review Consultation" and "Emergency / Casualty Doctor's Consultation"), so
    /// they can be booked and billed for review and emergency visits too. Data only, no schema
    /// change. The types are matched by name the same way the Add Consultant form does it
    /// (masters/configs/consultant.ts): case, extra spaces and curly apostrophes are ignored,
    /// and only active types count. A hospital without one of them is skipped for that type.
    /// A consultant who already has the type keeps their row and charge. The consultant charge
    /// on a new row is left blank, the same as ticking the box by hand.
    /// </summary>
    public partial class BackfillConsultantDefaultConsultationTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO masters.consultant_consultation_types (id, consultant_id, consultation_type_id, consultant_charge)
                SELECT gen_random_uuid(), c.id, t.id, NULL
                FROM masters.consultants c
                CROSS JOIN masters.consultation_types t
                WHERE c.is_deleted = false
                  AND t.is_deleted = false
                  AND t.is_active = true
                  AND lower(btrim(regexp_replace(translate(t.name, '‘’', ''''''), '\s+', ' ', 'g')))
                      IN ('review consultation', 'emergency / casualty doctor''s consultation')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM masters.consultant_consultation_types x
                      WHERE x.consultant_id = c.id
                        AND x.consultation_type_id = t.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo safely: a row added here can't be told apart from one staff
            // ticked on the consultant's page afterwards.
        }
    }
}
