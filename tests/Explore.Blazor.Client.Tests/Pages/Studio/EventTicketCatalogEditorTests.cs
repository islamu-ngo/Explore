// ABOUTME: bUnit coverage for HAL-gated ticket catalog controls and draft mutations.
// ABOUTME: Proves authoring controls fail closed while read-only catalog content remains visible.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Pages.Studio;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Studio;

public sealed class EventTicketCatalogEditorTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IEventTicketingService _ticketingService;
    private readonly IEventDayService _eventDayService;
    private readonly IEventService _eventService;
    private readonly IDialogService _dialogService;
    private readonly IAccessibilityAnnouncerService _announcer;
    private readonly IAccessibilityFocusService _focusService;

    public EventTicketCatalogEditorTests()
    {
        _ticketingService = _ctx.AddMockService<IEventTicketingService>();
        _eventDayService = _ctx.AddMockService<IEventDayService>();
        _eventService = _ctx.AddMockService<IEventService>();
        _dialogService = Substitute.For<IDialogService>();
        _announcer = _ctx.AddMockService<IAccessibilityAnnouncerService>();
        _focusService = _ctx.AddMockService<IAccessibilityFocusService>();
        _ctx.Services.AddSingleton(_dialogService);
        _eventDayService.GetDaysByEventAsync(Arg.Any<Guid>(), true).Returns([]);
        _eventService.GetSessionsByEventAsync(Arg.Any<Guid>(), true).Returns([]);
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task RenderWithoutMutationLinksKeepsCatalogReadOnly()
    {
        var eventId = Guid.CreateVersion7();
        _ticketingService.GetCatalogAsync(eventId).Returns(CreateCatalog(eventId));

        var cut = _ctx.RenderMudComponent<EventTicketCatalogEditor>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.CanManageTicketTypes, true)
            .Add(component => component.CanManageCapacityPools, true));

        cut.WaitForElement("[data-testid='event-ticket-catalog-editor']");
        cut.WaitForAssertion(() => cut.Markup.Contains("No ticket types yet.", StringComparison.Ordinal));
        await Assert.That(cut.FindAll("[data-testid='add-ticket-type']")).IsEmpty();
        await Assert.That(cut.FindAll("[data-testid='add-capacity-pool']")).IsEmpty();
        await Assert.That(cut.FindAll("[data-testid='publish-ticket-catalog']")).IsEmpty();
    }

    [Test]
    public async Task RenderShowsOnlyControlsBackedByExactCatalogLinks()
    {
        var eventId = Guid.CreateVersion7();
        _ticketingService.GetCatalogAsync(eventId).Returns(CreateCatalog(eventId, "create-type", "publish"));

        var cut = _ctx.RenderMudComponent<EventTicketCatalogEditor>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.CanManageTicketTypes, true));

        cut.WaitForElement("[data-testid='add-ticket-type']");
        await Assert.That(cut.FindAll("[data-testid='publish-ticket-catalog']").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll("[data-testid='add-capacity-pool']")).IsEmpty();
        await Assert.That(cut.FindAll("[data-testid='create-ticket-catalog-draft']")).IsEmpty();
    }

    [Test]
    public async Task CreateDraftUsesSelectedCurrencyAndReloadsCatalog()
    {
        var eventId = Guid.CreateVersion7();
        var initial = CreateCatalog(eventId, "create-draft");
        var created = CreateCatalog(eventId, "create-type", "create-pool", "publish");
        _ticketingService.GetCatalogAsync(eventId).Returns(initial, created);
        _ticketingService.CreateDraftAsync(eventId, Arg.Any<CreateEventTicketCatalogDraftCommand>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });

        var cut = _ctx.RenderMudComponent<EventTicketCatalogEditor>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.CanManageCapacityPools, true));

        cut.WaitForElement("[data-testid='create-ticket-catalog-draft']").Click();
        cut.WaitForElement("[data-testid='ticket-catalog-status']");

        await _ticketingService.Received(1).CreateDraftAsync(
            eventId,
            Arg.Is<CreateEventTicketCatalogDraftCommand>(request => request.EventId == eventId && request.CurrencyCode == "USD"));
        await Assert.That(cut.FindAll("[data-testid='add-capacity-pool']").Count).IsEqualTo(1);
        await _announcer.Received(1).AnnouncePoliteAsync("Draft created.");
    }

    [Test]
    [Arguments(true, false, 1, 0)]
    [Arguments(false, true, 0, 1)]
    [Arguments(false, false, 0, 0)]
    public async Task EventCapabilitiesIndependentlyGateTypeAndPoolControls(
        bool canManageTypes,
        bool canManagePools,
        int expectedTypeControls,
        int expectedPoolControls)
    {
        var eventId = Guid.CreateVersion7();
        _ticketingService.GetCatalogAsync(eventId).Returns(CreateCatalog(eventId, "create-type", "create-pool"));

        var cut = _ctx.RenderMudComponent<EventTicketCatalogEditor>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.CanManageTicketTypes, canManageTypes)
            .Add(component => component.CanManageCapacityPools, canManagePools));

        cut.WaitForElement("[data-testid='event-ticket-catalog-editor']");
        cut.WaitForAssertion(() => cut.Markup.Contains("No ticket types yet.", StringComparison.Ordinal));
        await Assert.That(cut.FindAll("[data-testid='add-ticket-type']").Count).IsEqualTo(expectedTypeControls);
        await Assert.That(cut.FindAll("[data-testid='add-capacity-pool']").Count).IsEqualTo(expectedPoolControls);
    }

    [Test]
    public async Task LoadAnnouncesCatalogCountsAndProvidesManagedTargetsToInlineEditor()
    {
        var eventId = Guid.CreateVersion7();
        var dayId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        _ticketingService.GetCatalogAsync(eventId).Returns(CreateCatalog(eventId, "create-type"));
        _eventDayService.GetDaysByEventAsync(eventId, true).Returns([new EventDayListDto { Id = dayId, Label = "Friday" }]);
        _eventService.GetSessionsByEventAsync(eventId, true).Returns([new EventSessionListDto { Id = sessionId, EventTitle = "Event", Title = "Opening" }]);

        var cut = _ctx.RenderMudComponent<EventTicketCatalogEditor>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.CanManageTicketTypes, true));

        cut.WaitForElement("[data-testid='add-ticket-type']").Click();
        var inlineEditor = cut.FindComponent<EventTicketTypeEditor>();
        await Assert.That(inlineEditor.Instance.EventDays.Single().Id).IsEqualTo(dayId);
        await Assert.That(inlineEditor.Instance.EventSessions.Single().Id).IsEqualTo(sessionId);
        await _announcer.Received(1).AnnouncePoliteAsync("Ticket catalog loaded: 0 ticket types and 0 capacity pools.");
    }

    [Test]
    public async Task PublishCancellationRestoresFocusAndPerformsNoMutation()
    {
        var eventId = Guid.CreateVersion7();
        _ticketingService.GetCatalogAsync(eventId).Returns(CreateCatalog(eventId, "publish"));
        _dialogService.ShowMessageBoxAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DialogOptions>())
            .Returns(false);

        var cut = _ctx.RenderMudComponent<EventTicketCatalogEditor>(parameters => parameters
            .Add(component => component.EventId, eventId));

        cut.WaitForElement("[data-testid='publish-ticket-catalog']").Click();
        await _focusService.Received(1).SaveFocusAsync();
        await _focusService.Received(1).RestoreFocusAsync();
        await _ticketingService.DidNotReceive().PublishAsync(eventId);
    }

    [Test]
    public async Task MutationFailureIsAnnouncedAssertively()
    {
        var eventId = Guid.CreateVersion7();
        _ticketingService.GetCatalogAsync(eventId).Returns(CreateCatalog(eventId, "create-draft"));
        _ticketingService.CreateDraftAsync(eventId, Arg.Any<CreateEventTicketCatalogDraftCommand>())
            .Returns(new BaseCommandResponseOfGuid { Success = false, Message = "Catalog is locked." });

        var cut = _ctx.RenderMudComponent<EventTicketCatalogEditor>(parameters => parameters
            .Add(component => component.EventId, eventId));

        cut.WaitForElement("[data-testid='create-ticket-catalog-draft']").Click();
        cut.WaitForElement("[data-testid='ticket-catalog-error']");
        await _announcer.Received(1).AnnounceAssertiveAsync("Catalog is locked.");
    }

    [Test]
    public async Task MissingSupportDataUsesBoundedGuidanceWithoutRawIdentifierInput()
    {
        var model = TicketTypeEditModel.Create();
        model.Entitlements.Single().SetScope(2);

        var cut = _ctx.RenderMudComponent<EventTicketTypeEditor>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.SupportDataUnavailable, true));

        cut.WaitForElement("[data-testid='ticket-entitlement-support-guidance']");
        await Assert.That(cut.Markup).DoesNotContain("Event day ID");
        await Assert.That(cut.Markup).Contains("whole-event admission");
    }

    private static EventTicketCatalogState CreateCatalog(Guid eventId, params string[] relations) => new(
        eventId,
        Guid.CreateVersion7(),
        1,
        "USD",
        1,
        "DRAFT",
        "Draft",
        [],
        [],
        relations.ToDictionary(
            relation => relation,
            relation => new HalLink { Href = $"/api/events/{eventId}/ticketing/{relation}", Method = "POST" },
            StringComparer.Ordinal));
}

