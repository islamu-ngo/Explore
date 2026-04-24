using Explore.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

[DbContext(typeof(ExploreDbContext))]
[Migration("20260424123000_AddEventWithSessionsView")]
public partial class AddEventWithSessionsView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE VIEW public.vw_event_with_sessions AS
            WITH session_summary AS (
                SELECT
                    es.event_id,
                    COUNT(*)::integer AS session_count,
                    MIN(es.start_time) AS first_session_start_at,
                    MAX(es.end_time) AS last_session_end_at,
                    BOOL_OR(es.location_id IS NOT NULL OR es.room_id IS NOT NULL) AS has_in_person_sessions,
                    BOOL_OR(es.location_id IS NULL AND es.room_id IS NULL) AS has_virtual_sessions,
                    NULL::text AS aggregated_session_islamic_themes
                FROM public.event_sessions es
                WHERE es.is_deleted = false
                GROUP BY es.event_id
            ),
            event_property_values AS (
                SELECT
                    p.event_id,
                    p.namespace,
                    p.key,
                    jsonb_agg(
                        COALESCE(
                            to_jsonb(p.text_value),
                            to_jsonb(p.number_value),
                            to_jsonb(p.boolean_value),
                            to_jsonb(p.date_time_value),
                            'null'::jsonb)
                        ORDER BY p.ordinal) AS values
                FROM public.event_custom_property_projections p
                GROUP BY p.event_id, p.namespace, p.key
            ),
            event_property_facets AS (
                SELECT
                    epv.event_id,
                    COALESCE(jsonb_object_agg(epv.namespace || '/' || epv.key, epv.values), '{}'::jsonb)::text AS event_custom_property_facets
                FROM event_property_values epv
                GROUP BY epv.event_id
            ),
            session_property_values AS (
                SELECT DISTINCT
                    es.event_id,
                    p.namespace,
                    p.key,
                    COALESCE(
                        to_jsonb(p.text_value),
                        to_jsonb(p.number_value),
                        to_jsonb(p.boolean_value),
                        to_jsonb(p.date_time_value),
                        'null'::jsonb) AS facet_value
                FROM public.event_session_custom_property_projections p
                INNER JOIN public.event_sessions es ON es.id = p.event_session_id
                WHERE es.is_deleted = false
            ),
            session_property_grouped AS (
                SELECT
                    spv.event_id,
                    spv.namespace,
                    spv.key,
                    jsonb_agg(spv.facet_value) AS values
                FROM session_property_values spv
                GROUP BY spv.event_id, spv.namespace, spv.key
            ),
            session_property_facets AS (
                SELECT
                    spg.event_id,
                    COALESCE(jsonb_object_agg(spg.namespace || '/' || spg.key, spg.values), '{}'::jsonb)::text AS event_session_custom_property_facets
                FROM session_property_grouped spg
                GROUP BY spg.event_id
            )
            SELECT
                e.id AS event_id,
                e.tenant_id,
                e.title,
                COALESCE(e.slug, '') AS slug,
                e.description,
                COALESCE(e.first_session_start_utc, ss.first_session_start_at, e.created_at) AS start_at,
                COALESCE(ss.last_session_end_at, e.last_session_start_utc) AS end_at,
                est.master_code AS status,
                vt.master_code AS visibility,
                e.is_deleted,
                e.created_at,
                e.updated_at,
                NULL::text AS islamic_theme,
                m.master_code AS madhab,
                NULL::boolean AS is_ramadan,
                CASE
                    WHEN eia.id IS NULL THEN NULL
                    WHEN eia.reference_prayer IS NOT NULL OR eia.prayer_time_offset IS NOT NULL THEN true
                    ELSE false
                END AS prayer_aware,
                eta.tech_stack_tags AS tech_stack,
                CASE eta.skill_level
                    WHEN 0 THEN 'AllLevels'
                    WHEN 1 THEN 'Beginner'
                    WHEN 2 THEN 'Intermediate'
                    WHEN 3 THEN 'Advanced'
                    ELSE NULL
                END AS difficulty_level,
                NULL::text AS target_audience,
                COALESCE(ss.session_count, 0) AS session_count,
                ss.first_session_start_at,
                ss.last_session_end_at,
                COALESCE(ss.has_in_person_sessions, false) AS has_in_person_sessions,
                COALESCE(ss.has_virtual_sessions, false) AS has_virtual_sessions,
                ss.aggregated_session_islamic_themes,
                COALESCE(epf.event_custom_property_facets, '{}'::jsonb::text) AS event_custom_property_facets,
                COALESCE(spf.event_session_custom_property_facets, '{}'::jsonb::text) AS event_session_custom_property_facets
            FROM public.events e
            INNER JOIN public.event_statuses est ON est.id = e.event_status_id
            INNER JOIN public.visibility_types vt ON vt.id = e.visibility_type_id
            LEFT JOIN public.event_islamic_aspects eia ON eia.id = e.id
            LEFT JOIN public.madhabs m ON m.id = eia.madhab_id
            LEFT JOIN public.event_tech_aspects eta ON eta.id = e.id
            LEFT JOIN session_summary ss ON ss.event_id = e.id
            LEFT JOIN event_property_facets epf ON epf.event_id = e.id
            LEFT JOIN session_property_facets spf ON spf.event_id = e.id
            WHERE e.is_deleted = false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW public.vw_event_with_sessions;");
    }
}
