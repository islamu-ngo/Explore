// ABOUTME: Projects closed authorization fact records into provider wire attributes.
// ABOUTME: Keeps dictionary materialization out of Application request authorship.

using Explore.Domain;

namespace Explore.Application.Authorization;

public static class AuthorizationFactsAttributeProjection
{
    public static Dictionary<string, object>? ToAttributes(IAuthorizationFacts? facts) => facts switch
    {
        EventAuthorizationFacts value => Event(value),
        EventSessionAuthorizationFacts value => EventSession(value),
        EventOrganizerClaimAuthorizationFacts value => EventOrganizerClaim(value),
        OrganizationMemberAuthorizationFacts value => OrganizationMember(value),
        ContactShareAuthorizationFacts value => ContactShare(value),
        SupportAccessSessionAuthorizationFacts value => SupportAccessSession(value),
        PersistedStorageObjectAuthorizationFacts value => StorageObject(value),
        WebhookOwnershipAuthorizationFacts value => Webhook(value),
        _ => null
    };

    private static Dictionary<string, object> Event(EventAuthorizationFacts value)
    {
        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = value.TenantId.ToString("D"),
            ["eventId"] = value.EventId.ToString("D"),
            ["actorId"] = value.ActorId.ToString("D")
        };
        Add(attributes, "userId", value.UserId);
        Add(attributes, "organizationId", value.OrganizationId);
        Add(attributes, "groupId", value.GroupId);
        Add(attributes, "organizerActorId", value.OrganizerActorId);
        Add(attributes, "organizerUserId", value.OrganizerUserId);
        Add(attributes, "organizerOrganizationId", value.OrganizerOrganizationId);
        Add(attributes, "organizerGroupId", value.OrganizerGroupId);
        Add(attributes, "provenanceType", value.ProvenanceType);
        Add(attributes, "submittedByUserId", value.SubmittedByUserId);
        return attributes;
    }

    private static Dictionary<string, object> EventSession(EventSessionAuthorizationFacts value)
    {
        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = value.TenantId.ToString("D"),
            ["eventId"] = value.EventId.ToString("D")
        };
        Add(attributes, "eventSessionId", value.EventSessionId);
        Add(attributes, "authorizationPhase", value.AuthorizationPhase);
        return attributes;
    }

    private static Dictionary<string, object> EventOrganizerClaim(EventOrganizerClaimAuthorizationFacts value)
    {
        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = value.TenantId.ToString("D"),
            ["eventId"] = value.EventId.ToString("D"),
            ["claimId"] = value.ClaimId.ToString("D"),
            ["claimantActorId"] = value.ClaimantActorId.ToString("D"),
            ["status"] = value.Status
        };
        Add(attributes, "claimantUserId", value.ClaimantUserId);
        Add(attributes, "claimantOrganizationId", value.ClaimantOrganizationId);
        Add(attributes, "claimantGroupId", value.ClaimantGroupId);
        return attributes;
    }

    private static Dictionary<string, object> OrganizationMember(OrganizationMemberAuthorizationFacts value)
    {
        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = value.TenantId.ToString("D"),
            ["organizationId"] = value.OrganizationId.ToString("D")
        };
        Add(attributes, "memberId", value.MemberId);
        Add(attributes, "userId", value.UserId);
        return attributes;
    }

    private static Dictionary<string, object> ContactShare(ContactShareAuthorizationFacts value) => new()
    {
        ["tenantId"] = value.TenantId.ToString("D"),
        ["organizationId"] = value.OrganizationId.ToString("D")
    };

    private static Dictionary<string, object> SupportAccessSession(SupportAccessSessionAuthorizationFacts value)
    {
        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = value.TenantId.ToString("D")
        };
        Add(attributes, "sessionId", value.SessionId);
        Add(attributes, "actorUserId", value.ActorUserId);
        Add(attributes, "mode", value.Mode);
        Add(attributes, "status", value.Status);
        return attributes;
    }

    private static Dictionary<string, object> StorageObject(PersistedStorageObjectAuthorizationFacts value)
    {
        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = value.TenantId.ToString("D"),
            ["storageObjectId"] = value.StorageObjectId.ToString("D"),
            ["visibility"] = value.Visibility,
            ["lifecycleState"] = value.LifecycleState
        };
        Add(attributes, "createdBy", value.CreatedBy);
        Add(attributes, "owningResourceKind", value.OwningResourceKind);
        Add(attributes, "owningResourceId", value.OwningResourceId);
        return attributes;
    }

    private static Dictionary<string, object> Webhook(WebhookOwnershipAuthorizationFacts value)
    {
        var attributes = new Dictionary<string, object>
        {
            ["ownerKindId"] = (int)value.Kind,
            ["ownerKind"] = value.Kind.ToString().ToUpperInvariant(),
            ["ownerId"] = value.OwnerId.ToString("D")
        };
        Add(attributes, "tenantId", value.TenantId);
        Add(attributes, "instanceId", value.InstanceId);
        Add(attributes, "organizationId", value.OrganizationId);
        Add(attributes, "groupId", value.GroupId);
        Add(attributes, "userId", value.UserId);
        return attributes;
    }

    private static void Add(Dictionary<string, object> attributes, string key, Guid? value)
    {
        if (value is { } guid && guid != Guid.Empty)
            attributes[key] = guid.ToString("D");
    }

    private static void Add(Dictionary<string, object> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            attributes[key] = value;
    }

}
