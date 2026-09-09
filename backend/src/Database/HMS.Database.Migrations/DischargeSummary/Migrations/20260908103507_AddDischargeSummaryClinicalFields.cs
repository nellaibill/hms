using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Database.Migrations.DischargeSummary.Migrations
{
    /// <inheritdoc />
    public partial class AddDischargeSummaryClinicalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "activity",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "anaesthesia",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "anaesthetist",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "assistant_surgeons",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "blood_pressure",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "chief_complaints",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cns_findings",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "condition_at_discharge",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "course_in_hospital",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cvs_findings",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "diet",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_instructions",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_history",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gait",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "general_examination",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "height_cm",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "history_of_presenting_illness",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "intra_operative_findings",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "local_examination",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "operative_notes",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pa_findings",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "past_medical_history",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "past_surgical_history",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personal_history",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "physiotherapy",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_surgeon",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "procedure_date_time",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "procedure_name",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pulse_rate",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "respiratory_rate",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_instructions",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rs_findings",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "spo2_percent",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "surgical_position",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "temperature_f",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_kg",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wound_care",
                schema: "discharge_summary",
                table: "discharge_summaries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activity",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "anaesthesia",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "anaesthetist",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "assistant_surgeons",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "blood_pressure",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "chief_complaints",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "cns_findings",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "condition_at_discharge",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "course_in_hospital",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "cvs_findings",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "diet",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "emergency_instructions",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "family_history",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "gait",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "general_examination",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "height_cm",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "history_of_presenting_illness",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "intra_operative_findings",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "local_examination",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "operative_notes",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "pa_findings",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "past_medical_history",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "past_surgical_history",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "personal_history",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "physiotherapy",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "primary_surgeon",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "procedure_date_time",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "procedure_name",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "pulse_rate",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "respiratory_rate",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "review_instructions",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "rs_findings",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "spo2_percent",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "surgical_position",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "temperature_f",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "weight_kg",
                schema: "discharge_summary",
                table: "discharge_summaries");

            migrationBuilder.DropColumn(
                name: "wound_care",
                schema: "discharge_summary",
                table: "discharge_summaries");
        }
    }
}
