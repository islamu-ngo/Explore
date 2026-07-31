using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowOwnerlessDeletedActorTombstones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_actors_exactly_one_owner",
                table: "actors");

            migrationBuilder.AddCheckConstraint(
                name: "ck_actors_exactly_one_owner",
                table: "actors",
                sql: "num_nonnulls(user_id, organization_id, group_id, external_actor_subject_id, service_principal_id) = 1 OR (is_deleted AND num_nonnulls(user_id, organization_id, group_id, external_actor_subject_id, service_principal_id) = 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM actors
                        WHERE is_deleted
                            AND num_nonnulls(
                                user_id,
                                organization_id,
                                group_id,
                                external_actor_subject_id,
                                service_principal_id) = 0)
                    THEN
                        RAISE EXCEPTION 'Cannot downgrade while deleted ownerless Actor tombstones exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_actors_exactly_one_owner",
                table: "actors");

            migrationBuilder.AddCheckConstraint(
                name: "ck_actors_exactly_one_owner",
                table: "actors",
                sql: "num_nonnulls(user_id, organization_id, group_id, external_actor_subject_id, service_principal_id) = 1");
        }
    }
}
