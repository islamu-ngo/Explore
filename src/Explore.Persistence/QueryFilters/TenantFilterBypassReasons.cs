// ABOUTME: Centralizes approved tenant-filter bypass reason strings for persistence queries.
// ABOUTME: Keeps system, admin, authentication, and worker cross-tenant reads auditable in code review.

namespace Explore.Persistence.QueryFilters;

public static class TenantFilterBypassReasons
{
    public const string TenantScopedRepositoryExactTenantPredicate =
        "Repository bypasses the ambient tenant filter only after applying an explicit tenant predicate.";

    public const string UserTenantMembershipEnumeration =
        "Repository enumerates all tenant memberships for one global user using an explicit user predicate.";

    public const string UserExternalLoginAuthentication =
        "External-login authentication must locate a provider subject before tenant binding.";

    public const string ExternalApiKeyAuthentication =
        "API-key authentication must locate a key by globally unique key id before tenant binding.";

    public const string ExternalApiKeyPlatformManagement =
        "Platform API-key management uses privileged owner/id predicates across tenant and instance keys.";

    public const string ExternalApiKeyPlatformUsageReport =
        "Platform usage reporting intentionally aggregates API-key quota rows across tenants.";

    public const string InstanceStorageAdministration =
        "Instance storage administration intentionally reports and reconciles storage usage across all tenants.";

    public const string ControlPlaneModerationReportingOperations =
        "Control-plane moderation reporting operations intentionally aggregate provider sync state across tenants without reading provider payloads.";

    public const string TenantCapabilityResolution =
        "Tenant capability resolution reads module flags by explicit tenant id before ambient context is guaranteed.";

    public const string EventAuthorizationTargetResolution =
        "MCP and AI proposal authorization resolve the target event tenant by explicit event id before provider authorization.";

    public const string TenantLookupCacheWarmup =
        "Tenant lookup cache warmup reads active tenants and domain settings before a request tenant exists.";

    public const string ManagedTenantDomainUniqueness =
        "Managed tenant provisioning checks one normalized domain host across all tenant domain settings.";

    public const string LegacyTenantResolutionLookup =
        "Legacy tenant resolver performs pre-tenant host lookup using explicit setting predicates.";

    public const string DatabaseSeeding =
        "Database seeding performs system-scoped idempotency checks before any request tenant exists.";

    public const string EmailDispatchWorkerCrossTenantQueue =
        "Email dispatch worker polls and updates durable outbox rows across tenants using explicit id/status predicates.";

    public const string IntegrationSyncWorkerCrossTenantQueue =
        "Integration sync worker polls and updates durable integration outbox rows across tenants using explicit id/status predicates.";

    public const string EmailDispatchTenantOperation =
        "Email dispatch tenant operation bypasses ambient context only after applying an explicit tenant predicate.";

    public const string WebhookWorkerCrossTenantQueue =
        "Webhook delivery worker polls and updates durable webhook rows across tenants using explicit id/status predicates.";

    public const string WebhookTenantOperation =
        "Webhook tenant operation bypasses ambient context only after applying an explicit tenant predicate.";

    public const string WebhookOwnerOperation =
        "Webhook owner operation bypasses ambient tenant filtering only with an exact typed owner and resource predicate.";

    public const string IncomingWebhookProviderAuthorityResolution =
        "Incoming webhook verification resolves one globally unique normalized provider application identity before tenant binding.";

    public const string NotificationFanoutWorkerCrossTenantQueue =
        "Notification fanout worker polls durable fanout runs across tenants using explicit status and batch-size predicates.";

    public const string WebPushWorkerCrossTenantQueue =
        "Web Push worker polls and updates durable dispatch rows across tenants using explicit id/status predicates.";

    public const string WebPushTenantOperation =
        "Web Push tenant operation bypasses ambient context only after applying an explicit tenant predicate.";

    public const string WebPushSubscriptionEndpointOwnership =
        "Web Push subscription endpoint ownership lookup uses the globally unique endpoint as a bounded predicate.";
}
