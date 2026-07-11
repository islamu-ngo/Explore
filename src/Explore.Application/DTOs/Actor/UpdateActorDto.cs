// ABOUTME: Wrapper DTO for partial actor updates using nullable logical groups.
// ABOUTME: Body IDs and tenant IDs are absent because PATCH routes use route/context authority.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Actor;

public class UpdateActorDto
{
    public UpdateActorProfileDto? Profile { get; set; }
    public UpdateActorProfileImageDto? ProfileImage { get; set; }
    public UpdateActorAppearanceDto? Appearance { get; set; }
    public UpdateActorFederationIdentifiersDto? FederationIdentifiers { get; set; }
    public UpdateActorFederationMetadataDto? FederationMetadata { get; set; }
}

public class UpdateActorProfileDto
{
    public int? ActorTypeId { get; set; }
    public string? DisplayName { get; set; }
}

public class UpdateActorProfileImageDto
{
    public OptionalUpdate<Guid?> ProfilePictureId { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public class UpdateActorFederationIdentifiersDto
{
    public OptionalUpdate<string?> Did { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> Handle { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<int?> DidCustodyTypeId { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateActorFederationMetadataDto
{
    public OptionalUpdate<string?> PdsHost { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> Description { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<DateTime?> IndexedAt { get; set; } = OptionalUpdate<DateTime?>.Unspecified();
    public OptionalUpdate<string?> ProfilePictureCid { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> ProfilePictureUri { get; set; } = OptionalUpdate<string?>.Unspecified();
}
