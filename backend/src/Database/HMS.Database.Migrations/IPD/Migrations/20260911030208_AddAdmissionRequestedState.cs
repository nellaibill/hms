using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.IPD.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionRequestedState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_admissions_active_patient",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.AlterColumn<Guid>(
                name: "ward_id",
                schema: "ipd",
                table: "admissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "bed_id",
                schema: "ipd",
                table: "admissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "ux_admissions_active_patient",
                schema: "ipd",
                table: "admissions",
                column: "patient_id",
                unique: true,
                filter: "status IN ('Admitted', 'Requested') AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_admissions_active_patient",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.AlterColumn<Guid>(
                name: "ward_id",
                schema: "ipd",
                table: "admissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "bed_id",
                schema: "ipd",
                table: "admissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_admissions_active_patient",
                schema: "ipd",
                table: "admissions",
                column: "patient_id",
                unique: true,
                filter: "status = 'Admitted' AND is_deleted = false");
        }
    }
}
