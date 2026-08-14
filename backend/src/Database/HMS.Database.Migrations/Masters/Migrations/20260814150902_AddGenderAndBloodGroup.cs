using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderAndBloodGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blood_groups",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("pk_blood_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "genders",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("pk_genders", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "masters",
                table: "blood_groups",
                columns: new[] { "id", "code", "created_at", "created_by", "deleted_at", "deleted_by", "is_active", "name", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("019a0000-0000-7000-8000-000000000011"), "A_POS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "A+", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000012"), "A_NEG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "A-", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000013"), "B_POS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "B+", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000014"), "B_NEG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "B-", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000015"), "O_POS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "O+", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000016"), "O_NEG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "O-", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000017"), "AB_POS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "AB+", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000018"), "AB_NEG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "AB-", null, null }
                });

            migrationBuilder.InsertData(
                schema: "masters",
                table: "genders",
                columns: new[] { "id", "code", "created_at", "created_by", "deleted_at", "deleted_by", "is_active", "name", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("019a0000-0000-7000-8000-000000000001"), "MALE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "Male", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000002"), "FEMALE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "Female", null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000003"), "OTHER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "Other", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "ux_blood_groups_code",
                schema: "masters",
                table: "blood_groups",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_genders_code",
                schema: "masters",
                table: "genders",
                column: "code",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blood_groups",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "genders",
                schema: "masters");
        }
    }
}
