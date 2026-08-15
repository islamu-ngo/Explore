// ABOUTME: Closed trusted authorization fact records for active resource boundaries.
// ABOUTME: Replaces ad-hoc attribute bags at provider boundaries without adding a second port.

using Explore.Domain;

namespace Explore.Application.Authorization;

public sealed record EventAuthorizationFacts(
    Guid TenantId,
    Guid EventId,
    Guid ActorId,
    Guid? UserId,
    Guid? OrganizationId,
    Guid? GroupId,
    Guid? OrganizerActorId,
    Guid? OrganizerUserId,
    Guid? OrganizerOrganizationId,
    Guid? OrganizerGroupId,
    string? ProvenanceType,
    Guid? SubmittedByUserId) : IAuthorizationFacts;

public sealed record EventSessionAuthorizationFacts(
    Guid TenantId,
    Guid EventId,
    Guid? EventSessionId,
    string? AuthorizationPhase) : IAuthorizationFacts;

public sealed record EventOrganizerClaimAuthorizationFacts(
    Guid TenantId,
    Guid EventId,
    Guid ClaimId,
    Guid ClaimantActorId,
    Guid? ClaimantUserId,
    Guid? ClaimantOrganizationId,
    Guid? ClaimantGroupId,
    string Status) : IAuthorizationFacts;

public sealed record OrganizationMemberAuthorizationFacts(
    Guid TenantId,
    Guid OrganizationId,
    Guid? MemberId,
    Guid? UserId) : IAuthorizationFacts;

public sealed record ContactShareAuthorizationFacts(
    Guid TenantId,
    Guid OrganizationId) : IAuthorizationFacts;

public sealed record SupportAccessSessionAuthorizationFacts(
    Guid TenantId,
    Guid? SessionId,
    Guid? ActorUserId,
    string? Mode,
    string? Status) : IAuthorizationFacts;

public sealed record PersistedStorageObjectAuthorizationFacts(
    Guid TenantId,
    Guid StorageObjectId,
    string Visibility,
    string LifecycleState,
    string? CreatedBy,
    string? OwningResourceKind,
    Guid? OwningResourceId) : IAuthorizationFacts;

public sealed record WebhookOwnershipAuthorizationFacts(
    WebhookConsumerKind Kind,
    Guid OwnerId,
    Guid? TenantId,
    Guid? InstanceId,
    Guid? OrganizationId,
    Guid? GroupId,
    Guid? UserId) : IAuthorizationFacts
{
    public static WebhookOwnershipAuthorizationFacts From(WebhookOwnershipScope ownership) => new(
        ownership.Kind,
        ownership.OwnerId,
        ownership.TenantId,
        ownership.InstanceId,
        ownership.OrganizationId,
        ownership.GroupId,
        ownership.UserId);
}
