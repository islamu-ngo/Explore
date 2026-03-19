// ABOUTME: Reserved Layer 2 semantic identifiers that Layer 3 custom properties must not redefine.
// ABOUTME: Gives future validators and handlers a single source for collision prevention with typed sector schema.

namespace Explore.Domain.Constants;

public static class CustomPropertySemanticReservations
{
    public static readonly IReadOnlyCollection<ReservedSemanticKey> Layer2EventSemantics =
    [
        new("sector.islamic", "madhab_id"),
        new("sector.islamic", "reference_prayer"),
        new("sector.islamic", "gender_mode"),
        new("sector.islamic", "includes_quran_recitation"),
        new("sector.islamic", "primary_language_id"),
        new("sector.tech", "github_repo_url"),
        new("sector.tech", "hackathon_track"),
        new("sector.tech", "skill_level"),
        new("sector.tech", "tech_stack_tags"),
        new("sector.tech", "requires_laptop"),
        new("sector.tech", "is_coding_competition"),
    ];

    public static bool IsReservedLayer2Semantic(string? namespaceValue, string? key)
    {
        if (string.IsNullOrWhiteSpace(namespaceValue) || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return Layer2EventSemantics.Any(reservation =>
            reservation.Namespace.Equals(namespaceValue, StringComparison.OrdinalIgnoreCase)
            && reservation.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public readonly record struct ReservedSemanticKey(string Namespace, string Key);
}
