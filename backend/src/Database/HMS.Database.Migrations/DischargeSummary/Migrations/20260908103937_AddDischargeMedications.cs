using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.DischargeSummary.Migrations
{
    /// <inheritdoc />
    public partial class AddDischargeMedications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discharge_medications",
                schema: "discharge_summary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    discharge_summary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    drug_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    route = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    morning_qty = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    noon_qty = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    evening_qty = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    night_qty = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    food_instruction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discharge_medications", x => x.id);
                    table.ForeignKey(
                        name: "fk_discharge_medications_discharge_summary_id",
                        column: x => x.discharge_summary_id,
                        principalSchema: "discharge_summary",
                        principalTable: "discharge_summaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discharge_medications_discharge_summary_id",
                schema: "discharge_summary",
                table: "discharge_medications",
                column: "discharge_summary_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discharge_medications",
                schema: "discharge_summary");
        }
    }
}
