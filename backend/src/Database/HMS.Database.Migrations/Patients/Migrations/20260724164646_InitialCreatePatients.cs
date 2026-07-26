using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.Patients.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatePatients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "patients");

            migrationBuilder.CreateSequence(
                name: "registration_no_seq",
                schema: "patients");

            migrationBuilder.CreateSequence(
                name: "uhid_seq",
                schema: "patients");

            migrationBuilder.CreateTable(
                name: "patients",
                schema: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uhid = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    blood_group = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line3 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pincode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    primary_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    primary_phone_relation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    alternate_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    profession = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    emergency_contact_relationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    emergency_contact_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    emergency_contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    has_known_allergy = table.Column<bool>(type: "boolean", nullable: false),
                    allergy_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    allergy_severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    photo_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    id_proof_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    id_proof_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_patients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patient_registrations",
                schema: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    encounter_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mode_of_arrival = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    consultant = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    admission_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    referral_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_patient_registrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_registrations_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patients",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patient_registrations_patient_id",
                schema: "patients",
                table: "patient_registrations",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ux_patient_registrations_registration_number",
                schema: "patients",
                table: "patient_registrations",
                column: "registration_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_patients_name",
                schema: "patients",
                table: "patients",
                columns: new[] { "first_name", "last_name" });

            migrationBuilder.CreateIndex(
                name: "ix_patients_primary_phone",
                schema: "patients",
                table: "patients",
                column: "primary_phone");

            migrationBuilder.CreateIndex(
                name: "ux_patients_uhid",
                schema: "patients",
                table: "patients",
                column: "uhid",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_registrations",
                schema: "patients");

            migrationBuilder.DropTable(
                name: "patients",
                schema: "patients");

            migrationBuilder.DropSequence(
                name: "registration_no_seq",
                schema: "patients");

            migrationBuilder.DropSequence(
                name: "uhid_seq",
                schema: "patients");
        }
    }
}
