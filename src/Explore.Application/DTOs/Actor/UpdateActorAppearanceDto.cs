// ABOUTME: Actor appearance update group with explicit field-operation semantics.
// ABOUTME: OptionalUpdate distinguishes absent fields from intentional clear/set operations.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Actor;

public sealed record UpdateActorAppearanceDto
{
    public OptionalUpdate<string?> BackgroundColor { get; init; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> BackgroundEffect { get; init; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> BannerColor { get; init; } = OptionalUpdate<string?>.Unspecified();
}
