// ABOUTME: Wrapper DTO for partial actor updates using nullable logical groups.
// ABOUTME: Body IDs and tenant IDs are absent because PATCH routes use route/context authority.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Actor;

public sealed record UpdateActorDto
{
    public UpdateActorProfileDto? Profile { get; init; }
    public UpdateActorProfileImageDto? ProfileImage { get; init; }
    public UpdateActorAppearanceDto? Appearance { get; init; }
}

public sealed record UpdateActorProfileDto
{
    public int? ActorTypeId { get; init; }
    public string? DisplayName { get; init; }
    public OptionalUpdate<string?> Description { get; init; } = OptionalUpdate<string?>.Unspecified();
}

public sealed record UpdateActorProfileImageDto
{
    public OptionalUpdate<Guid?> ProfilePictureId { get; init; } = OptionalUpdate<Guid?>.Unspecified();
}
