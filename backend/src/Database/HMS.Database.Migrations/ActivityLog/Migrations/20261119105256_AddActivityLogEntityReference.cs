using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.ActivityLog.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLogEntityReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "entity_reference",
                schema: "activity_log",
                table: "activity_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "entity_reference",
                schema: "activity_log",
                table: "activity_logs");
        }
    }
}
