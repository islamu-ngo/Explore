// ABOUTME: Actor appearance update group with explicit field-operation semantics.
// ABOUTME: OptionalUpdate distinguishes absent fields from intentional clear/set operations.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Actor;

public class UpdateActorAppearanceDto
{
    public OptionalUpdate<string?> BackgroundColor { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> BackgroundEffect { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> BannerColor { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<Guid?> BannerPictureId { get; set; } = OptionalUpdate<Guid?>.Unspecified();
    public OptionalUpdate<Guid?> BackgroundImageId { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}
