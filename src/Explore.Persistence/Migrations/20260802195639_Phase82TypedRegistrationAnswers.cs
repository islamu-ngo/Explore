using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase82TypedRegistrationAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "applies_to_subject_key",
                table: "registration_requirements",
                type: "uuid",
                nullable: false,
                computedColumnSql: "COALESCE(applies_to_subject_id, '00000000-0000-0000-0000-000000000000'::uuid)",
                stored: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_ticket_assignments_tenant_id_registration_orde",
                table: "registration_ticket_assignments",
                columns: new[] { "tenant_id", "registration_order_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_ticket_assignments_tenant_id_registration_orde1",
                table: "registration_ticket_assignments",
                columns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_submissions_tenant_id_event_id_registration_or",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_form_id", "registration_form_version_id", "registration_attempt_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_requirements_tenant_id_event_id_registration_w1",
                table: "registration_requirements",
                columns: new[] { "tenant_id", "event_id", "registration_workflow_id", "id", "applies_to_subject_type_id", "applies_to_subject_key" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_order_lines_tenant_id_registration_order_id_id1",
                table: "registration_order_lines",
                columns: new[] { "tenant_id", "registration_order_id", "id", "ticket_type_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_form_fields_tenant_id_event_id_registration_fo1",
                table: "registration_form_fields",
                columns: new[] { "tenant_id", "event_id", "registration_form_id", "registration_form_version_id", "registration_form_section_id", "id", "field_type_id" });

            migrationBuilder.CreateTable(
                name: "registration_answer_subject_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    master_code = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_answer_subject_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registration_sensitive_answer_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ciphertext = table.Column<string>(type: "character varying(131072)", maxLength: 131072, nullable: false),
                    key_version = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_registration_sensitive_answer_values", x => x.id);
                    table.UniqueConstraint("ak_registration_sensitive_answer_values_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_sensitive_answer_values_shape", "key_version > 0 AND length(btrim(ciphertext)) > 0");
                    table.ForeignKey(
                        name: "fk_registration_sensitive_answer_values_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_form_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_form_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_type_id = table.Column<int>(type: "integer", nullable: false),
                    requirement_subject_type_id = table.Column<int>(type: "integer", nullable: false),
                    requirement_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requirement_subject_key = table.Column<Guid>(type: "uuid", nullable: false, computedColumnSql: "COALESCE(requirement_subject_id, '00000000-0000-0000-0000-000000000000'::uuid)", stored: true),
                    answer_subject_type_id = table.Column<int>(type: "integer", nullable: false),
                    order_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchaser_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    participant_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ticket_assignment_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ticket_assignment_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_selection_subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_subject_identity = table.Column<Guid>(type: "uuid", nullable: false, computedColumnSql: "COALESCE(order_subject_id, purchaser_subject_id, participant_subject_id, ticket_assignment_subject_id, session_selection_subject_id)", stored: true),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    text_value = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    integer_value = table.Column<long>(type: "bigint", nullable: true),
                    decimal_value = table.Column<decimal>(type: "numeric", nullable: true),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    date_value = table.Column<DateOnly>(type: "date", nullable: true),
                    time_value = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    instant_value = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    selected_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sensitive_answer_value_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_registration_answers", x => x.id);
                    table.CheckConstraint("ck_registration_answers_exactly_one_value", "num_nonnulls(text_value, integer_value, decimal_value, boolean_value, date_value, time_value, instant_value, selected_option_id, sensitive_answer_value_id) = 1");
                    table.CheckConstraint("ck_registration_answers_positive_ordinal", "ordinal > 0");
                    table.CheckConstraint("ck_registration_answers_subject_shape", "num_nonnulls(order_subject_id, purchaser_subject_id, participant_subject_id, ticket_assignment_subject_id, session_selection_subject_id) = 1 AND ((answer_subject_type_id = 1 AND order_subject_id = registration_order_id AND ticket_assignment_order_line_id IS NULL AND requirement_subject_type_id = 1) OR (answer_subject_type_id = 2 AND purchaser_subject_id = registration_order_id AND ticket_assignment_order_line_id IS NULL AND requirement_subject_type_id IN (1, 4)) OR (answer_subject_type_id = 3 AND participant_subject_id IS NOT NULL AND ticket_assignment_order_line_id IS NULL AND requirement_subject_type_id IN (3, 5)) OR (answer_subject_type_id = 4 AND ticket_assignment_subject_id IS NOT NULL AND ticket_assignment_order_line_id IS NOT NULL AND requirement_subject_id IS NOT NULL AND requirement_subject_type_id = 2) OR (answer_subject_type_id = 5 AND session_selection_subject_id = requirement_subject_id AND ticket_assignment_order_line_id IS NULL AND requirement_subject_type_id = 6))");
                    table.CheckConstraint("ck_registration_answers_value_matches_field_type", "(field_type_id IN (1, 2, 9, 10, 11, 12, 13) AND (text_value IS NOT NULL OR sensitive_answer_value_id IS NOT NULL)) OR (field_type_id IN (3, 16) AND (integer_value IS NOT NULL OR sensitive_answer_value_id IS NOT NULL)) OR (field_type_id = 4 AND (decimal_value IS NOT NULL OR sensitive_answer_value_id IS NOT NULL)) OR (field_type_id = 5 AND (boolean_value IS NOT NULL OR sensitive_answer_value_id IS NOT NULL)) OR (field_type_id = 6 AND (date_value IS NOT NULL OR sensitive_answer_value_id IS NOT NULL)) OR (field_type_id = 7 AND (time_value IS NOT NULL OR sensitive_answer_value_id IS NOT NULL)) OR (field_type_id = 8 AND (instant_value IS NOT NULL OR sensitive_answer_value_id IS NOT NULL)) OR (field_type_id IN (14, 15) AND selected_option_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_registration_answers_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_answer_subject_types_answ",
                        column: x => x.answer_subject_type_id,
                        principalTable: "registration_answer_subject_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_form_field_options_tenant",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_form_id, x.registration_form_version_id, x.registration_form_section_id, x.registration_form_field_id, x.selected_option_id },
                        principalTable: "registration_form_field_options",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_form_id", "registration_form_version_id", "registration_form_section_id", "registration_form_field_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_form_fields_tenant_id_eve",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_form_id, x.registration_form_version_id, x.registration_form_section_id, x.registration_form_field_id, x.field_type_id },
                        principalTable: "registration_form_fields",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_form_id", "registration_form_version_id", "registration_form_section_id", "id", "field_type_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_order_lines_tenant_id_reg",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.ticket_assignment_order_line_id, x.requirement_subject_id },
                        principalTable: "registration_order_lines",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "ticket_type_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_participants_tenant_id_re",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.participant_subject_id },
                        principalTable: "registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_requirements_tenant_id_ev",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_workflow_id, x.registration_requirement_id, x.requirement_subject_type_id, x.requirement_subject_key },
                        principalTable: "registration_requirements",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_workflow_id", "id", "applies_to_subject_type_id", "applies_to_subject_key" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_sensitive_answer_values_t",
                        columns: x => new { x.tenant_id, x.sensitive_answer_value_id },
                        principalTable: "registration_sensitive_answer_values",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_submissions_tenant_id_eve",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_workflow_id, x.registration_requirement_id, x.registration_form_id, x.registration_form_version_id, x.registration_attempt_id, x.registration_submission_id },
                        principalTable: "registration_submissions",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_form_id", "registration_form_version_id", "registration_attempt_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_registration_ticket_assignments_tenant",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.ticket_assignment_subject_id, x.ticket_assignment_order_line_id },
                        principalTable: "registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_answers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_answer_subject_type_id",
                table: "registration_answers",
                column: "answer_subject_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_event_id_registration_form_i",
                table: "registration_answers",
                columns: new[] { "tenant_id", "event_id", "registration_form_id", "registration_form_version_id", "registration_form_section_id", "registration_form_field_id", "field_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_event_id_registration_form_i1",
                table: "registration_answers",
                columns: new[] { "tenant_id", "event_id", "registration_form_id", "registration_form_version_id", "registration_form_section_id", "registration_form_field_id", "selected_option_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_event_id_registration_order_",
                table: "registration_answers",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_form_id", "registration_form_version_id", "registration_attempt_id", "registration_submission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_event_id_registration_workfl",
                table: "registration_answers",
                columns: new[] { "tenant_id", "event_id", "registration_workflow_id", "registration_requirement_id", "requirement_subject_type_id", "requirement_subject_key" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_partic",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "participant_subject_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_ticket",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_assignment_order_line_id", "requirement_subject_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_registration_order_id_ticket1",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_assignment_subject_id", "ticket_assignment_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_answers_tenant_id_sensitive_answer_value_id",
                table: "registration_answers",
                columns: new[] { "tenant_id", "sensitive_answer_value_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_registration_answers_durable_identity",
                table: "registration_answers",
                columns: new[] { "tenant_id", "registration_submission_id", "registration_form_field_id", "answer_subject_type_id", "effective_subject_identity", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_sensitive_answer_values_tenant_id_key_version",
                table: "registration_sensitive_answer_values",
                columns: new[] { "tenant_id", "key_version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_answers");

            migrationBuilder.DropTable(
                name: "registration_answer_subject_types");

            migrationBuilder.DropTable(
                name: "registration_sensitive_answer_values");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_ticket_assignments_tenant_id_registration_orde",
                table: "registration_ticket_assignments");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_ticket_assignments_tenant_id_registration_orde1",
                table: "registration_ticket_assignments");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_submissions_tenant_id_event_id_registration_or",
                table: "registration_submissions");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_requirements_tenant_id_event_id_registration_w1",
                table: "registration_requirements");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_order_lines_tenant_id_registration_order_id_id1",
                table: "registration_order_lines");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_form_fields_tenant_id_event_id_registration_fo1",
                table: "registration_form_fields");

            migrationBuilder.DropColumn(
                name: "applies_to_subject_key",
                table: "registration_requirements");
        }
    }
}
