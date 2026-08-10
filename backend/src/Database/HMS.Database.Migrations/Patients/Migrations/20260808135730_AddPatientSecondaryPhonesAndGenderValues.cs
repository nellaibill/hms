using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientSecondaryPhonesAndGenderValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "alternate_phone2",
                schema: "patients",
                table: "patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alternate_phone2_relation",
                schema: "patients",
                table: "patients",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alternate_phone_relation",
                schema: "patients",
                table: "patients",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alternate_phone2",
                schema: "patients",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "alternate_phone2_relation",
                schema: "patients",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "alternate_phone_relation",
                schema: "patients",
                table: "patients");
        }
    }
}
