using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <summary>
    /// Empty on purpose — EF Core's PendingModelChangesWarning flagged the model as out of
    /// sync with the recorded snapshot checksum after the two migrations immediately before
    /// this one (AddConsultationType, RemoveCodeFromAppointmentTypeConsultantConsultationType),
    /// even though `dotnet ef migrations add` here produced no actual Up/Down SQL — i.e. the
    /// schema itself was already correct, only the snapshot's checksum record was stale. This
    /// migration exists solely to re-sync that checksum so app startup's migration step stops
    /// throwing; it changes nothing in the database.
    /// </summary>
    public partial class SyncMastersModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
