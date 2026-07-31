// ABOUTME: Orchestrates generated registration-order client calls for Studio and recovery pages.
// ABOUTME: Reuses authorized managed-event reads and never logs or persists guest bearer capabilities.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Services;

public sealed class RegistrationOrderService(
    IEventApiClient apiClient,
    IEventService eventService,
    Explore.Blazor.Client.Services.Shell.UiShellState shellState,
    IGuestRegistrationOrderCapabilityStore capabilityStore,
    ILogger<RegistrationOrderService> logger) : IRegistrationOrderService
{
    public Task<RegistrationCheckoutCompositionDto?> GetCheckoutAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.GetRegistrationCheckoutCompositionAsync(eventId, cancellationToken: cancellationToken));

    public async Task<GuestRegistrationOrderStartDto?> StartGuestAsync(Guid eventId, StartRegistrationOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var started = await apiClient.StartGuestRegistrationOrderWithCapabilityAsync(eventId, request, cancellationToken);
            if (started.Response.Id is { } orderId)
            {
                capabilityStore.Store(eventId, orderId, new GuestRegistrationOrderCapability(started.Capability));
            }

            return started.Response;
        }
        catch (ApiException exception)
        {
            logger.LogWarning("Guest registration order could not be started. Status: {StatusCode}.", exception.StatusCode);
            return null;
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning("Guest registration order capability was unavailable.");
            return null;
        }
    }

    public Task<BaseCommandResponseOfGuid?> StartAuthenticatedAsync(Guid eventId, StartRegistrationOrderRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.StartAuthenticatedRegistrationOrderAsync(eventId, body: request, cancellationToken: cancellationToken));

    public async Task<IReadOnlyList<HalResourceOfRegistrationOrderDto>> GetActorOrdersAsync(CancellationToken cancellationToken = default)
    {
        var events = shellState.ActiveActorId is { } actorId
            ? await eventService.GetManagedEventsByActorAsync(actorId, cancellationToken: cancellationToken)
            : await eventService.GetMyEventsPagedAsync(1, 100, cancellationToken);

        var authorizedEventIds = events.Items
            .Where(item => item.Id is { } id && item.HasHalLink("view-registration-orders"))
            .Select(item => item.Id!.Value)
            .ToArray();

        var collections = await Task.WhenAll(authorizedEventIds.Select(eventId => GetEventOrdersAsync(eventId, cancellationToken)));
        return collections.SelectMany(collection => collection).ToArray();
    }

    public async Task<IReadOnlyList<HalResourceOfRegistrationOrderDto>> GetEventOrdersAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var resource = await apiClient.GetEventRegistrationOrdersAsync(eventId, cancellationToken: cancellationToken);
            return resource._embedded?.Items?.ToArray() ?? [];
        }
        catch (ApiException exception)
        {
            logger.LogWarning("Registration orders were unavailable for event {EventId}. Status: {StatusCode}.", eventId, exception.StatusCode);
            return [];
        }
    }

    public Task<HalResourceOfRegistrationOrderDto?> GetCurrentAsync(Guid eventId, Guid orderId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.GetCurrentRegistrationOrderAsync(eventId, orderId, cancellationToken: cancellationToken));

    public Task<HalResourceOfRegistrationOrderDto?> CancelCurrentAsync(Guid eventId, Guid orderId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.CancelAuthenticatedRegistrationOrderAsync(eventId, orderId, cancellationToken: cancellationToken));

    public Task<HalResourceOfRegistrationOrderDto?> ContinueCurrentAsync(
        Guid eventId,
        Guid orderId,
        int? contributionBasisPoints,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.ContinueAuthenticatedRegistrationOrderAsync(
            eventId,
            orderId,
            body: new ContinueRegistrationOrderRequest { PlatformContributionBasisPoints = contributionBasisPoints },
            cancellationToken: cancellationToken));

    public Task<HalResourceOfRegistrationOrderDto?> FinalizeCurrentAsync(
        Guid eventId,
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.FinalizeAuthenticatedRegistrationOrderAsync(
            eventId,
            orderId,
            cancellationToken: cancellationToken));

    public Task<HalResourceOfGuestRegistrationOrderDto?> GetGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.GetGuestRegistrationOrderAsync(eventId, orderId, capability.Value, cancellationToken: cancellationToken));

    public Task<GuestRegistrationOrderLifecycleResponseDto?> CancelGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.CancelGuestRegistrationOrderAsync(eventId, orderId, capability.Value, cancellationToken: cancellationToken));

    public Task<GuestRegistrationOrderLifecycleResponseDto?> ContinueGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, int? contributionBasisPoints, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.ContinueGuestRegistrationOrderAsync(
            eventId,
            orderId,
            capability.Value,
            body: new ContinueRegistrationOrderRequest { PlatformContributionBasisPoints = contributionBasisPoints },
            cancellationToken: cancellationToken));

    public Task<GuestRegistrationOrderLifecycleResponseDto?> FinalizeGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.FinalizeGuestRegistrationOrderAsync(eventId, orderId, capability.Value, cancellationToken: cancellationToken));

    private async Task<T?> ExecuteAsync<T>(Func<Task<T>> execute)
        where T : class
    {
        try
        {
            return await execute();
        }
        catch (ApiException exception)
        {
            logger.LogWarning("Registration order request was unavailable. Status: {StatusCode}.", exception.StatusCode);
            return null;
        }
    }
}
