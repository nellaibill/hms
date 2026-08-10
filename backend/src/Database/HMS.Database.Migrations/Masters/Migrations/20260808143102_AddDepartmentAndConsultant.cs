using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Masters.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentAndConsultant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departments",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consultants",
                schema: "masters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    specialization = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
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
                    table.PrimaryKey("pk_consultants", x => x.id);
                    table.ForeignKey(
                        name: "fk_consultants_department_id",
                        column: x => x.department_id,
                        principalSchema: "masters",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_consultants_department_id",
                schema: "masters",
                table: "consultants",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ux_consultants_code",
                schema: "masters",
                table: "consultants",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_departments_code",
                schema: "masters",
                table: "departments",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            // Department is being consolidated out of HR into Masters (see docs/DecisionLog.md)
            // — copy any existing hr.departments rows across, preserving their original ids,
            // so weekly_rosters.department_id/shift_assignments.department_id (plain, FK-less
            // Guid columns validated at the application layer) keep pointing at valid rows once
            // a later HR migration drops hr.departments. Guarded by an existence check because
            // Masters migrates before HR (see HMS.Api/Program.cs) — on a brand-new database,
            // hr.departments won't exist yet at this point, and there's nothing to copy.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'hr' AND table_name = 'departments') THEN
                        INSERT INTO masters.departments (id, code, name, is_active, created_at, created_by, updated_at, updated_by, is_deleted, deleted_at, deleted_by)
                        SELECT id, code, name, is_active, created_at, created_by, updated_at, updated_by, is_deleted, deleted_at, deleted_by
                        FROM hr.departments
                        ON CONFLICT (id) DO NOTHING;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consultants",
                schema: "masters");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "masters");
        }
    }
}
