using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Laboratory.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateLaboratory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "laboratory");

            migrationBuilder.CreateSequence(
                name: "lab_order_no_seq",
                schema: "laboratory");

            migrationBuilder.CreateTable(
                name: "lab_orders",
                schema: "laboratory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lab_order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    patient_uhid = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    report_generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    report_generated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    report_released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    report_released_by = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_lab_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lab_order_items",
                schema: "laboratory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lab_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_line_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consultant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sample_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    collected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    collection_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sample_quantity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    collection_remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    rejection_remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    correction_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    correction_requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    correction_requested_by = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_for_verification_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_for_verification_by = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_lab_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_order_items_lab_orders_lab_order_id",
                        column: x => x.lab_order_id,
                        principalSchema: "laboratory",
                        principalTable: "lab_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lab_order_item_events",
                schema: "laboratory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lab_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_lab_order_item_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_order_item_events_lab_order_items_lab_order_item_id",
                        column: x => x.lab_order_item_id,
                        principalSchema: "laboratory",
                        principalTable: "lab_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lab_result_parameters",
                schema: "laboratory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lab_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parameter_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    result_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_range = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    flag = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_lab_result_parameters", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_result_parameters_lab_order_items_lab_order_item_id",
                        column: x => x.lab_order_item_id,
                        principalSchema: "laboratory",
                        principalTable: "lab_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_item_events_lab_order_item_id",
                schema: "laboratory",
                table: "lab_order_item_events",
                column: "lab_order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_item_events_occurred_at",
                schema: "laboratory",
                table: "lab_order_item_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_items_lab_order_id",
                schema: "laboratory",
                table: "lab_order_items",
                column: "lab_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_items_package_id",
                schema: "laboratory",
                table: "lab_order_items",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_items_status",
                schema: "laboratory",
                table: "lab_order_items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_created_at",
                schema: "laboratory",
                table: "lab_orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_patient_id",
                schema: "laboratory",
                table: "lab_orders",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ux_lab_orders_invoice_id",
                schema: "laboratory",
                table: "lab_orders",
                column: "invoice_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_lab_orders_lab_order_number",
                schema: "laboratory",
                table: "lab_orders",
                column: "lab_order_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lab_result_parameters_lab_order_item_id",
                schema: "laboratory",
                table: "lab_result_parameters",
                column: "lab_order_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lab_order_item_events",
                schema: "laboratory");

            migrationBuilder.DropTable(
                name: "lab_result_parameters",
                schema: "laboratory");

            migrationBuilder.DropTable(
                name: "lab_order_items",
                schema: "laboratory");

            migrationBuilder.DropTable(
                name: "lab_orders",
                schema: "laboratory");

            migrationBuilder.DropSequence(
                name: "lab_order_no_seq",
                schema: "laboratory");
        }
    }
}
