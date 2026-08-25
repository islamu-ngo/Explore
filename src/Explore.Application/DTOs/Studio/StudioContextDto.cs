// ABOUTME: Link-only private state used to assemble actor-scoped Studio navigation.
// ABOUTME: Keeps authority, actor identity, tenant inventory, and role facts out of serialized responses.

using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.Studio;

public sealed record StudioContextDto
{
    [JsonIgnore]
    public Guid? SelectedActorId { get; init; }

    [JsonIgnore]
    private IReadOnlySet<string> _allowedLinkRelations =
        Array.Empty<string>().ToFrozenSet(StringComparer.Ordinal);

    public IReadOnlySet<string> AllowedLinkRelations
    {
        get => _allowedLinkRelations;
        init => _allowedLinkRelations = value is null
            ? null!
            : value.ToFrozenSet(StringComparer.Ordinal);
    }
}
