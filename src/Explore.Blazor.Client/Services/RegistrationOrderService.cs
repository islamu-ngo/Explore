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

    public async Task<HalResourceOfRegistrationOrderDto?> ApplyCurrentPromotionAsync(
        Guid eventId,
        Guid orderId,
        HalResourceOfRegistrationOrderDto order,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (order._links?.ContainsKey("apply-promotion") != true || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return await ExecuteAsync(() => apiClient.ApplyAuthenticatedRegistrationOrderPromotionAsync(
            eventId,
            orderId,
            new PromotionCodeRequest { Code = code.Trim() },
            idempotency_Key: NewIdempotencyKey(),
            cancellationToken: cancellationToken)) is null
            ? null
            : await GetCurrentAsync(eventId, orderId, cancellationToken);
    }

    public async Task<HalResourceOfRegistrationOrderDto?> RemoveCurrentPromotionAsync(
        Guid eventId,
        Guid orderId,
        HalResourceOfRegistrationOrderDto order,
        CancellationToken cancellationToken = default)
    {
        if (order._links?.ContainsKey("remove-promotion") != true)
        {
            return null;
        }

        return await ExecuteAsync(() => apiClient.RemoveAuthenticatedRegistrationOrderPromotionAsync(
            eventId,
            orderId,
            idempotency_Key: NewIdempotencyKey(),
            cancellationToken: cancellationToken)) is null
            ? null
            : await GetCurrentAsync(eventId, orderId, cancellationToken);
    }

    public Task<HalResourceOfRegistrationOrderParticipantsDto?> GetCurrentParticipantsAsync(
        Guid eventId,
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.GetAuthenticatedRegistrationOrderParticipantsAsync(
            eventId, orderId, cancellationToken: cancellationToken));

    public async Task<HalResourceOfRegistrationOrderParticipantsDto?> SaveCurrentParticipantAsync(
        Guid eventId,
        Guid orderId,
        Guid? participantId,
        Guid lineId,
        int ordinal,
        RegistrationParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        BaseCommandResponseOfGuid? response = participantId is { } existingId
            ? await ExecuteAsync(() => apiClient.UpdateAuthenticatedRegistrationOrderParticipantAsync(
                eventId, orderId, existingId, request, cancellationToken: cancellationToken))
            : await ExecuteAsync(() => apiClient.AddAuthenticatedRegistrationOrderParticipantAsync(
                eventId, orderId, request, cancellationToken: cancellationToken));
        Guid? savedId = participantId ?? response?.Id;
        if (response?.Success != true || savedId is null)
        {
            return null;
        }

        if (participantId is null)
        {
            var assignmentRequest = new RegistrationTicketAssignmentsRequest
            {
                Assignments = [new TicketParticipantAssignmentInputDto
                {
                    RegistrationOrderLineId = lineId,
                    Ordinal = ordinal,
                    ParticipantId = savedId.Value
                }]
            };
            BaseCommandResponseOfGuid? assignment = await ExecuteAsync(() =>
                apiClient.AssignAuthenticatedRegistrationOrderTicketsAsync(
                    eventId, orderId, assignmentRequest, cancellationToken: cancellationToken));
            if (assignment?.Success != true)
            {
                return null;
            }
        }

        return await GetCurrentParticipantsAsync(eventId, orderId, cancellationToken);
    }

    public async Task<HalResourceOfRegistrationOrderParticipantsDto?> DeferCurrentParticipantsAsync(
        Guid eventId,
        Guid orderId,
        IReadOnlyCollection<TicketDeferralInputDto> assignments,
        DateTimeOffset deadline,
        CancellationToken cancellationToken = default)
    {
        BaseCommandResponseOfGuid? response = await ExecuteAsync(() =>
            apiClient.DeferAuthenticatedRegistrationOrderTicketsAsync(
                eventId,
                orderId,
                new RegistrationTicketDeferralsRequest { Assignments = assignments.ToArray(), AssignmentDeadline = deadline },
                cancellationToken: cancellationToken));
        return response?.Success == true
            ? await GetCurrentParticipantsAsync(eventId, orderId, cancellationToken)
            : null;
    }

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

    public Task<HalResourceOfRegistrationOrderParticipantsDto?> GetGuestParticipantsAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.GetGuestRegistrationOrderParticipantsAsync(
            eventId, orderId, capability.Value, cancellationToken: cancellationToken));

    public async Task<HalResourceOfRegistrationOrderParticipantsDto?> SaveGuestParticipantAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        Guid? participantId,
        Guid lineId,
        int ordinal,
        RegistrationParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        BaseCommandResponseOfGuid? response = participantId is { } existingId
            ? await ExecuteAsync(() => apiClient.UpdateGuestRegistrationOrderParticipantAsync(
                eventId, orderId, existingId, request, capability.Value, cancellationToken: cancellationToken))
            : await ExecuteAsync(() => apiClient.AddGuestRegistrationOrderParticipantAsync(
                eventId, orderId, request, capability.Value, cancellationToken: cancellationToken));
        Guid? savedId = participantId ?? response?.Id;
        if (response?.Success != true || savedId is null)
        {
            return null;
        }

        if (participantId is null)
        {
            BaseCommandResponseOfGuid? assignment = await ExecuteAsync(() =>
                apiClient.AssignGuestRegistrationOrderTicketsAsync(
                    eventId,
                    orderId,
                    new RegistrationTicketAssignmentsRequest
                    {
                        Assignments = [new TicketParticipantAssignmentInputDto
                        {
                            RegistrationOrderLineId = lineId,
                            Ordinal = ordinal,
                            ParticipantId = savedId.Value
                        }]
                    },
                    capability.Value,
                    cancellationToken: cancellationToken));
            if (assignment?.Success != true)
            {
                return null;
            }
        }

        return await GetGuestParticipantsAsync(eventId, orderId, capability, cancellationToken);
    }

    public async Task<HalResourceOfRegistrationOrderParticipantsDto?> DeferGuestParticipantsAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        IReadOnlyCollection<TicketDeferralInputDto> assignments,
        DateTimeOffset deadline,
        CancellationToken cancellationToken = default)
    {
        BaseCommandResponseOfGuid? response = await ExecuteAsync(() =>
            apiClient.DeferGuestRegistrationOrderTicketsAsync(
                eventId,
                orderId,
                new RegistrationTicketDeferralsRequest { Assignments = assignments.ToArray(), AssignmentDeadline = deadline },
                capability.Value,
                cancellationToken: cancellationToken));
        return response?.Success == true
            ? await GetGuestParticipantsAsync(eventId, orderId, capability, cancellationToken)
            : null;
    }

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

    public async Task<HalResourceOfGuestRegistrationOrderDto?> ApplyGuestPromotionAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        HalResourceOfGuestRegistrationOrderDto order,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (order._links?.ContainsKey("apply-promotion") != true || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return await ExecuteAsync(() => apiClient.ApplyGuestRegistrationOrderPromotionAsync(
            eventId,
            orderId,
            new PromotionCodeRequest { Code = code.Trim() },
            capability.Value,
            NewIdempotencyKey(),
            cancellationToken: cancellationToken)) is null
            ? null
            : await GetGuestAsync(eventId, orderId, capability, cancellationToken);
    }

    public async Task<HalResourceOfGuestRegistrationOrderDto?> RemoveGuestPromotionAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        HalResourceOfGuestRegistrationOrderDto order,
        CancellationToken cancellationToken = default)
    {
        if (order._links?.ContainsKey("remove-promotion") != true)
        {
            return null;
        }

        return await ExecuteAsync(() => apiClient.RemoveGuestRegistrationOrderPromotionAsync(
            eventId,
            orderId,
            capability.Value,
            NewIdempotencyKey(),
            cancellationToken: cancellationToken)) is null
            ? null
            : await GetGuestAsync(eventId, orderId, capability, cancellationToken);
    }

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

    private static string NewIdempotencyKey() => Guid.CreateVersion7().ToString("D");
}
