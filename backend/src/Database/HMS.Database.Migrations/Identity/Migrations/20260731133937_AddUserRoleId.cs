using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_users_role_id",
                schema: "identity",
                table: "users",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_role_id",
                schema: "identity",
                table: "users",
                column: "role_id",
                principalSchema: "identity",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_role_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_role_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_id",
                schema: "identity",
                table: "users");
        }
    }
}
