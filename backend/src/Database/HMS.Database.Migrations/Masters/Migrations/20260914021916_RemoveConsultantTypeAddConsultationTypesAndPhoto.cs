using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConsultantTypeAddConsultationTypesAndPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consultant_type",
                schema: "masters",
                table: "consultants");

            migrationBuilder.AddColumn<string>(
                name: "photo_url",
                schema: "masters",
                table: "consultants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "consultant_consultation_types",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    consultant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consultation_type_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consultant_consultation_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_consultant_consultation_types_consultant_id",
                        column: x => x.consultant_id,
                        principalSchema: "masters",
                        principalTable: "consultants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_consultant_consultation_types_consultant_id",
                schema: "masters",
                table: "consultant_consultation_types",
                column: "consultant_id");

            migrationBuilder.CreateIndex(
                name: "ix_consultant_consultation_types_consultation_type_id",
                schema: "masters",
                table: "consultant_consultation_types",
                column: "consultation_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consultant_consultation_types",
                schema: "masters");

            migrationBuilder.DropColumn(
                name: "photo_url",
                schema: "masters",
                table: "consultants");

            migrationBuilder.AddColumn<string>(
                name: "consultant_type",
                schema: "masters",
                table: "consultants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
