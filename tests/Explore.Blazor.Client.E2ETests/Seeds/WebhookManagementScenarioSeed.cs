// ABOUTME: API-driven webhook management setup for Playwright E2E coverage.
// ABOUTME: Creates consumers and endpoints, then triggers a real failed delivery through generated contracts.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class WebhookManagementScenarioSeed
{
    private const int SystemIntegrationConsumerKind = 5;
    private const int LocalProviderMode = 2;
    private const int DryRunProviderMode = 5;
    private const string OperationalSecretRef = "webhooks.svix.operational_webhook_secret";

    public sealed record Result(
        Guid TenantId,
        Guid AdminUserId,
        Guid LocalConsumerId,
        Guid DryRunEndpointId,
        Guid ExistingEndpointId,
        Guid FailedAttemptId,
        string DryRunEndpointUrl,
        string ExistingEndpointUrl);

    public static async Task<Result> SeedAsync(IEventApiClient api)
    {
        var user = await api.GetCurrentUserAsync();
        var tenant = (await api.GetTenantsAsync()).Single(candidate => candidate.IsActive == true);
        var eventType = (await api.GetWebhookEventTypesAsync())
            .First(candidate => string.Equals(candidate.Name, "webhook.test", StringComparison.OrdinalIgnoreCase));
        var eventTypeId = Required(eventType.Id, "webhook event type id");

        var localConsumerId = EventApiScenario.SuccessfulId(
            await api.CreateWebhookConsumerAsync(new CreateWebhookConsumerRequestDto
            {
                ConsumerKindId = SystemIntegrationConsumerKind,
                ProviderModeId = LocalProviderMode,
                Name = "Operations local bridge"
            }),
            "creating the local webhook consumer");
        var dryRunConsumerId = EventApiScenario.SuccessfulId(
            await api.CreateWebhookConsumerAsync(new CreateWebhookConsumerRequestDto
            {
                ConsumerKindId = SystemIntegrationConsumerKind,
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
        var dryRunEndpointUrl = "https://hooks.example.test/dryrun-no-outbound";
        var dryRunEndpointId = await CreateEndpointAsync(
            api,
            dryRunConsumerId,
            dryRunEndpointUrl,
            "DryRun endpoint",
            eventTypeId);

        EventApiScenario.EnsureSuccess(
            await api.TestWebhookEndpointAsync(existingEndpointId),
            "triggering a real failed webhook delivery");
        var failedAttempt = await WaitForFailedAttemptAsync(api, existingEndpointId);

        return new Result(
            Required(tenant.Id, "tenant id"),
            Required(user.Id, "administrator user id"),
            localConsumerId,
            dryRunEndpointId,
            existingEndpointId,
            Required(failedAttempt.Id, "failed delivery attempt id"),
            dryRunEndpointUrl,
            existingEndpointUrl);
    }

    private static async Task<Guid> CreateEndpointAsync(
        IEventApiClient api,
        Guid consumerId,
        string url,
        string description,
        Guid eventTypeId) =>
        EventApiScenario.SuccessfulId(
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

    private static async Task<HalResourceOfWebhookDeliveryAttemptDto> WaitForFailedAttemptAsync(
        IEventApiClient api,
        Guid endpointId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var attempts = await api.GetWebhookDeliveryAttemptsAsync(endpointId: endpointId, limit: 20);
            var failed = attempts._embedded?.Items?.FirstOrDefault(candidate =>
                string.Equals(candidate.OutcomeName, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.OutcomeName, "Unknown", StringComparison.OrdinalIgnoreCase));
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
}
