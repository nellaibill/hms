using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformUserMfaAndChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "mfa_enabled",
                schema: "platform",
                table: "platform_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "mfa_secret",
                schema: "platform",
                table: "platform_users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "platform_mfa_challenges",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_mfa_challenges", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_platform_mfa_challenges_token",
                schema: "platform",
                table: "platform_mfa_challenges",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_mfa_challenges",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "mfa_enabled",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "mfa_secret",
                schema: "platform",
                table: "platform_users");
        }
    }
}
