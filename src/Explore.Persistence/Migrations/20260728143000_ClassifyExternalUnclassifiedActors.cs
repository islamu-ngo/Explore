// ABOUTME: Reclassifies legacy external-subject Actors from BOT to the dedicated external-unclassified lookup.
// ABOUTME: Adds a reversible database invariant binding ExternalActorSubject ownership to Actor type ID 6.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

[DbContext(typeof(ExploreDbContext))]
[Migration("20260728143000_ClassifyExternalUnclassifiedActors")]
public sealed class ClassifyExternalUnclassifiedActors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM actor_types
                    WHERE (id = 6 AND master_code <> 'EXTERNAL_UNCLASSIFIED')
                       OR (master_code = 'EXTERNAL_UNCLASSIFIED' AND id <> 6)
                ) THEN
                    RAISE EXCEPTION 'External-unclassified Actor type conflicts with the stable lookup contract.';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM actors
                    WHERE external_actor_subject_id IS NOT NULL
                      AND actor_type_id <> 3
                ) THEN
                    RAISE EXCEPTION 'External-subject Actor has an unexpected pre-migration Actor type.';
                END IF;
            END
            $$;

            INSERT INTO actor_types (id, master_code, full_name, description)
            VALUES (6, 'EXTERNAL_UNCLASSIFIED', 'External unclassified', 'Verified external subject awaiting explicit classification')
            ON CONFLICT (id) DO NOTHING;

            UPDATE actors
            SET actor_type_id = 6
            WHERE external_actor_subject_id IS NOT NULL
              AND actor_type_id = 3;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM actors
                    WHERE (external_actor_subject_id IS NULL AND actor_type_id = 6)
                       OR (external_actor_subject_id IS NOT NULL AND actor_type_id <> 6)
                ) THEN
                    RAISE EXCEPTION 'Actor external ownership and type did not converge.';
                END IF;
            END
            $$;
            """);

        migrationBuilder.AddCheckConstraint(
            name: "ck_actors_external_type_matches_owner",
            table: "actors",
            sql: "(external_actor_subject_id IS NULL AND actor_type_id <> 6) OR (external_actor_subject_id IS NOT NULL AND actor_type_id = 6)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_actors_external_type_matches_owner",
            table: "actors");

        migrationBuilder.Sql(
            """
            UPDATE actors
            SET actor_type_id = 3
            WHERE external_actor_subject_id IS NOT NULL
              AND actor_type_id = 6;

            DELETE FROM actor_types
            WHERE id = 6
              AND master_code = 'EXTERNAL_UNCLASSIFIED';
            """);
    }
}
