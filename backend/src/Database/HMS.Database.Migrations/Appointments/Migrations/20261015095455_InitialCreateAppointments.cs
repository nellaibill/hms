using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Appointments.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "appointments");

            migrationBuilder.CreateSequence(
                name: "appointment_no_seq",
                schema: "appointments");

            migrationBuilder.CreateTable(
                name: "appointments",
                schema: "appointments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    patient_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uhid = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consultant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    session = table.Column<int>(type: "integer", nullable: false),
                    seat_number = table.Column<int>(type: "integer", nullable: false),
                    token_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    arrived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    arrived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    no_show_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reschedule_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("pk_appointments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consultant_blocks",
                schema: "appointments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    consultant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    session = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
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
                    table.PrimaryKey("pk_consultant_blocks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_date_consultant",
                schema: "appointments",
                table: "appointments",
                columns: new[] { "appointment_date", "consultant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_patient_id",
                schema: "appointments",
                table: "appointments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_phone_number",
                schema: "appointments",
                table: "appointments",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ux_appointments_appointment_number",
                schema: "appointments",
                table: "appointments",
                column: "appointment_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_appointments_consultant_date_start_seat",
                schema: "appointments",
                table: "appointments",
                columns: new[] { "consultant_id", "appointment_date", "start_time", "seat_number" },
                unique: true,
                filter: "status IN ('Booked', 'Arrived') AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_consultant_blocks_consultant_dates",
                schema: "appointments",
                table: "consultant_blocks",
                columns: new[] { "consultant_id", "from_date", "to_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointments",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "consultant_blocks",
                schema: "appointments");

            migrationBuilder.DropSequence(
                name: "appointment_no_seq",
                schema: "appointments");
        }
    }
}
