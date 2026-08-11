using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.IPD.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ipd");

            migrationBuilder.CreateSequence(
                name: "admission_no_seq",
                schema: "ipd");

            migrationBuilder.CreateTable(
                name: "admissions",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consultant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ward_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bed_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    admission_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason_for_admission = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discharge_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discharge_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    final_diagnosis = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    discharge_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    follow_up_advice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_admissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "beds",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ward_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bed_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    bed_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_beds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wards",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ward_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_wards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admission_charges",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_admission_charges", x => x.id);
                    table.ForeignKey(
                        name: "fk_admission_charges_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admission_charges_admission_id",
                schema: "ipd",
                table: "admission_charges",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ux_admissions_active_bed",
                schema: "ipd",
                table: "admissions",
                column: "bed_id",
                unique: true,
                filter: "status = 'Admitted' AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_admissions_active_patient",
                schema: "ipd",
                table: "admissions",
                column: "patient_id",
                unique: true,
                filter: "status = 'Admitted' AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_admissions_admission_number",
                schema: "ipd",
                table: "admissions",
                column: "admission_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_beds_ward_bed_number",
                schema: "ipd",
                table: "beds",
                columns: new[] { "ward_id", "bed_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_wards_code",
                schema: "ipd",
                table: "wards",
                column: "code",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admission_charges",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "beds",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "wards",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "admissions",
                schema: "ipd");

            migrationBuilder.DropSequence(
                name: "admission_no_seq",
                schema: "ipd");
        }
    }
}
