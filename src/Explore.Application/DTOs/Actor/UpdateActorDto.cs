// ABOUTME: Wrapper DTO for partial actor updates using nullable logical groups.
// ABOUTME: Body IDs and tenant IDs are absent because PATCH routes use route/context authority.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Actor;

public class UpdateActorDto
{
    public UpdateActorProfileDto? Profile { get; set; }
    public UpdateActorProfileImageDto? ProfileImage { get; set; }
    public UpdateActorAppearanceDto? Appearance { get; set; }
}

public class UpdateActorProfileDto
{
    public int? ActorTypeId { get; set; }
    public string? DisplayName { get; set; }
    public OptionalUpdate<string?> Description { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateActorProfileImageDto
{
    public OptionalUpdate<Guid?> ProfilePictureId { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}
