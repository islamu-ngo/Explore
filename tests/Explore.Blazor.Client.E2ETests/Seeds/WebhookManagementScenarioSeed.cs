// ABOUTME: API-driven webhook management setup for Playwright E2E coverage.
// ABOUTME: Creates consumers and endpoints, then triggers a real failed delivery through generated contracts.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class WebhookManagementScenarioSeed
{
    public const int TenantOwnerKindId = 1;
    public const int OrganizationOwnerKindId = 2;
    public const int GroupOwnerKindId = 3;
    public const int UserOwnerKindId = 4;
    public const int InstanceOwnerKindId = 5;
    private const int LocalProviderMode = 2;
    private const int DryRunProviderMode = 5;
    private const string OperationalSecretRef = "webhooks.svix.operational_webhook_secret";

    public sealed record Result(
        Guid TenantId,
        Guid AdminUserId,
        Guid LocalConsumerId,
        Guid IdleEndpointId,
        Guid ExistingEndpointId,
        Guid FailedAttemptId,
        string IdleEndpointUrl,
        string ExistingEndpointUrl);

    public sealed record TypedOwnershipResult(
        Guid TenantId,
        string TenantSlug,
        Guid UserId,
        Guid OrganizationId,
        Guid GroupId,
        Guid UnrelatedOrganizationId,
        Guid UnrelatedGroupId,
        string TenantConsumerName,
        string OrganizationConsumerName,
        string GroupConsumerName,
        string UserConsumerName);

    public static async Task<Result> SeedAsync(IEventApiClient api)
    {
        var administratorUserId = EventApiScenario.SuccessfulId(
            await api.SyncUserAsync(),
            "synchronizing the instance administrator");
        var tenant = (await api.GetTenantsAsync()).Single(candidate => candidate.IsActive == true);
        var eventType = (await api.GetWebhookEventTypesAsync())
            .First(candidate => string.Equals(candidate.Name, "webhook.test", StringComparison.OrdinalIgnoreCase));
        var eventTypeId = Required(eventType.Id, "webhook event type id");

        var localConsumerId = EventApiScenario.SuccessfulId(
            await api.CreateWebhookConsumerAsync(new CreateWebhookConsumerRequestDto
            {
                ConsumerKindId = InstanceOwnerKindId,
                ProviderModeId = LocalProviderMode,
                Name = "Operations local bridge"
            }),
            "creating the local webhook consumer");
        _ = EventApiScenario.SuccessfulId(
            await api.CreateWebhookConsumerAsync(new CreateWebhookConsumerRequestDto
            {
                ConsumerKindId = InstanceOwnerKindId,
                ProviderModeId = DryRunProviderMode,
                Name = "DryRun verification bridge"
            }),
            "creating the dry-run webhook consumer");

        var existingEndpointUrl = "https://127.0.0.1:1/e2e-webhook-failure";
        var existingEndpointId = await CreateEndpointAsync(
            api,
            localConsumerId,
            existingEndpointUrl,
            "Real failure endpoint",
            eventTypeId);
        var idleEndpointUrl = "https://hooks.example.test/idle-no-outbound";
        var idleEndpointId = await CreateEndpointAsync(
            api,
            localConsumerId,
            idleEndpointUrl,
            "Idle local endpoint",
            eventTypeId);

        EventApiScenario.EnsureSuccess(
            await api.TestWebhookEndpointAsync(existingEndpointId),
            "triggering a real failed webhook delivery");
        var failedAttempt = await WaitForFailedAttemptAsync(api, existingEndpointId);

        return new Result(
            Required(tenant.Id, "tenant id"),
            administratorUserId,
            localConsumerId,
            idleEndpointId,
            existingEndpointId,
            Required(failedAttempt.Id, "failed delivery attempt id"),
            idleEndpointUrl,
            existingEndpointUrl);
    }

    public static async Task<TypedOwnershipResult> SeedTypedOwnershipAsync(
        IEventApiClient instanceAdministratorApi,
        Func<string, IEventApiClient> tenantAdministratorApiFactory,
        Func<string, IEventApiClient> userApiFactory,
        string userProviderSubject)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantSlug = $"webhook-owner-{suffix}";
        var provisioning = await instanceAdministratorApi.EnsureManagedProviderClientProvisionedAsync(
            new ManagedProviderClientProvisioningDto
            {
                ProviderKey = "webhook-e2e",
                ExternalSystem = "keycloak",
                ExternalCustomerId = $"webhook-owner-{suffix}",
                TenantFullName = $"Webhook Ownership Tenant {suffix}",
                TenantSlug = tenantSlug,
                ActivateTenant = true,
                ExternalAdmin = new ManagedProviderExternalAdminDto
                {
                    IdentityProvider = "keycloak",
                    Subject = userProviderSubject,
                    Email = "user@test.islamu.org",
                    FirstName = "Test",
                    LastName = "User",
                    DisplayName = "Test User",
                    EmailVerified = true
                }
            });
        if (provisioning.Success != true || provisioning.Id is null)
        {
            throw new InvalidOperationException(
                $"API failed while provisioning the typed webhook tenant: {provisioning.Message}");
        }

        var tenantId = Required(provisioning.Id.TenantId, "provisioned tenant id");
        var userId = Required(provisioning.Id.UserId, "provisioned scoped administrator user id");
        var administratorApi = tenantAdministratorApiFactory(tenantSlug);
        var userApi = userApiFactory(tenantSlug);

        var organizationId = await CreateOrganizationAsync(
            userApi,
            $"Webhook Owner Organization {suffix}",
            $"owner-{suffix}@example.test");
        var groupId = await CreateGroupAsync(
            userApi,
            $"Webhook Owner Group {suffix}",
            organizationId);
        var unrelatedOrganizationId = await CreateOrganizationAsync(
            administratorApi,
            $"Unrelated Webhook Organization {suffix}",
            $"unrelated-{suffix}@example.test");
        var unrelatedGroupId = await CreateGroupAsync(
            administratorApi,
            $"Unrelated Webhook Group {suffix}",
            unrelatedOrganizationId);

        var tenantConsumerName = $"Tenant delivery {suffix}";
        var organizationConsumerName = $"Organization delivery {suffix}";
        var groupConsumerName = $"Group delivery {suffix}";
        var userConsumerName = $"User delivery {suffix}";

        await CreateConsumerAsync(administratorApi, TenantOwnerKindId, null, tenantConsumerName);
        await CreateConsumerAsync(userApi, OrganizationOwnerKindId, organizationId, organizationConsumerName);
        await CreateConsumerAsync(userApi, GroupOwnerKindId, groupId, groupConsumerName);
        await CreateConsumerAsync(userApi, UserOwnerKindId, null, userConsumerName);
        await CreateConsumerAsync(
            administratorApi,
            OrganizationOwnerKindId,
            unrelatedOrganizationId,
            $"Unrelated organization delivery {suffix}");
        await CreateConsumerAsync(
            administratorApi,
            GroupOwnerKindId,
            unrelatedGroupId,
            $"Unrelated group delivery {suffix}");

        return new TypedOwnershipResult(
            tenantId,
            tenantSlug,
            userId,
            organizationId,
            groupId,
            unrelatedOrganizationId,
            unrelatedGroupId,
            tenantConsumerName,
            organizationConsumerName,
            groupConsumerName,
            userConsumerName);
    }

    public static async Task EnableMultiTenantRoutingAsync(IEventApiClient administratorApi)
    {
        var runbook = await administratorApi.GetControlPlaneDeploymentModeRunbookAsync();
        if (!string.Equals(runbook.CurrentMode, "MultiTenant", StringComparison.Ordinal))
        {
            var target = runbook.TargetOptions?.SingleOrDefault(option =>
                string.Equals(option.TargetMode, "MultiTenant", StringComparison.Ordinal));
            if (target is null)
            {
                throw new InvalidOperationException(
                    "The deployment-mode runbook did not expose the MultiTenant target.");
            }

            if (target.Allowed != true)
            {
                throw new InvalidOperationException(
                    "The deployment-mode runbook blocked the MultiTenant transition. " +
                    $"Reason={target.BlockingReason}. Remediation={target.Remediation}");
            }

            var transition = await administratorApi.TransitionControlPlaneDeploymentModeAsync(
                body: new ControlPlaneDeploymentModeTransitionRequestDto
                {
                    TargetMode = target.TargetMode,
                    Reason = "Verify typed webhook ownership across tenant routing boundaries.",
                    ConfirmationText = target.ConfirmationText
                });
            if (transition.Success != true
                || transition.Id is null
                || !string.Equals(transition.Id.NewMode, "MultiTenant", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "API failed while switching the webhook E2E instance to multi-tenant mode. " +
                    $"Message={transition.Message}. Errors={string.Join(" | ", transition.Errors ?? [])}");
            }
        }

        var resolverConfiguration = await administratorApi.GetInstanceResolverConfigurationAsync();
        resolverConfiguration.HeaderEnabled = true;
        resolverConfiguration.SubdomainEnabled ??= false;
        resolverConfiguration.CustomDomainEnabled ??= false;
        resolverConfiguration.PathEnabled = true;
        resolverConfiguration.PathPrefix = "/t";
        resolverConfiguration.InstanceBaseDomain ??= string.Empty;
        resolverConfiguration.AllowTenantCustomDomains ??= true;

        try
        {
            EnsureSuccess(
                await administratorApi.UpdateInstanceResolverConfigurationAsync(resolverConfiguration),
                "enabling webhook E2E tenant routing");
        }
        catch (ApiException<ValidationProblemDetails> exception)
        {
            var errors = exception.Result.Errors?
                .SelectMany(pair => pair.Value.Select(message => $"{pair.Key}: {message}"))
                .ToArray() ?? [];
            throw new InvalidOperationException(
                "API rejected the webhook E2E tenant resolver configuration. " +
                $"Title={exception.Result.Title}. Detail={exception.Result.Detail}. " +
                $"Errors={string.Join(" | ", errors)}",
                exception);
        }
    }

    private static async Task<Guid> CreateOrganizationAsync(
        IEventApiClient api,
        string name,
        string email) =>
        EventApiScenario.SuccessfulId(
            await api.CreateOrganizationAsync(new CreateOrganizationDto
            {
                FullName = name,
                Email = email,
                Country = "Belgium",
                City = "Brussels",
                Postcode = 1000,
                Address = "Webhook E2E Street 1"
            }),
            $"creating organization {name}");

    private static async Task<Guid> CreateGroupAsync(
        IEventApiClient api,
        string name,
        Guid organizationId) =>
        EventApiScenario.SuccessfulId(
            await api.CreateGroupAsync(new CreateGroupDto
            {
                FullName = name,
                Description = "Typed webhook ownership browser evidence.",
                ParentOrganizationId = organizationId
            }),
            $"creating group {name}");

    private static async Task<Guid> CreateConsumerAsync(
        IEventApiClient api,
        int ownerKindId,
        Guid? ownerId,
        string name) =>
        EventApiScenario.SuccessfulId(
            await api.CreateWebhookConsumerAsync(new CreateWebhookConsumerRequestDto
            {
                ConsumerKindId = ownerKindId,
                OwnerId = ownerId,
                ProviderModeId = LocalProviderMode,
                Name = name
            }),
            $"creating {name} webhook consumer");

    private static async Task<Guid> CreateEndpointAsync(
        IEventApiClient api,
        Guid consumerId,
        string url,
        string description,
        Guid eventTypeId)
    {
        try
        {
            return EventApiScenario.SuccessfulId(
                await api.CreateWebhookEndpointAsync(new CreateWebhookEndpointRequestDto
                {
                    ConsumerId = consumerId,
                    Url = url,
                    Description = description,
                    SecretRef = OperationalSecretRef,
                    EventTypeIds = [eventTypeId],
                    MaxAttempts = 3,
                    TimeoutSeconds = 2,
                    RateLimitPerMinute = 60
                }),
                $"creating webhook endpoint {url}");
        }
        catch (ApiException<ValidationProblemDetails> exception)
        {
            var errors = exception.Result.Errors?
                .SelectMany(pair => pair.Value.Select(message => $"{pair.Key}: {message}"))
                .ToArray() ?? [];
            throw new InvalidOperationException(
                $"API rejected webhook endpoint {url}. " +
                $"Title={exception.Result.Title}. Detail={exception.Result.Detail}. " +
                $"Errors={string.Join(" | ", errors)}",
                exception);
        }
    }

    private static async Task<HalResourceOfWebhookDeliveryAttemptDto> WaitForFailedAttemptAsync(
        IEventApiClient api,
        Guid endpointId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var attempts = await api.GetWebhookDeliveryAttemptsAsync(
                ownerKindId: InstanceOwnerKindId,
                endpointId: endpointId,
                limit: 20);
            var failed = attempts._embedded?.Items?.FirstOrDefault(candidate =>
                string.Equals(candidate.OutcomeName, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.OutcomeName, "Abandoned", StringComparison.OrdinalIgnoreCase));
            if (failed is not null)
            {
                return failed;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException("The API did not expose the real failed webhook delivery attempt.");
    }

    private static Guid Required(Guid? value, string name) =>
        value is { } result && result != Guid.Empty
            ? result
            : throw new InvalidOperationException($"The API did not return a valid {name}.");

    private static void EnsureSuccess(BaseCommandResponseOfGuid response, string operation)
    {
        if (response.Success != true)
        {
            throw new InvalidOperationException($"API failed while {operation}: {response.Message}");
        }
    }
}
