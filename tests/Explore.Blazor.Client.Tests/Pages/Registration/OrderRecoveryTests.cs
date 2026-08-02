// ABOUTME: bUnit coverage for account and guest registration-order recovery pages.
// ABOUTME: Verifies HAL-only authenticated cancellation and fail-closed guest capability handling.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Registration.FormRenderer;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Pages.Registration;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Registration;

public sealed class OrderRecoveryTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IRegistrationOrderService _service;
    private readonly INativeRegistrationFormService _nativeForms;
    private readonly IGuestRegistrationOrderCapabilityStore _capabilityStore;

    public OrderRecoveryTests()
    {
        _service = _ctx.AddMockService<IRegistrationOrderService>();
        _nativeForms = _ctx.AddMockService<INativeRegistrationFormService>();
        _capabilityStore = _ctx.AddMockService<IGuestRegistrationOrderCapabilityStore>();
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task AuthenticatedRecovery_RendersServerStatusAndHoldCountdownWithoutCancelRelation()
    {
        var order = CreateOrder("EXPIRED", "Expired");
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfRegistrationOrderDto?>(order));

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => cut.Find("h2").TextContent.Contains("Expired", StringComparison.Ordinal));
        await Assert.That(cut.Markup).Contains("This reservation has expired.");
        await Assert.That(cut.Markup).DoesNotContain("Cancel registration order");
    }

    [Test]
    public async Task AuthenticatedRecovery_RendersCancelOnlyWhenOrderHalContainsCancel()
    {
        var order = CreateOrder("AWAITING_APPROVAL", "Awaiting approval", "cancel");
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfRegistrationOrderDto?>(order));

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => cut.Markup.Contains("Cancel registration order", StringComparison.Ordinal));
        await Assert.That(cut.Markup).Contains("This registration is waiting for organizer approval.");
    }

    [Test]
    public async Task AuthenticatedRecovery_RendersNativeJourneyOnlyFromRequirementProgressRelation()
    {
        var order = CreateOrder("AWAITING_REQUIREMENTS", "Awaiting requirements", "requirement-progress");
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>()).Returns(order);
        _nativeForms.GetRequirementsAsync(order.EventId.Value, order.Id.Value, null, Arg.Any<CancellationToken>())
            .Returns(new NativeRegistrationRequirementCollectionView(
                [], new Dictionary<string, HalLink> { ["launch-attempt"] = new() { Href = "/launch", Method = "POST" } }));

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("All registration details are complete."));
        await _nativeForms.Received(1).GetRequirementsAsync(
            order.EventId.Value, order.Id.Value, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AuthenticatedRecovery_RendersLifecycleActionsAndServerContributionOptionsFromHal()
    {
        var order = CreateOrder("READY_FOR_CHECKOUT", "Ready for checkout", "continue", "finalize");
        order.PlatformContribution = new PlatformContribution2
        {
            SelectedBasisPoints = 0,
            Options =
            [
                new Options12 { ContributionBasisPoints = 0, AmountMinor = 0, IsDefault = true },
                new Options12 { ContributionBasisPoints = 500, AmountMinor = 625 }
            ]
        };
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfRegistrationOrderDto?>(order));

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup).Contains("Continue registration");
            Assert.That(cut.Markup).Contains("Finalize registration");
        });
        await Assert.That(cut.Markup).Contains("Platform contribution");
        await Assert.That(cut.FindComponents<MudSelectItem<int?>>().Select(item => item.Instance.Value))
            .Contains(500);
    }

    [Test]
    public async Task AuthenticatedRecovery_ReadyForCheckout_RequiresExactLifecycleRelations()
    {
        var order = CreateOrder("READY_FOR_CHECKOUT", "Ready for checkout");
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>())
            .Returns(order);

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("Review your order and complete checkout."));
        await Assert.That(cut.Markup).DoesNotContain("Continue registration");
        await Assert.That(cut.Markup).DoesNotContain("Finalize registration");
    }

    [Test]
    public async Task AuthenticatedRecovery_InvokesContinueOnlyWhenContinueRelationExists()
    {
        var order = CreateOrder("READY_FOR_CHECKOUT", "Ready for checkout", "continue");
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>()).Returns(order);
        _service.ContinueCurrentAsync(order.EventId.Value, order.Id.Value, Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("Continue registration"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Continue registration", StringComparison.Ordinal)).Click());

        await _service.Received(1).ContinueCurrentAsync(order.EventId.Value, order.Id.Value, null, Arg.Any<CancellationToken>());
        await _service.DidNotReceive().FinalizeCurrentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AuthenticatedRecovery_InvokesFinalizeOnlyWhenFinalizeRelationExists()
    {
        var order = CreateOrder("READY_FOR_CHECKOUT", "Ready for checkout", "finalize");
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>()).Returns(order);
        _service.FinalizeCurrentAsync(order.EventId.Value, order.Id.Value, Arg.Any<CancellationToken>()).Returns(order);

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("Finalize registration"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Finalize registration", StringComparison.Ordinal)).Click());

        await _service.Received(1).FinalizeCurrentAsync(order.EventId.Value, order.Id.Value, Arg.Any<CancellationToken>());
        await _service.DidNotReceive().ContinueCurrentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AuthenticatedRecovery_ContinueSubmitsOnlySelectedContributionBasisPoints()
    {
        var order = CreateOrder("READY_FOR_CHECKOUT", "Ready for checkout", "continue");
        order.PlatformContribution = new PlatformContribution2
        {
            SelectedBasisPoints = 500,
            Options = [new Options12 { ContributionBasisPoints = 500, AmountMinor = 625 }]
        };
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>()).Returns(order);
        _service.ContinueCurrentAsync(order.EventId.Value, order.Id.Value, 500, Arg.Any<CancellationToken>()).Returns(order);

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("Platform contribution"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Continue registration", StringComparison.Ordinal)).Click());

        await _service.Received(1).ContinueCurrentAsync(order.EventId.Value, order.Id.Value, 500, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AuthenticatedRecovery_ReadyForCheckout_RendersContributionValuesFromDto()
    {
        var order = CreateOrder("READY_FOR_CHECKOUT", "Ready for checkout", "continue");
        order.PlatformContribution = new PlatformContribution2
        {
            Heading = "Support this event",
            Body = "Choose an optional contribution.",
            SelectedBasisPoints = 500,
            Options =
            [
                new Options12 { ContributionBasisPoints = 0, AmountMinor = 0, IsDefault = true },
                new Options12 { ContributionBasisPoints = 500, AmountMinor = 625 }
            ]
        };
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>())
            .Returns(order);

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains("Support this event"));
        await Assert.That(cut.Markup).Contains("5%");
        await Assert.That(cut.Markup).Contains("625 EUR minor units");
    }

    [Test]
    public async Task GuestRecovery_WithoutInMemoryCapability_FailsClosed()
    {
        var cut = _ctx.RenderMudComponent<GuestOrderRecovery>(parameters => parameters
            .Add(component => component.EventId, Guid.CreateVersion7())
            .Add(component => component.OrderId, Guid.CreateVersion7()));

        cut.WaitForElement("[role='alert']");
        await Assert.That(cut.Markup).Contains("same guest registration session");
        await _service.DidNotReceive().GetGuestAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<GuestRegistrationOrderCapability>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GuestRecovery_RendersActionsOnlyFromHalRelations()
    {
        var eventId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var capability = new GuestRegistrationOrderCapability("opaque-capability");
        _capabilityStore.TryGet(eventId, orderId, out Arg.Any<GuestRegistrationOrderCapability?>())
            .Returns(call =>
            {
                call[2] = capability;
                return true;
            });
        _service.GetGuestAsync(eventId, orderId, capability, Arg.Any<CancellationToken>()).Returns(
            new HalResourceOfGuestRegistrationOrderDto
            {
                Id = orderId,
                EventId = eventId,
                StatusCode = "READY_FOR_CHECKOUT",
                StatusName = "Ready for checkout",
                CurrencyCode = "EUR",
                _links = new Dictionary<string, HalLink>
                {
                    ["continue"] = new() { Href = "/continue", Method = "POST" }
                }
            });

        var cut = _ctx.RenderMudComponent<GuestOrderRecovery>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.OrderId, orderId));

        cut.WaitForAssertion(() => cut.Markup.Contains("Continue registration", StringComparison.Ordinal));
        await Assert.That(cut.Markup).DoesNotContain("Finalize registration");
        await Assert.That(cut.Markup).DoesNotContain("Cancel registration order");
    }

    [Test]
    [Arguments("CONFIRMED", "Your registration is confirmed.")]
    [Arguments("DRAFT", "Choose your tickets to begin registration.")]
    [Arguments("AWAITING_IDENTITY", "Confirm your identity to continue registration.")]
    [Arguments("AWAITING_PARTICIPANT_DETAILS", "Add the required participant details to continue.")]
    [Arguments("AWAITING_REQUIREMENTS", "Complete the remaining registration requirements.")]
    [Arguments("READY_FOR_CHECKOUT", "Review your order and complete checkout.")]
    [Arguments("AWAITING_PAYMENT", "Payment is required")]
    [Arguments("AWAITING_APPROVAL", "waiting for organizer approval")]
    [Arguments("WAITLISTED", "waitlisted")]
    [Arguments("REJECTED", "not approved")]
    [Arguments("EXPIRED", "reservation expired")]
    [Arguments("CANCELLED", "was cancelled")]
    [Arguments("NEEDS_RECONCILIATION", "needs organizer review")]
    public async Task AuthenticatedRecovery_RendersStatusSpecificGuidance(string statusCode, string guidance)
    {
        var order = CreateOrder(statusCode, statusCode.Replace('_', ' '));
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>()).Returns(order);

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => cut.Markup.Contains(guidance, StringComparison.Ordinal));
        await Assert.That(cut.Markup).Contains(guidance);
    }

    [Test]
    public async Task AuthenticatedRecovery_WithActiveHold_RendersCountdown()
    {
        var order = CreateOrder("READY_FOR_CHECKOUT", "Ready for checkout");
        order.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        _service.GetCurrentAsync(order.EventId!.Value, order.Id!.Value, Arg.Any<CancellationToken>()).Returns(order);

        var cut = _ctx.RenderMudComponent<OrderRecovery>(parameters => parameters
            .Add(component => component.EventId, order.EventId.Value)
            .Add(component => component.OrderId, order.Id.Value));

        cut.WaitForAssertion(() => cut.Markup.Contains("Reservation expires in", StringComparison.Ordinal));
        await Assert.That(cut.Markup).Contains("Reservation expires in");
    }

    [Test]
    public async Task AuthenticatedRecovery_CountdownOverOneHour_RendersTotalRemainingMinutes()
    {
        var now = DateTimeOffset.UtcNow;
        await Assert.That(RegistrationOrderRecovery.Countdown(now.AddMinutes(61), now))
            .IsEqualTo("Reservation expires in 61:00.");
    }

    [Test]
    public async Task GuestRecovery_ContributionOptionsUseServerAmountsAndPreselectZero()
    {
        var eventId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var capability = new GuestRegistrationOrderCapability("opaque-capability");
        _capabilityStore.TryGet(eventId, orderId, out Arg.Any<GuestRegistrationOrderCapability?>())
            .Returns(call =>
            {
                call[2] = capability;
                return true;
            });
        _service.GetGuestAsync(eventId, orderId, capability, Arg.Any<CancellationToken>()).Returns(
            new HalResourceOfGuestRegistrationOrderDto
            {
                Id = orderId,
                EventId = eventId,
                StatusCode = "READY_FOR_CHECKOUT",
                StatusName = "Ready for checkout",
                CurrencyCode = "EUR",
                PlatformContribution = new PlatformContribution
                {
                    SelectedBasisPoints = 0,
                    Options =
                    [
                        new Options11 { ContributionBasisPoints = 0, AmountMinor = 0, IsDefault = true },
                        new Options11 { ContributionBasisPoints = 500, AmountMinor = 625 }
                    ]
                }
            });

        var cut = _ctx.RenderMudComponent<GuestOrderRecovery>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.OrderId, orderId));

        var select = cut.WaitForComponent<MudSelect<int?>>();
        await Assert.That(select.Instance.Value).IsEqualTo(0);
        var optionValues = cut.FindComponents<MudSelectItem<int?>>().Select(item => item.Instance.Value).ToArray();
        await Assert.That(optionValues).Contains(0);
        await Assert.That(optionValues).Contains(500);
    }

    private static HalResourceOfRegistrationOrderDto CreateOrder(string statusCode, string statusName, params string[] relations)
    {
        var eventId = Guid.CreateVersion7();
        var resource = new HalResourceOfRegistrationOrderDto
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            StatusCode = statusCode,
            StatusName = statusName,
            CurrencyCode = "EUR",
            TotalDueMinor = 1250,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1),
            Lines = [new Lines2 { Quantity = 1, TicketTypeName = "General admission", SubtotalMinor = 1250, CurrencyCode = "EUR" }]
        };

        foreach (var relation in relations)
        {
            resource._links ??= new Dictionary<string, HalLink>();
            resource._links[relation] = new HalLink { Href = $"/api/orders/{resource.Id}/{relation}" };
        }

        return resource;
    }
}
