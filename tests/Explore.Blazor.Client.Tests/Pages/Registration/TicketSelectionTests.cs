// ABOUTME: bUnit coverage for the attendee ticket-selection entry page.
// ABOUTME: Verifies server composition drives guest order creation and recovery navigation.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Pages.Registration;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Registration;

public sealed class TicketSelectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IRegistrationOrderService _service;

    public TicketSelectionTests() => _service = _ctx.AddMockService<IRegistrationOrderService>();

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task PaidDirectoryDisclaimer_RendersServerAuthoredTenantBranding()
    {
        _ctx.SetAnonymousUser();
        Guid eventId = Guid.CreateVersion7();
        _service.GetCheckoutAsync(eventId, Arg.Any<CancellationToken>()).Returns(new RegistrationCheckoutCompositionDto
        {
            EventId = eventId,
            TicketCatalogVersionId = Guid.CreateVersion7(),
            CurrencyCode = "EUR",
            PaidEventDirectoryDisclaimer = "Tenant Events provides an event discovery and management directory only.",
            TicketTypes =
            [
                new RegistrationCheckoutTicketTypeDto
                {
                    Id = Guid.CreateVersion7(),
                    Name = "General admission",
                    TicketPricingModeCode = "FIXED",
                    FixedPriceMinor = 1200
                }
            ]
        });

        var cut = _ctx.RenderMudComponent<TicketSelection>(
            parameters => parameters.Add(component => component.EventId, eventId));

        var notice = cut.WaitForElement("[data-testid='ticket-selection-paid-event-directory-disclaimer']");

        await Assert.That(notice.TextContent)
            .Contains("Tenant Events provides an event discovery and management directory only.");
        await Assert.That(notice.GetAttribute("dir")).IsNull();
        await Assert.That(notice.QuerySelectorAll("[lang='en'][dir='ltr']").Length).IsEqualTo(2);
    }

    [Test]
    public async Task SubmitGuestSelection_UsesServerCatalogAndNavigatesToRecovery()
    {
        _ctx.SetAnonymousUser();
        var eventId = Guid.CreateVersion7();
        var catalogId = Guid.CreateVersion7();
        var ticketId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        _service.GetCheckoutAsync(eventId, Arg.Any<CancellationToken>()).Returns(new RegistrationCheckoutCompositionDto
        {
            EventId = eventId,
            TicketCatalogVersionId = catalogId,
            CurrencyCode = "EUR",
            TicketTypes = [new RegistrationCheckoutTicketTypeDto { Id = ticketId, Name = "General admission", TicketPricingModeCode = "FIXED", FixedPriceMinor = 1200 }]
        });
        _service.StartGuestAsync(eventId, Arg.Any<StartRegistrationOrderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GuestRegistrationOrderStartDto { Id = orderId, Success = true });

        var cut = _ctx.RenderMudComponent<TicketSelection>(parameters => parameters.Add(component => component.EventId, eventId));
        cut.WaitForElement("input").Change("1");
        await cut.FindAll("button").Single(button => button.TextContent.Contains("Reserve selected tickets", StringComparison.Ordinal)).ClickAsync(new());

        cut.WaitForAssertion(() => Assert.That(_ctx.Services.GetRequiredService<NavigationManager>().Uri)
            .EndsWith($"/registration/guest/events/{eventId}/orders/{orderId}"));
        await _service.Received(1).StartGuestAsync(
            eventId,
            Arg.Is<StartRegistrationOrderRequest>(request =>
                request.TicketCatalogVersionId == catalogId
                && request.Lines!.Single().TicketTypeId == ticketId
                && request.Lines.Single().Quantity == 1),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SlidingScaleSelection_SubmitsBuyerPriceFromLinkedServerOption()
    {
        _ctx.SetAnonymousUser();
        var eventId = Guid.CreateVersion7();
        var ticketId = Guid.CreateVersion7();
        _service.GetCheckoutAsync(eventId, Arg.Any<CancellationToken>()).Returns(new RegistrationCheckoutCompositionDto
        {
            EventId = eventId,
            TicketCatalogVersionId = Guid.CreateVersion7(),
            CurrencyCode = "EUR",
            TicketTypes = [new RegistrationCheckoutTicketTypeDto
            {
                Id = ticketId,
                Name = "Community rate",
                TicketPricingModeCode = "SLIDING_SCALE",
                SlidingScaleOptions =
                [
                    new RegistrationCheckoutSlidingScaleOptionDto { BuyerPriceMinor = 500, OrganizerEarningsMinor = 450 },
                    new RegistrationCheckoutSlidingScaleOptionDto { BuyerPriceMinor = 1000, OrganizerEarningsMinor = 900 }
                ]
            }]
        });
        _service.StartGuestAsync(eventId, Arg.Any<StartRegistrationOrderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GuestRegistrationOrderStartDto { Id = Guid.CreateVersion7(), Success = true });

        var cut = _ctx.RenderMudComponent<TicketSelection>(parameters => parameters.Add(component => component.EventId, eventId));
        cut.WaitForElement("input");
        cut.FindAll("input").Single(input => input.GetAttribute("type") != "range").Change("1");
        cut.Find("input[type='range']").Input("0");
        await cut.FindAll("button").Single(button => button.TextContent.Contains("Reserve selected tickets", StringComparison.Ordinal)).ClickAsync(new());

        await _service.Received(1).StartGuestAsync(
            eventId,
            Arg.Is<StartRegistrationOrderRequest>(request => request.Lines!.Single().ChosenUnitPriceMinor == 500),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SlidingScaleSelection_RendersBothServerAuthoredValues()
    {
        _ctx.SetAnonymousUser();
        var eventId = Guid.CreateVersion7();
        var ticketId = Guid.CreateVersion7();
        _service.GetCheckoutAsync(eventId, Arg.Any<CancellationToken>()).Returns(new RegistrationCheckoutCompositionDto
        {
            EventId = eventId,
            TicketCatalogVersionId = Guid.CreateVersion7(),
            CurrencyCode = "EUR",
            TicketTypes =
            [
                new RegistrationCheckoutTicketTypeDto
                {
                    Id = ticketId,
                    Name = "Community rate",
                    TicketPricingModeCode = "SLIDING_SCALE",
                    SlidingScaleOptions =
                    [
                        new RegistrationCheckoutSlidingScaleOptionDto { BuyerPriceMinor = 500, OrganizerEarningsMinor = 450 }
                    ]
                }
            ]
        });

        var cut = _ctx.RenderMudComponent<TicketSelection>(parameters => parameters.Add(component => component.EventId, eventId));

        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("You pay"));
        await Assert.That(cut.Markup).Contains("500 EUR minor units");
        await Assert.That(cut.Markup).Contains("Organizer earns");
        await Assert.That(cut.Markup).Contains("450 EUR minor units");
    }

    [Test]
    public async Task NullPerOrderLimit_AllowsQuantitiesAboveNinetyNine()
    {
        _ctx.SetAnonymousUser();
        var eventId = Guid.CreateVersion7();
        _service.GetCheckoutAsync(eventId, Arg.Any<CancellationToken>()).Returns(new RegistrationCheckoutCompositionDto
        {
            EventId = eventId,
            TicketCatalogVersionId = Guid.CreateVersion7(),
            CurrencyCode = "EUR",
            TicketTypes = [new RegistrationCheckoutTicketTypeDto
            {
                Id = Guid.CreateVersion7(),
                Name = "General admission",
                TicketPricingModeCode = "FIXED",
                FixedPriceMinor = 1200,
                PerOrderLimit = null
            }]
        });

        var cut = _ctx.RenderMudComponent<TicketSelection>(parameters => parameters.Add(component => component.EventId, eventId));

        var quantityField = cut.FindComponents<MudNumericField<int>>().Single();
        await Assert.That(quantityField.Instance.Max).IsEqualTo(int.MaxValue);

        cut.WaitForElement("input").Change("123");
        await cut.FindAll("button").Single(button => button.TextContent.Contains("Reserve selected tickets", StringComparison.Ordinal)).ClickAsync(new());

        await _service.Received(1).StartGuestAsync(
            eventId,
            Arg.Is<StartRegistrationOrderRequest>(request => request.Lines!.Single().Quantity == 123),
            Arg.Any<CancellationToken>());
    }
}
