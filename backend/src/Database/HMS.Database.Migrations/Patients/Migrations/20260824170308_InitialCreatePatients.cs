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
                    blood_group = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    marital_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    primary_phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    secondary_phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    profession = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    id_proof_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    id_proof_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    mode_of_arrival_source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    mode_of_arrival_channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    mode_of_arrival_specify = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                name: "addresses",
                schema: "patients",
                columns: table => new
                {
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_line_1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_line_2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line_3 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    district_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pincode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.patient_id);
                    table.ForeignKey(
                        name: "fk_addresses_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patients",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "allergies",
                schema: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allergy_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    specify = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allergies", x => x.id);
                    table.ForeignKey(
                        name: "fk_allergies_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patients",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emergency_contacts",
                schema: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emergency_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_emergency_contacts_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patients",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_allergies_patient_id",
                schema: "patients",
                table: "allergies",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contacts_patient_id",
                schema: "patients",
                table: "emergency_contacts",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_id_proof_number",
                schema: "patients",
                table: "patients",
                column: "id_proof_number");

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
                name: "addresses",
                schema: "patients");

            migrationBuilder.DropTable(
                name: "allergies",
                schema: "patients");

            migrationBuilder.DropTable(
                name: "emergency_contacts",
                schema: "patients");

            migrationBuilder.DropTable(
                name: "patients",
                schema: "patients");

            migrationBuilder.DropSequence(
                name: "uhid_seq",
                schema: "patients");
        }
    }
}
