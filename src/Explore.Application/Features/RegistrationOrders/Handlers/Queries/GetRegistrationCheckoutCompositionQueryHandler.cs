// ABOUTME: Builds public ticket-selection data from the current published catalog.
// ABOUTME: Fails closed for non-public, ineligible, or non-platform-managed events.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Application.Settings;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Handlers.Queries;

public sealed class GetRegistrationCheckoutCompositionQueryHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    IPlatformFeePolicyRepository feePolicies,
    IPaidEventPolicyRepository paidEventPolicies,
    ITypedSettingsDocumentResolver settingsDocumentResolver,
    IOrganizerEarningsCalculator earningsCalculator)
    : IRequestHandler<GetRegistrationCheckoutCompositionQuery, RegistrationCheckoutCompositionDto?>
{
    public async Task<RegistrationCheckoutCompositionDto?> Handle(
        GetRegistrationCheckoutCompositionQuery request,
        CancellationToken cancellationToken)
    {
        var @event = await events.GetById(request.EventId);
        if (@event is null
            || @event.EventStatusId != (int)EventStatusEnum.Published
            || @event.VisibilityTypeId != (int)VisibilityTypeEnum.Public
            || @event.ParticipationConfiguration?.ParticipationHandlingModeId != (int)ParticipationHandlingModeEnum.PlatformManaged
            || !await events.IsPubliclyEligibleAsync(@event.TenantId, @event.Id, cancellationToken))
        {
            return null;
        }

        var catalog = await catalogs.GetPublishedCatalogAsync(@event.Id, @event.TenantId, cancellationToken);
        if (catalog is null)
        {
            return null;
        }

        var ticketTypes = catalog.TicketTypes
            .Where(ticketType => !ticketType.IsDeleted)
            .ToArray();
        var feePolicy = await feePolicies.GetActiveAsync(cancellationToken);
        string? paidEventDirectoryDisclaimer = null;
        if (ticketTypes.Any(ticketType => ticketType.TicketPricingModeId != (int)TicketPricingModeEnum.Free))
        {
            var instancePolicy = await paidEventPolicies.GetActiveInstanceAsync(cancellationToken);
            var tenantPolicy = instancePolicy is null
                ? null
                : await paidEventPolicies.GetActiveTenantAsync(@event.TenantId, cancellationToken);
            if (instancePolicy is not null &&
                PaidEventPolicyRules.GetEffectiveCurrencyCodes(instancePolicy, tenantPolicy).Count > 0)
            {
                var branding = await settingsDocumentResolver.ResolveTenantDocumentAsync<BrandingSettings>(
                    new SettingsResolutionContext(
                        @event.TenantId,
                        RequestedDocuments: [SettingsDocumentKeys.Tenant.Branding]),
                    SettingsDocumentKeys.Tenant.Branding,
                    cancellationToken);
                paidEventDirectoryDisclaimer = PaidEventDisclaimerFormatter.Format(branding?.Payload.DisplayName);
            }
        }

        return new RegistrationCheckoutCompositionDto
        {
            EventId = @event.Id,
            TicketCatalogVersionId = catalog.Id,
            CurrencyCode = catalog.CurrencyCode,
            PaidEventDirectoryDisclaimer = paidEventDirectoryDisclaimer,
            TicketTypes = ticketTypes
                .Select(ticketType => new RegistrationCheckoutTicketTypeDto
                    {
                        Id = ticketType.Id,
                        Name = ticketType.Name,
                        TicketPricingModeId = ticketType.TicketPricingModeId,
                        TicketPricingModeCode = ticketType.TicketPricingMode?.MasterCode,
                        FixedPriceMinor = ticketType.FixedPriceMinor,
                        MinimumPriceMinor = ticketType.MinimumPriceMinor,
                        SuggestedPriceMinor = ticketType.SuggestedPriceMinor,
                        PerOrderLimit = ticketType.PerOrderLimit,
                        SlidingScaleOptions = ticketType.TicketPricingModeId == (int)TicketPricingModeEnum.SlidingScale
                            ? BuildSlidingScaleOptions(
                                catalog.CurrencyCode,
                                ticketType.MinimumPriceMinor.GetValueOrDefault(),
                                ticketType.SuggestedPriceMinor.GetValueOrDefault(ticketType.MinimumPriceMinor.GetValueOrDefault()),
                                feePolicy,
                                earningsCalculator)
                            : []
                    })
                .ToArray()
        };
    }

    private static IReadOnlyList<RegistrationCheckoutSlidingScaleOptionDto> BuildSlidingScaleOptions(
        string currencyCode,
        long minimumMinor,
        long suggestedMinor,
        Explore.Domain.PlatformFeePolicy? feePolicy,
        IOrganizerEarningsCalculator earningsCalculator)
    {
        var maximumMinor = Math.Max(minimumMinor, suggestedMinor);
        var pointCount = maximumMinor == minimumMinor ? 1 : 5;
        return Enumerable.Range(0, pointCount)
            .Select(index => pointCount == 1
                ? minimumMinor
                : minimumMinor + ((maximumMinor - minimumMinor) * index / (pointCount - 1)))
            .Distinct()
            .Select(priceMinor => new RegistrationCheckoutSlidingScaleOptionDto
            {
                BuyerPriceMinor = priceMinor,
                OrganizerEarningsMinor = earningsCalculator.Calculate(currencyCode, priceMinor, feePolicy).OrganizerEarningsMinor
            })
            .ToArray();
    }
}
