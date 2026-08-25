// ABOUTME: Orchestrates generated registration-order client calls for Studio and recovery pages.
// ABOUTME: Reuses authorized managed-event reads and never logs or persists guest bearer capabilities.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services.Http;

namespace Explore.Blazor.Client.Services;

public sealed class RegistrationOrderService(
    IEventApiClient apiClient,
    IEventService eventService,
    Explore.Blazor.Client.Services.Shell.UiShellState shellState,
    IGuestRegistrationOrderCapabilityStore capabilityStore,
    IBffClient bffClient,
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
            idempotency_Key: NewIdempotencyKey(),
            body: new PromotionCodeRequest { Code = code.Trim() },
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

    public Task<PaidOrderAcceptanceDisclosureDto?> GetCurrentPaymentAcceptanceAsync(
        Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, CancellationToken cancellationToken = default) =>
        HasLink(order._links, "payment-acceptance")
            ? ExecuteAsync(() => apiClient.GetAuthenticatedPaidOrderAcceptanceAsync(eventId, orderId, cancellationToken: cancellationToken))
            : Task.FromResult<PaidOrderAcceptanceDisclosureDto?>(null);

    public Task<HalResourceOfRegistrationPaymentDto?> StartCurrentPaymentAsync(
        Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, string disclosureRevision, CancellationToken cancellationToken = default) =>
        HasLink(order._links, "start-payment") && !string.IsNullOrWhiteSpace(disclosureRevision)
            ? ExecuteAsync(() => apiClient.StartAuthenticatedRegistrationPaymentAsync(
                eventId, orderId, NewIdempotencyKey(), body: new PaidOrderAcceptanceAcknowledgementDto
                {
                    DisclosureRevision = disclosureRevision,
                    Acknowledged = true
                }, cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public Task<HalResourceOfRegistrationPaymentDto?> GetCurrentPaymentAsync(
        Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, CancellationToken cancellationToken = default) =>
        HasLink(order._links, "payment-status")
            ? ExecuteAsync(() => apiClient.GetAuthenticatedRegistrationPaymentAsync(eventId, orderId, cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public Task<HalResourceOfRegistrationPaymentDto?> RefreshCurrentPaymentAsync(
        Guid eventId, Guid orderId, HalResourceOfRegistrationPaymentDto payment, CancellationToken cancellationToken = default) =>
        HasLink(payment._links, "payment-status")
            ? ExecuteAsync(() => apiClient.GetAuthenticatedRegistrationPaymentAsync(eventId, orderId, cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public Task<HalResourceOfRegistrationPaymentDto?> RetryCurrentPaymentAsync(
        Guid eventId, Guid orderId, HalResourceOfRegistrationPaymentDto payment, CancellationToken cancellationToken = default) =>
        HasLink(payment._links, "retry-payment")
            ? ExecuteAsync(() => apiClient.RetryAuthenticatedRegistrationPaymentAsync(
                eventId, orderId, NewIdempotencyKey(), cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public async Task<HalResourceOfRegistrationPaymentDto?> RequestCurrentRefundAsync(
        Guid eventId,
        Guid orderId,
        HalResourceOfRegistrationPaymentDto payment,
        CancellationToken cancellationToken = default)
    {
        if (!HasLink(payment._links, "request-refund"))
        {
            return null;
        }

        HalResourceOfRegistrationRefundDto? requested = await ExecuteAsync(() =>
            apiClient.RequestAuthenticatedRegistrationRefundAsync(
                eventId,
                orderId,
                NewIdempotencyKey(),
                new RegistrationRefundRequestDto { ReasonCode = "event_cancelled" },
                cancellationToken: cancellationToken));
        return requested is null
            ? null
            : await ExecuteAsync(() => apiClient.GetAuthenticatedRegistrationPaymentAsync(
                eventId, orderId, cancellationToken: cancellationToken));
    }

    public async Task<string?> IssueCurrentPaymentCheckoutTicketAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        (await bffClient.IssueRegistrationPaymentCheckoutTicketAsync(path, null, cancellationToken))?.CheckoutPath;

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
                eventId, orderId, existingId, NewIdempotencyKey(), request, cancellationToken: cancellationToken))
            : await ExecuteAsync(() => apiClient.AddAuthenticatedRegistrationOrderParticipantAsync(
                eventId, orderId, NewIdempotencyKey(), request, cancellationToken: cancellationToken));
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
                    eventId, orderId, NewIdempotencyKey(), assignmentRequest, cancellationToken: cancellationToken));
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
                NewIdempotencyKey(),
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
                eventId, orderId, existingId, NewIdempotencyKey(), request, capability.Value, cancellationToken: cancellationToken))
            : await ExecuteAsync(() => apiClient.AddGuestRegistrationOrderParticipantAsync(
                eventId, orderId, NewIdempotencyKey(), request, capability.Value, cancellationToken: cancellationToken));
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
                    NewIdempotencyKey(),
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
                NewIdempotencyKey(),
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
            NewIdempotencyKey(),
            new PromotionCodeRequest { Code = code.Trim() },
            capability.Value,
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

    public Task<PaidOrderAcceptanceDisclosureDto?> GetGuestPaymentAcceptanceAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        HalResourceOfGuestRegistrationOrderDto order,
        CancellationToken cancellationToken = default) =>
        HasLink(order._links, "payment-acceptance")
            ? ExecuteAsync(() => apiClient.GetGuestPaidOrderAcceptanceAsync(
                eventId, orderId, capability.Value, cancellationToken: cancellationToken))
            : Task.FromResult<PaidOrderAcceptanceDisclosureDto?>(null);

    public Task<HalResourceOfRegistrationPaymentDto?> StartGuestPaymentAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        HalResourceOfGuestRegistrationOrderDto order,
        string disclosureRevision,
        CancellationToken cancellationToken = default) =>
        HasLink(order._links, "start-payment") && !string.IsNullOrWhiteSpace(disclosureRevision)
            ? ExecuteAsync(() => apiClient.StartGuestRegistrationPaymentAsync(
                eventId, orderId, NewIdempotencyKey(), capability.Value, body: new PaidOrderAcceptanceAcknowledgementDto
                {
                    DisclosureRevision = disclosureRevision,
                    Acknowledged = true
                }, cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public Task<HalResourceOfRegistrationPaymentDto?> GetGuestPaymentAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        HalResourceOfGuestRegistrationOrderDto order,
        CancellationToken cancellationToken = default) =>
        HasLink(order._links, "payment-status")
            ? ExecuteAsync(() => apiClient.GetGuestRegistrationPaymentAsync(
                eventId, orderId, capability.Value, cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public Task<HalResourceOfRegistrationPaymentDto?> RefreshGuestPaymentAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        HalResourceOfRegistrationPaymentDto payment,
        CancellationToken cancellationToken = default) =>
        HasLink(payment._links, "payment-status")
            ? ExecuteAsync(() => apiClient.GetGuestRegistrationPaymentAsync(
                eventId, orderId, capability.Value, cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public Task<HalResourceOfRegistrationPaymentDto?> RetryGuestPaymentAsync(
        Guid eventId,
        Guid orderId,
        GuestRegistrationOrderCapability capability,
        HalResourceOfRegistrationPaymentDto payment,
        CancellationToken cancellationToken = default) =>
        HasLink(payment._links, "retry-payment")
            ? ExecuteAsync(() => apiClient.RetryGuestRegistrationPaymentAsync(
                eventId, orderId, NewIdempotencyKey(), capability.Value, cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public async Task<string?> IssueGuestPaymentCheckoutTicketAsync(
        string path,
        GuestRegistrationOrderCapability capability,
        CancellationToken cancellationToken = default) =>
        (await bffClient.IssueRegistrationPaymentCheckoutTicketAsync(path, capability.Value, cancellationToken))?.CheckoutPath;

    public Task<HalResourceOfRegistrationPaymentDto?> GetStudioPaymentAsync(
        Guid eventId,
        Guid orderId,
        HalResourceOfRegistrationOrderDto order,
        CancellationToken cancellationToken = default) =>
        HasLink(order._links, "studio-payment-status")
            ? ExecuteAsync(() => apiClient.GetStudioRegistrationPaymentAsync(eventId, orderId, cancellationToken: cancellationToken))
            : Task.FromResult<HalResourceOfRegistrationPaymentDto?>(null);

    public async Task<HalResourceOfRegistrationPaymentDto?> CreateStudioRefundAsync(
        Guid eventId,
        Guid orderId,
        HalResourceOfRegistrationPaymentDto payment,
        long? amountMinor,
        CancellationToken cancellationToken = default)
    {
        if (!HasLink(payment._links, "create-refund"))
        {
            return null;
        }

        HalResourceOfRegistrationRefundDto? created = await ExecuteAsync(() =>
            apiClient.CreateStudioRegistrationRefundAsync(
                eventId,
                orderId,
                NewIdempotencyKey(),
                new RegistrationRefundRequestDto { AmountMinor = amountMinor, ReasonCode = "organizer_refund" },
                cancellationToken: cancellationToken));
        return created is null
            ? null
            : await ExecuteAsync(() => apiClient.GetStudioRegistrationPaymentAsync(
                eventId, orderId, cancellationToken: cancellationToken));
    }

    public async Task<HalResourceOfRegistrationPaymentDto?> RetryStudioRefundAsync(
        Guid eventId,
        Guid orderId,
        HalResourceOfRegistrationPaymentDto payment,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetRefundAttemptId(payment._links, "retry-refund", out Guid refundAttemptId))
        {
            return null;
        }

        HalResourceOfRegistrationRefundDto? retried = await ExecuteAsync(() =>
            apiClient.RetryStudioRegistrationRefundAsync(
                eventId, orderId, refundAttemptId, NewIdempotencyKey(), cancellationToken: cancellationToken));
        return retried is null
            ? null
            : await ExecuteAsync(() => apiClient.GetStudioRegistrationPaymentAsync(
                eventId, orderId, cancellationToken: cancellationToken));
    }

    public async Task<HalResourceOfRegistrationPaymentDto?> RespondCurrentMaterialChangeAsync(
        Guid eventId,
        Guid orderId,
        HalResourceOfRegistrationPaymentDto payment,
        Guid campaignId,
        string choiceCode,
        CancellationToken cancellationToken = default)
    {
        if (!HasLink(payment._links, "respond-material-change") ||
            choiceCode is not ("accept_new_terms" or "request_refund"))
        {
            return null;
        }

        HalResourceOfRegistrationMaterialChangeChoiceDto? response = await ExecuteAsync(() =>
            apiClient.RespondAuthenticatedRegistrationMaterialChangeAsync(
                eventId,
                orderId,
                NewIdempotencyKey(),
                new RegistrationMaterialChangeChoiceRequestDto
                {
                    CampaignId = campaignId,
                    ChoiceCode = choiceCode
                },
                cancellationToken: cancellationToken));
        return response is null
            ? null
            : await ExecuteAsync(() => apiClient.GetAuthenticatedRegistrationPaymentAsync(
                eventId, orderId, cancellationToken: cancellationToken));
    }

    public Task<HalCollectionResourceOfRefundCampaignDto?> GetRefundCampaignsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => apiClient.GetRefundCampaignsAsync(eventId, cancellationToken: cancellationToken));

    public async Task<HalCollectionResourceOfRefundCampaignDto?> ResumeRefundCampaignAsync(
        Guid eventId,
        HalResourceOfRefundCampaignDto campaign,
        CancellationToken cancellationToken = default)
    {
        if (!HasLink(campaign._links, "resume-refund-campaign") || campaign.Id is not { } campaignId)
        {
            return null;
        }
        HalResourceOfRefundCampaignDto? resumed = await ExecuteAsync(() => apiClient.ResumeRefundCampaignAsync(
            eventId, campaignId, NewIdempotencyKey(), cancellationToken: cancellationToken));
        return resumed is null
            ? null
            : await GetRefundCampaignsAsync(eventId, cancellationToken);
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

    private static bool TryGetRefundAttemptId(
        IDictionary<string, HalLink>? links,
        string relation,
        out Guid refundAttemptId)
    {
        refundAttemptId = Guid.Empty;
        if (links is null || !links.TryGetValue(relation, out HalLink? link) ||
            !string.Equals(link.Method, HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(link.Href))
        {
            return false;
        }

        string[] segments = link.Href.Split(['?', '#'], 2)[0]
            .TrimEnd('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 &&
               string.Equals(segments[^1], "retry", StringComparison.Ordinal) &&
               Guid.TryParse(segments[^2], out refundAttemptId);
    }

    private static bool HasLink(IDictionary<string, HalLink>? links, string relation) =>
        links?.ContainsKey(relation) == true;
}
