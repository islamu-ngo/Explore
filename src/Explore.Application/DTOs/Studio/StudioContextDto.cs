// ABOUTME: Link-only private state used to assemble actor-scoped Studio navigation.
// ABOUTME: Keeps authority, actor identity, tenant inventory, and role facts out of serialized responses.

using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.Studio;

public sealed class StudioContextDto
{
    [JsonIgnore]
    public Guid? SelectedActorId { get; set; }

    [JsonIgnore]
    public ISet<string> AllowedLinkRelations { get; } = new HashSet<string>(StringComparer.Ordinal);
}
