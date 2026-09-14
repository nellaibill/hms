using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConsultationTypeCostPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost_price",
                schema: "masters",
                table: "consultation_types");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cost_price",
                schema: "masters",
                table: "consultation_types",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
