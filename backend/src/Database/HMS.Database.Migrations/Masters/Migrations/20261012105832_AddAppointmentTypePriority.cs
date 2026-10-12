using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentTypePriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "priority",
                schema: "masters",
                table: "appointment_types",
                type: "integer",
                nullable: true);

            // Gives the four standard seeded appointment types (see
            // cicd/scripts/seed-appointment-and-consultation-types.ps1) their reception-desk
            // order on every already-provisioned tenant, so the picker changes without an
            // admin editing each record. A tenant's own renamed/extra types are untouched —
            // they stay unprioritized (alphabetical, after these) until set in Masters.
            migrationBuilder.Sql(
                """
                UPDATE masters.appointment_types
                SET priority = CASE name
                    WHEN 'Regular (Walk-in)' THEN 1
                    WHEN 'Scheduled' THEN 2
                    WHEN 'Phone Call' THEN 3
                    WHEN 'Online (Website/WhatsApp)' THEN 4
                END
                WHERE name IN ('Regular (Walk-in)', 'Scheduled', 'Phone Call', 'Online (Website/WhatsApp)');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "priority",
                schema: "masters",
                table: "appointment_types");
        }
    }
}
