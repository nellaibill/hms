using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Laboratory.Migrations
{
    /// <inheritdoc />
    public partial class AddLabOrderAdmissionOriginSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_lab_orders_invoice_id",
                schema: "laboratory",
                table: "lab_orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "visit_id",
                schema: "laboratory",
                table: "lab_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "invoice_id",
                schema: "laboratory",
                table: "lab_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "admission_id",
                schema: "laboratory",
                table: "lab_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "invoice_line_item_id",
                schema: "laboratory",
                table: "lab_order_items",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_admission_id",
                schema: "laboratory",
                table: "lab_orders",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ux_lab_orders_invoice_id",
                schema: "laboratory",
                table: "lab_orders",
                column: "invoice_id",
                unique: true,
                filter: "invoice_id IS NOT NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_lab_orders_admission_id",
                schema: "laboratory",
                table: "lab_orders");

            migrationBuilder.DropIndex(
                name: "ux_lab_orders_invoice_id",
                schema: "laboratory",
                table: "lab_orders");

            migrationBuilder.DropColumn(
                name: "admission_id",
                schema: "laboratory",
                table: "lab_orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "visit_id",
                schema: "laboratory",
                table: "lab_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "invoice_id",
                schema: "laboratory",
                table: "lab_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "invoice_line_item_id",
                schema: "laboratory",
                table: "lab_order_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_lab_orders_invoice_id",
                schema: "laboratory",
                table: "lab_orders",
                column: "invoice_id",
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
