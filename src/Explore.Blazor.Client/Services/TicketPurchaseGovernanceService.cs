// ABOUTME: Submits HAL-gated purchase-governance requests through same-origin BFF endpoints.
// ABOUTME: Never accepts tenant, quantity, policy lineage, or browser-created idempotency authority.

using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Services.Http;

namespace Explore.Blazor.Client.Services;

public sealed class TicketPurchaseGovernanceService(
    IBffClient bff) : ITicketPurchaseGovernanceService
{
    public async Task<TicketPurchaseGovernanceSubmission>
        ReserveAsync(
            Guid eventId,
            Guid orderId,
            int accessMode,
            Guid? requestedPurchaserActorId,
            string? guestCapability,
            bool authenticated,
            CancellationToken cancellationToken)
    {
        string path = authenticated
            ? "/bff/ticket-purchases/authenticated"
            : "/bff/ticket-purchases/guest";
        var request = new TicketPurchaseBffRequest(
            eventId,
            orderId,
            accessMode,
            requestedPurchaserActorId,
            authenticated ? null : guestCapability);
        using HttpResponseMessage response =
            await bff.PostAsync(
                path,
                request,
                cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new TicketPurchaseGovernanceSubmission(
                false,
                false,
                "unavailable");
        }

        HalResourceOfTicketPurchaseGovernanceResource? resource =
            await response.Content.ReadFromJsonAsync<
                HalResourceOfTicketPurchaseGovernanceResource>(
                cancellationToken);
        return resource is null
            ? new TicketPurchaseGovernanceSubmission(
                false,
                false,
                "unavailable")
            : new TicketPurchaseGovernanceSubmission(
                true,
                resource.SupportsHardCrossOrderCeiling,
                resource.EnforcementScopeCode);
    }

    private sealed record TicketPurchaseBffRequest(
        Guid EventId,
        Guid OrderId,
        int AccessMode,
        Guid? RequestedPurchaserActorId,
        string? GuestCapability);
}
