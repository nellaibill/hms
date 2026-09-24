using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.OpdConsultation.Migrations
{
    /// <inheritdoc />
    public partial class AddOpdPrescriptionsAndInvestigationService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "service_id",
                schema: "opd_consultation",
                table: "opd_consultation_investigations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "opd_consultation_prescriptions",
                schema: "opd_consultation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    opd_consultation_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    drug_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    route = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    frequency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    duration_days = table.Column<int>(type: "integer", nullable: true),
                    instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opd_consultation_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_opd_consultation_prescriptions_note_id",
                        column: x => x.opd_consultation_note_id,
                        principalSchema: "opd_consultation",
                        principalTable: "opd_consultation_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_opd_consultation_prescriptions_note_id",
                schema: "opd_consultation",
                table: "opd_consultation_prescriptions",
                column: "opd_consultation_note_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opd_consultation_prescriptions",
                schema: "opd_consultation");

            migrationBuilder.DropColumn(
                name: "service_id",
                schema: "opd_consultation",
                table: "opd_consultation_investigations");
        }
    }
}
