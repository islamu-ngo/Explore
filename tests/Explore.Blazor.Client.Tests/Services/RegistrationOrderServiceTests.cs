// ABOUTME: Service tests for registration-order lifecycle transport and safe guest recovery failures.
// ABOUTME: Proves missing capability transport metadata never escapes into the page flow.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Http;
using Explore.Blazor.Client.Services.Shell;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class RegistrationOrderServiceTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IEventApiClient _apiClient;
    private readonly IBffClient _bffClient;
    private readonly RegistrationOrderService _service;

    public RegistrationOrderServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        var eventService = Substitute.For<IEventService>();
        var capabilityStore = Substitute.For<IGuestRegistrationOrderCapabilityStore>();
        _bffClient = Substitute.For<IBffClient>();
        var logger = Substitute.For<ILogger<RegistrationOrderService>>();
        _ctx.Services.AddScoped<IWorkspaceRegistry, WorkspaceRegistry>();
        _ctx.Services.AddScoped<WorkspaceRouteClassifier>();
        _ctx.Services.AddScoped<UiShellState>();
        var shellState = _ctx.Services.GetRequiredService<UiShellState>();
        _service = new RegistrationOrderService(_apiClient, eventService, shellState, capabilityStore, _bffClient, logger);
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task IssueGuestPaymentCheckoutTicketAsync_ForwardsCapabilityOnlyToBffHeaderBoundary()
    {
        var capability = new GuestRegistrationOrderCapability("guest-capability");
        const string issuePath = "/bff/registration-payments/events/event/orders/order/checkout-ticket";
        _bffClient.IssueRegistrationPaymentCheckoutTicketAsync(issuePath, capability.Value, Arg.Any<CancellationToken>())
            .Returns(new BffRegistrationPaymentCheckoutTicketResponseDto("/bff/registration-payments/checkout/opaque"));

        string? checkoutPath = await _service.IssueGuestPaymentCheckoutTicketAsync(issuePath, capability);

        await Assert.That(checkoutPath).IsEqualTo("/bff/registration-payments/checkout/opaque");
        await _bffClient.Received(1).IssueRegistrationPaymentCheckoutTicketAsync(
            issuePath, capability.Value, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartGuestAsync_WhenCapabilityHeaderIsMissing_ReturnsNullInsteadOfEscaping()
    {
        _apiClient.StartGuestRegistrationOrderWithCapabilityAsync(
                Arg.Any<Guid>(),
                Arg.Any<StartRegistrationOrderRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GuestRegistrationOrderStartResult>(
                new InvalidOperationException("Guest registration capability was not returned.")));

        var result = await _service.StartGuestAsync(Guid.CreateVersion7(), new StartRegistrationOrderRequest());

        await Assert.That(result).IsNull();
        await _apiClient.Received(1).StartGuestRegistrationOrderWithCapabilityAsync(
            Arg.Any<Guid>(),
            Arg.Any<StartRegistrationOrderRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyCurrentPromotionAsync_WithoutApplyPromotionRelation_DoesNotCallGeneratedClient()
    {
        var order = CreateOrder();

        var result = await _service.ApplyCurrentPromotionAsync(order.EventId!.Value, order.Id!.Value, order, "SAVE10");

        await Assert.That(result).IsNull();
        await _apiClient.DidNotReceive().ApplyAuthenticatedRegistrationOrderPromotionAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<PromotionCodeRequest>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyCurrentPromotionAsync_WithApplyPromotionRelation_UsesGeneratedClientThenReloadsOrder()
    {
        var order = CreateOrder("apply-promotion");
        var updated = CreateOrder("remove-promotion");
        updated.Id = order.Id;
        updated.EventId = order.EventId;
        _apiClient.ApplyAuthenticatedRegistrationOrderPromotionAsync(
                order.EventId!.Value,
                order.Id!.Value,
                Arg.Is<string>(value => IsUuid7(value)),
                Arg.Is<PromotionCodeRequest>(request => request.Code == "SAVE10"),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new PromotionRedemptionResponseDto { AppliedPromotionDisplayLabel = "Promotion ending in 10" });
        _apiClient.GetCurrentRegistrationOrderAsync(order.EventId.Value, order.Id.Value, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(updated);

        var result = await _service.ApplyCurrentPromotionAsync(order.EventId.Value, order.Id.Value, order, "SAVE10");

        await Assert.That(result).IsSameReferenceAs(updated);
        await _apiClient.Received(1).ApplyAuthenticatedRegistrationOrderPromotionAsync(
            order.EventId.Value,
            order.Id.Value,
            Arg.Is<string>(value => IsUuid7(value)),
            Arg.Any<PromotionCodeRequest>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoveCurrentPromotionAsync_WithoutRemovePromotionRelation_DoesNotCallGeneratedClient()
    {
        var order = CreateOrder();

        var result = await _service.RemoveCurrentPromotionAsync(order.EventId!.Value, order.Id!.Value, order);

        await Assert.That(result).IsNull();
        await _apiClient.DidNotReceive().RemoveAuthenticatedRegistrationOrderPromotionAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoveGuestPromotionAsync_WithRemovePromotionRelation_PreservesCapabilityHeaderTransport()
    {
        var order = CreateGuestOrder("remove-promotion");
        var capability = new GuestRegistrationOrderCapability("opaque-capability");
        _apiClient.RemoveGuestRegistrationOrderPromotionAsync(
                order.EventId!.Value,
                order.Id!.Value,
                capability.Value,
                Arg.Is<string?>(value => IsUuid7(value)),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new PromotionRedemptionResponseDto());
        _apiClient.GetGuestRegistrationOrderAsync(order.EventId.Value, order.Id.Value, capability.Value, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _service.RemoveGuestPromotionAsync(order.EventId.Value, order.Id.Value, capability, order);

        await Assert.That(result).IsSameReferenceAs(order);
        await _apiClient.Received(1).RemoveGuestRegistrationOrderPromotionAsync(
            order.EventId.Value,
            order.Id.Value,
            capability.Value,
            Arg.Is<string?>(value => IsUuid7(value)),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyGuestPromotionAsync_WithoutApplyPromotionRelation_DoesNotSendCapabilityOrCode()
    {
        var order = CreateGuestOrder();

        var result = await _service.ApplyGuestPromotionAsync(
            order.EventId!.Value,
            order.Id!.Value,
            new GuestRegistrationOrderCapability("opaque-capability"),
            order,
            "SAVE10");

        await Assert.That(result).IsNull();
        await _apiClient.DidNotReceive().ApplyGuestRegistrationOrderPromotionAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<PromotionCodeRequest>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartCurrentPaymentAsync_WithoutStartPaymentRelation_DoesNotCallGeneratedClient()
    {
        var order = CreateOrder();

        var result = await _service.StartCurrentPaymentAsync(order.EventId!.Value, order.Id!.Value, order, "revision");

        await Assert.That(result).IsNull();
        await _apiClient.DidNotReceive().StartAuthenticatedRegistrationPaymentAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<PaidOrderAcceptanceAcknowledgementDto?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartCurrentPaymentAsync_WithExactRelation_UsesFreshUuid7()
    {
        var order = CreateOrder("start-payment");
        var payment = CreatePayment("payment-status", "checkout-redirect");
        _apiClient.StartAuthenticatedRegistrationPaymentAsync(
                order.EventId!.Value, order.Id!.Value, Arg.Is<string>(value => IsUuid7(value)), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Is<PaidOrderAcceptanceAcknowledgementDto>(value => value.Acknowledged == true && value.DisclosureRevision == "revision"),
                Arg.Any<CancellationToken>())
            .Returns(payment);

        var result = await _service.StartCurrentPaymentAsync(order.EventId.Value, order.Id.Value, order, "revision");

        await Assert.That(result).IsSameReferenceAs(payment);
    }

    [Test]
    public async Task RetryGuestPaymentAsync_RequiresExactRelationAndPreservesCapability()
    {
        var capability = new GuestRegistrationOrderCapability("opaque-capability");
        var payment = CreatePayment("retry-payment");
        var retried = CreatePayment("payment-status");
        _apiClient.RetryGuestRegistrationPaymentAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Is<string>(value => IsUuid7(value)), capability.Value,
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(retried);

        var result = await _service.RetryGuestPaymentAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), capability, payment);

        await Assert.That(result).IsSameReferenceAs(retried);
        await Assert.That(retried._links?.ContainsKey("retry-payment") == true).IsFalse();
    }

    [Test]
    public async Task GetStudioPaymentAsync_WithoutStudioRelation_DoesNotCallGeneratedClient()
    {
        var order = CreateOrder();

        var result = await _service.GetStudioPaymentAsync(order.EventId!.Value, order.Id!.Value, order);

        await Assert.That(result).IsNull();
        await _apiClient.DidNotReceive().GetStudioRegistrationPaymentAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    private static HalResourceOfRegistrationOrderDto CreateOrder(params string[] relations)
    {
        var order = new HalResourceOfRegistrationOrderDto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7()
        };
        foreach (var relation in relations)
        {
            order._links ??= new Dictionary<string, HalLink>();
            order._links[relation] = new HalLink { Href = $"/orders/{order.Id}/{relation}", Method = "POST" };
        }

        return order;
    }

    private static HalResourceOfGuestRegistrationOrderDto CreateGuestOrder(params string[] relations)
    {
        var order = new HalResourceOfGuestRegistrationOrderDto
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7()
        };
        foreach (var relation in relations)
        {
            order._links ??= new Dictionary<string, HalLink>();
            order._links[relation] = new HalLink { Href = $"/guest/orders/{order.Id}/{relation}", Method = "POST" };
        }

        return order;
    }

    private static HalResourceOfRegistrationPaymentDto CreatePayment(params string[] relations) => new()
    {
        Id = Guid.CreateVersion7(),
        RegistrationOrderId = Guid.CreateVersion7(),
        StatusCode = "Processing",
        StatusName = "Processing",
        _links = relations.ToDictionary(
            relation => relation,
            relation => new HalLink { Href = $"/payments/{relation}", Method = relation == "payment-status" ? "GET" : "POST" })
    };

    private static bool IsUuid7(string? value) =>
        Guid.TryParse(value, out Guid idempotencyKey) && idempotencyKey.Version == 7;
}
