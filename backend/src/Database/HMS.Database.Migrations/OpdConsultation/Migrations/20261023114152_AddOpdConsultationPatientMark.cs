using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.OpdConsultation.Migrations
{
    /// <inheritdoc />
    public partial class AddOpdConsultationPatientMark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mark_reason",
                schema: "opd_consultation",
                table: "opd_consultation_notes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "marked_at",
                schema: "opd_consultation",
                table: "opd_consultation_notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "marked_by",
                schema: "opd_consultation",
                table: "opd_consultation_notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_opd_consultation_notes_marked_at",
                schema: "opd_consultation",
                table: "opd_consultation_notes",
                column: "marked_at",
                filter: "marked_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_opd_consultation_notes_marked_at",
                schema: "opd_consultation",
                table: "opd_consultation_notes");

            migrationBuilder.DropColumn(
                name: "mark_reason",
                schema: "opd_consultation",
                table: "opd_consultation_notes");

            migrationBuilder.DropColumn(
                name: "marked_at",
                schema: "opd_consultation",
                table: "opd_consultation_notes");

            migrationBuilder.DropColumn(
                name: "marked_by",
                schema: "opd_consultation",
                table: "opd_consultation_notes");
        }
    }
}
