using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "consultant_id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_consultant_id",
                schema: "identity",
                table: "users",
                column: "consultant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_consultant_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "consultant_id",
                schema: "identity",
                table: "users");
        }
    }
}