public sealed class TicketCatalogEditModelsTests
{
    [Test]
    public async Task TicketTypePricingModesRejectIncompatibleAmounts()
    {
        var model = TicketTypeEditModel.Create();
        model.Name = "General admission";

        model.SetPricingMode(1);
        await Assert.That(model.IsValid).IsFalse();
        model.FixedPriceMinor = 2500;
        await Assert.That(model.IsValid).IsTrue();

        model.SetPricingMode(2);
        await Assert.That(model.FixedPriceMinor).IsNull();
        await Assert.That(model.IsValid).IsTrue();

        model.SetPricingMode(5);
        model.MinimumPriceMinor = 1000;
        model.SuggestedPriceMinor = 500;
        await Assert.That(model.IsValid).IsFalse();
    }

    [Test]
    public async Task EntitlementScopeClearsForeignTargetIdentifiers()
    {
        var model = TicketEntitlementEditModel.Create();
        model.SetScope(3);
        model.EventSessionId = Guid.CreateVersion7();
        await Assert.That(model.IsValid).IsTrue();

        model.SetScope(1);
        await Assert.That(model.EventSessionId).IsNull();
        await Assert.That(model.SelectionRuleId).IsEqualTo(1);
        await Assert.That(model.IsValid).IsTrue();
    }
}
