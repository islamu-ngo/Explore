using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audience_ages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    min_age = table.Column<int>(type: "integer", nullable: true),
                    max_age = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience_ages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audience_genders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience_genders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "education_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_education_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "program_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_status_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    file_type_id = table.Column<int>(type: "integer", nullable: false),
                    uri = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    extension = table.Column<string>(type: "text", nullable: false),
                    size = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_files_file_type_file_type_id",
                        column: x => x.file_type_id,
                        principalTable: "file_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    postcode = table.Column<int>(type: "integer", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    status_type_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                    table.ForeignKey(
                        name: "fk_organizations_status_types_status_type_id",
                        column: x => x.status_type_id,
                        principalTable: "status_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "programs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    program_type_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    audience_gender_id = table.Column<int>(type: "integer", nullable: false),
                    audience_age_id = table.Column<int>(type: "integer", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    audience_attendees = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<double>(type: "double precision", nullable: false),
                    featured_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_views = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_registration_required = table.Column<bool>(type: "boolean", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    post_code = table.Column<int>(type: "integer", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    program_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programs", x => x.id);
                    table.ForeignKey(
                        name: "fk_programs_audience_ages_audience_age_id",
                        column: x => x.audience_age_id,
                        principalTable: "audience_ages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_programs_audience_genders_audience_gender_id",
                        column: x => x.audience_gender_id,
                        principalTable: "audience_genders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_programs_files_featured_image_id",
                        column: x => x.featured_image_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_programs_program_types_program_type_id",
                        column: x => x.program_type_id,
                        principalTable: "program_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "educations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    education_type_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_educations", x => x.id);
                    table.ForeignKey(
                        name: "fk_educations_education_types_education_type_id",
                        column: x => x.education_type_id,
                        principalTable: "education_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_educations_programs_id",
                        column: x => x.id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    event_type_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_events_event_types_event_type_id",
                        column: x => x.event_type_id,
                        principalTable: "event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_events_programs_id",
                        column: x => x.id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_registartions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_type_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_registartions", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_registartions_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_program_registartions_status_types_status_type_id",
                        column: x => x.status_type_id,
                        principalTable: "status_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "audience_ages",
                columns: new[] { "id", "full_name", "max_age", "min_age" },
                values: new object[,]
                {
                    { 1, "All Ages", null, null },
                    { 2, "Adults Only (18+)", null, 18 },
                    { 3, "Teens & Adults (16+)", null, 16 },
                    { 4, "Preteens & Up (12+)", null, 12 },
                    { 5, "Young Children (0-6)", 6, null },
                    { 6, "Children (0-12)", 12, null },
                    { 7, "Children & Young Teens (0-16)", 16, null },
                    { 8, "Youth (0-18)", 18, null }
                });

            migrationBuilder.InsertData(
                table: "audience_genders",
                columns: new[] { "id", "full_name" },
                values: new object[,]
                {
                    { 1, "Man" },
                    { 2, "Woman" },
                    { 3, "Both" }
                });

            migrationBuilder.InsertData(
                table: "event_types",
                columns: new[] { "id", "description", "full_name" },
                values: new object[,]
                {
                    { 1, null, "Conference" },
                    { 2, null, "Webinar" },
                    { 3, null, "Workshop" }
                });

            migrationBuilder.InsertData(
                table: "program_types",
                columns: new[] { "id", "description", "full_name" },
                values: new object[,]
                {
                    { 1, "Events like Conferences, Webinars, Workshops & More!", "Event" },
                    { 2, "Educations like Schools, Bootcamps & More!", "Education" }
                });

            migrationBuilder.InsertData(
                table: "status_types",
                columns: new[] { "id", "description", "full_name" },
                values: new object[,]
                {
                    { 1, "Status is pending approval of Admin verifying the Existence of Legal Entity", "Pending" },
                    { 2, "Status has been approved by Admin after verifying the Existence of Legal Entity", "Approved" },
                    { 3, "Status has been rejected by Admin after failing to verify the Existence of Legal Entity", "Rejected" }
                });

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "id", "address", "city", "country", "email", "full_name", "postcode", "status_type_id", "website_url" },
                values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000001"), "Parc Du Peterbos ...", "Brussels", "Belgium", "contact@openislamu.org", "ISLAMU", 1070, 2, "https://islamu.ngo" });

            migrationBuilder.CreateIndex(
                name: "ix_educations_education_type_id",
                table: "educations",
                column: "education_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_event_type_id",
                table: "events",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_files_file_type_id",
                table: "files",
                column: "file_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_status_type_id",
                table: "organizations",
                column: "status_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_program_registartions_program_id",
                table: "program_registartions",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "ix_program_registartions_status_type_id",
                table: "program_registartions",
                column: "status_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_programs_audience_age_id",
                table: "programs",
                column: "audience_age_id");

            migrationBuilder.CreateIndex(
                name: "ix_programs_audience_gender_id",
                table: "programs",
                column: "audience_gender_id");

            migrationBuilder.CreateIndex(
                name: "ix_programs_featured_image_id",
                table: "programs",
                column: "featured_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_programs_program_type_id",
                table: "programs",
                column: "program_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "educations");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "organization_members");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropTable(
                name: "program_registartions");

            migrationBuilder.DropTable(
                name: "education_types");

            migrationBuilder.DropTable(
                name: "event_types");

            migrationBuilder.DropTable(
                name: "programs");

            migrationBuilder.DropTable(
                name: "status_types");

            migrationBuilder.DropTable(
                name: "audience_ages");

            migrationBuilder.DropTable(
                name: "audience_genders");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "program_types");

            migrationBuilder.DropTable(
                name: "file_type");
        }
    }
}
