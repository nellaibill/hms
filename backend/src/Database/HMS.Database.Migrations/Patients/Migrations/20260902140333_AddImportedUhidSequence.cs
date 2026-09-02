using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedUhidSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "imported_uhid_seq",
                schema: "patients",
                maxValue: 40000L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "imported_uhid_seq",
                schema: "patients");
        }
    }
}
