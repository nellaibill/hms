using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Billing.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDiscountApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable at first: existing lines get their part from the SQL below (ADR-111).
            migrationBuilder.AddColumn<Guid>(
                name: "approval_id",
                schema: "billing",
                table: "invoice_discount_request_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "invoice_discount_approvals",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    consultant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    decided_on_behalf = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("pk_invoice_discount_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_discount_approvals_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "billing",
                        principalTable: "invoice_discount_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Requests raised before ADR-111 each become one Super Admin part carrying the
            // request's own status and decision, so a request still waiting stays with the Super
            // Admin, as it was raised.
            migrationBuilder.Sql(@"
INSERT INTO billing.invoice_discount_approvals
    (id, request_id, approver_type, consultant_id, status, decided_by, decided_by_name, decided_at,
     decision_notes, decided_on_behalf, created_at, created_by, updated_at, updated_by, is_deleted,
     deleted_at, deleted_by)
SELECT gen_random_uuid(), r.id, 'SuperAdmin', NULL, r.status, r.decided_by, r.decided_by_name, r.decided_at,
       r.decision_notes, false, r.created_at, r.created_by, r.updated_at, r.updated_by, r.is_deleted,
       r.deleted_at, r.deleted_by
FROM billing.invoice_discount_requests r;

UPDATE billing.invoice_discount_request_lines l
SET approval_id = a.id
FROM billing.invoice_discount_approvals a
WHERE a.request_id = l.request_id;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "approval_id",
                schema: "billing",
                table: "invoice_discount_request_lines",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoice_discount_request_lines_approval_id",
                schema: "billing",
                table: "invoice_discount_request_lines",
                column: "approval_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_discount_approvals_consultant_id_status",
                schema: "billing",
                table: "invoice_discount_approvals",
                columns: new[] { "consultant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_discount_approvals_request_id",
                schema: "billing",
                table: "invoice_discount_approvals",
                column: "request_id");

            migrationBuilder.AddForeignKey(
                name: "fk_invoice_discount_request_lines_approvals_approval_id",
                schema: "billing",
                table: "invoice_discount_request_lines",
                column: "approval_id",
                principalSchema: "billing",
                principalTable: "invoice_discount_approvals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_invoice_discount_request_lines_approvals_approval_id",
                schema: "billing",
                table: "invoice_discount_request_lines");

            migrationBuilder.DropTable(
                name: "invoice_discount_approvals",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "ix_invoice_discount_request_lines_approval_id",
                schema: "billing",
                table: "invoice_discount_request_lines");

            migrationBuilder.DropColumn(
                name: "approval_id",
                schema: "billing",
                table: "invoice_discount_request_lines");
        }
    }
}
