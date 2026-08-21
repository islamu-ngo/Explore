// ABOUTME: bUnit coverage for authoritative attendee payment state rendering and exact HAL actions.
// ABOUTME: Verifies every bounded status, same-origin checkout filtering, and no blind retry behavior.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Registration;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using System.Reflection;

namespace Explore.Blazor.Client.Tests.Components.Registration;

public sealed class PaymentStatusPanelTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly RecordingAnnouncer _announcer = new();
    private readonly RecordingFocus _focus = new();

    public PaymentStatusPanelTests()
    {
        _ctx.Services.RemoveAll<IAccessibilityAnnouncerService>();
        _ctx.Services.RemoveAll<IAccessibilityFocusService>();
        _ctx.Services.AddSingleton<IAccessibilityAnnouncerService>(_announcer);
        _ctx.Services.AddSingleton<IAccessibilityFocusService>(_focus);
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    [Arguments("Created", "waiting to continue")]
    [Arguments("Processing", "still processing")]
    [Arguments("RequiresAction", "Continue to secure checkout")]
    [Arguments("Unknown", "outcome is not known yet")]
    [Arguments("Failed", "payment failed")]
    [Arguments("Cancelled", "payment was cancelled")]
    [Arguments("Succeeded", "confirmed by the server")]
    [Arguments("NeedsReconciliation", "needs organizer review")]
    public async Task RendersCompleteGeneratedContractStateMatrix(string status, string copy)
    {
        var payment = CreatePayment(status, "payment-status");
        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment)));

        cut.WaitForAssertion(() => Assert.That(cut.Markup).Contains(copy));
        await Assert.That(cut.Find("#payment-actionable-status").GetAttribute("role")).IsNull();
        await Assert.That(cut.Find("#payment-actionable-status").GetAttribute("aria-live")).IsNull();
        await Assert.That(cut.FindAll("[data-testid='retry-payment']")).IsEmpty();
    }

    [Test]
    public async Task CheckoutActionAcceptsOnlySameOriginBffRelation()
    {
        var unsafePayment = CreatePayment("RequiresAction", "checkout-redirect");
        unsafePayment._links!["checkout-redirect"].Href = "https://provider.example/checkout";
        var unsafeCut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(unsafePayment)));
        unsafeCut.WaitForElement("[data-testid='payment-status']");
        await Assert.That(unsafeCut.FindAll("[data-testid='checkout-redirect']")).IsEmpty();
        await Assert.That(unsafeCut.FindAll("[data-testid='prepare-checkout']")).IsEmpty();
        using var safeContext = new BlazorTestContext();
        var safePayment = CreatePayment("RequiresAction", "checkout-redirect");
        var safeCut = safeContext.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(safePayment))
            .Add(component => component.IssueCheckoutTicket, (_, _) => Task.FromResult<string?>(null)));
        safeCut.WaitForElement("[data-testid='prepare-checkout']");
        await Assert.That(safeCut.FindAll("[data-testid='prepare-checkout']").Count).IsEqualTo(1);
    }

    [Test]
    public async Task CheckoutAction_PostsTicketIssuerThenNavigatesToOpaqueGetPath()
    {
        string? issuedFrom = null;
        var payment = CreatePayment("RequiresAction", "checkout-redirect");
        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment))
            .Add(component => component.IssueCheckoutTicket, (path, _) =>
            {
                issuedFrom = path;
                return Task.FromResult<string?>("/bff/registration-payments/checkout");
            }));
        cut.WaitForElement("[data-testid='prepare-checkout']");

        await cut.Find("[data-testid='prepare-checkout']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await Assert.That(issuedFrom).EndsWith("/checkout-ticket");
        var link = cut.Find("[data-testid='checkout-redirect']");
        await Assert.That(link.GetAttribute("href")).IsEqualTo("/bff/registration-payments/checkout");
        await Assert.That(link.GetAttribute("target")).IsEqualTo("_blank");
        await Assert.That(link.GetAttribute("rel")).IsEqualTo("opener");
        await Assert.That(_ctx.Services.GetRequiredService<NavigationManager>().Uri).IsEqualTo("http://localhost/");
    }

    [Test]
    public async Task CheckoutAction_DisposedWhileIssuing_DoesNotForceNavigate()
    {
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var payment = CreatePayment("RequiresAction", "checkout-redirect");
        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment))
            .Add(component => component.IssueCheckoutTicket, (_, _) => completion.Task));
        cut.WaitForElement("[data-testid='prepare-checkout']");
        Task click = cut.Find("[data-testid='prepare-checkout']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await cut.Instance.DisposeAsync();
        cut.Dispose();
        completion.SetResult("/bff/registration-payments/checkout");
        await click;

        await Assert.That(_ctx.Services.GetRequiredService<NavigationManager>().Uri)
            .IsEqualTo("http://localhost/");
    }

    [Test]
    public async Task CheckoutAction_PreservesApplicationSubpath()
    {
        using var context = new BlazorTestContext();
        var navigation = new SubpathNavigationManager();
        context.Services.AddSingleton<NavigationManager>(navigation);
        string? issuePath = null;
        var payment = CreatePayment("RequiresAction", "checkout-redirect");
        var cut = context.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment))
            .Add(component => component.IssueCheckoutTicket, (path, _) =>
            {
                issuePath = path;
                return Task.FromResult<string?>("/events/bff/registration-payments/checkout");
            }));
        cut.WaitForElement("[data-testid='prepare-checkout']");

        await cut.Find("[data-testid='prepare-checkout']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForElement("[data-testid='checkout-redirect']");

        await Assert.That(issuePath).StartsWith("/events/bff/registration-payments/");
        await Assert.That(cut.Find("[data-testid='checkout-redirect']").GetAttribute("href"))
            .IsEqualTo("/events/bff/registration-payments/checkout");
    }

    [Test]
    public async Task InitialAuthoritativeSuccess_ReloadsOrderOnce()
    {
        int reloads = 0;
        var payment = CreatePayment("Succeeded", "payment-status");

        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment))
            .Add(component => component.Succeeded, EventCallback.Factory.Create(this, () => reloads++)));

        cut.WaitForAssertion(() => Assert.That(reloads).IsEqualTo(1));
        await Assert.That(cut.Markup).Contains("Payment is confirmed by the server");
    }

    [Test]
    public async Task PollTransitionToFailure_RendersAssertiveFocusableStatus()
    {
        var processing = CreatePayment("Processing", "payment-status");
        var failed = CreatePayment("Failed", "payment-status", "retry-payment");
        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(processing))
            .Add(component => component.Refresh, (_, _) => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(failed))
            .Add(component => component.Retry, (payment, _) => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment)));
        cut.WaitForAssertion(
            () =>
            {
                if (!cut.Markup.Contains("The payment failed", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Payment status has not transitioned to failure.");
                }
            },
            TimeSpan.FromSeconds(5));
        var status = cut.Find("#payment-actionable-status");
        await Assert.That(status.GetAttribute("role")).IsNull();
        await Assert.That(status.GetAttribute("aria-live")).IsNull();
        await Assert.That(status.GetAttribute("tabindex")).IsEqualTo("-1");
        await Assert.That(cut.FindAll("[data-testid='retry-payment']").Count).IsEqualTo(1);
    }

    [Test]
    public async Task LoadNetworkFailure_RendersAccessibleBoundedErrorWithoutThrowing()
    {
        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromException<HalResourceOfRegistrationPaymentDto?>(new HttpRequestException("private transport detail"))));

        cut.WaitForElement("#payment-action-error");
        await Assert.That(cut.Markup).Contains("Payment status could not be loaded");
        await Assert.That(cut.Markup).DoesNotContain("private transport detail");
        await Assert.That(cut.Find("#payment-action-error").GetAttribute("tabindex")).IsEqualTo("-1");
    }

    [Test]
    public async Task CheckoutNetworkFailure_StaysOnPageAndShowsBoundedError()
    {
        var payment = CreatePayment("RequiresAction", "checkout-redirect");
        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment))
            .Add(component => component.IssueCheckoutTicket, (_, _) => Task.FromException<string?>(new JSException("private browser detail"))));
        cut.WaitForElement("[data-testid='prepare-checkout']");

        await cut.Find("[data-testid='prepare-checkout']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForElement("#payment-action-error");
        await Assert.That(cut.Markup).Contains("Secure checkout could not be opened");
        await Assert.That(cut.Markup).DoesNotContain("private browser detail");
        await Assert.That(_ctx.Services.GetRequiredService<NavigationManager>().Uri).IsEqualTo("http://localhost/");
        await Assert.That(_announcer.AssertiveMessages).IsEmpty();
        await Assert.That(_focus.Selectors).IsEquivalentTo(["#payment-action-error"]);
    }

    [Test]
    public async Task RetryableFailedTransition_UsesFocusAsItsOnlyAnnouncementChannel()
    {
        var failed = CreatePayment("Failed", "payment-status", "retry-payment");
        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(failed))
            .Add(component => component.Retry, (payment, _) => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment)));

        cut.WaitForElement("#payment-actionable-status");
        await Assert.That(_announcer.AssertiveMessages).IsEmpty();
        await Assert.That(_announcer.PoliteMessages).IsEmpty();
        await Assert.That(_focus.Selectors).IsEquivalentTo(["#payment-actionable-status"]);
        await Assert.That(cut.Find("#payment-actionable-status").GetAttribute("role")).IsNull();
        await Assert.That(cut.Find("#payment-actionable-status").GetAttribute("aria-live")).IsNull();
    }

    [Test]
    public async Task NeedsReconciliation_UsesGlobalAssertiveChannelWithoutMovingFocus()
    {
        var payment = CreatePayment("NeedsReconciliation", "payment-status");

        _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment)));

        await Assert.That(_announcer.AssertiveMessages.Count).IsEqualTo(1);
        await Assert.That(_announcer.PoliteMessages).IsEmpty();
        await Assert.That(_focus.Selectors).IsEmpty();
    }

    [Test]
    public async Task Polling_UsesProgressiveCappedBackoffAndOneTrackedNonOverlappingTask()
    {
        var now = DateTimeOffset.Parse("2026-08-21T12:00:00Z");
        var clock = new MutableTimeProvider(now);
        var delays = new List<TimeSpan>();
        int refreshCount = 0;
        int activeRefreshes = 0;
        int maximumActiveRefreshes = 0;
        var processing = CreatePayment("Processing", "payment-status");
        processing.ExpiresAt = now.AddMinutes(5);

        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(processing))
            .Add(component => component.Refresh, async (_, _) =>
            {
                activeRefreshes++;
                maximumActiveRefreshes = Math.Max(maximumActiveRefreshes, activeRefreshes);
                await Task.Yield();
                activeRefreshes--;
                refreshCount++;
                return refreshCount == 4 ? CreatePayment("Failed", "payment-status") : processing;
            })
            .Add(component => component.Clock, clock)
            .Add(component => component.PollDelay, (delay, _) =>
            {
                delays.Add(delay);
                clock.Advance(delay);
                return Task.CompletedTask;
            }));

        cut.WaitForAssertion(() =>
        {
            if (refreshCount != 4)
            {
                throw new InvalidOperationException("Polling sequence has not completed.");
            }
        });
        var pollingTask = (Task?)typeof(PaymentStatusPanel)
            .GetField("_pollingTask", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(cut.Instance);
        cut.Render();
        var rerenderedTask = (Task?)typeof(PaymentStatusPanel)
            .GetField("_pollingTask", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(cut.Instance);

        await Assert.That(delays).IsEquivalentTo([
            TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15)]);
        await Assert.That(maximumActiveRefreshes).IsEqualTo(1);
        await Assert.That(pollingTask).IsSameReferenceAs(rerenderedTask);
    }

    [Test]
    public async Task Polling_StopsAtEarliestAuthoritativeCutoffReloadsOnceAndOffersNoRetry()
    {
        var now = DateTimeOffset.Parse("2026-08-21T12:00:00Z");
        var clock = new MutableTimeProvider(now);
        var delays = new List<TimeSpan>();
        int refreshCount = 0;
        int expiryReloads = 0;
        var processing = CreatePayment("Processing", "payment-status", "retry-payment");
        processing.ExpiresAt = now.AddSeconds(30);

        var cut = _ctx.RenderMudComponent<PaymentStatusPanel>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(processing))
            .Add(component => component.Refresh, (_, _) =>
            {
                refreshCount++;
                return Task.FromResult<HalResourceOfRegistrationPaymentDto?>(processing);
            })
            .Add(component => component.Retry, (payment, _) => Task.FromResult<HalResourceOfRegistrationPaymentDto?>(payment))
            .Add(component => component.OrderExpiresAt, now.AddSeconds(8))
            .Add(component => component.Expired, EventCallback.Factory.Create(this, () => expiryReloads++))
            .Add(component => component.Clock, clock)
            .Add(component => component.PollDelay, (delay, _) =>
            {
                delays.Add(delay);
                clock.Advance(delay);
                return Task.CompletedTask;
            }));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Payment window expired", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expiry has not rendered.");
            }
        });

        await Assert.That(delays).IsEquivalentTo([TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5)]);
        await Assert.That(refreshCount).IsEqualTo(1);
        await Assert.That(expiryReloads).IsEqualTo(1);
        await Assert.That(cut.FindAll("[data-testid='retry-payment']")).IsEmpty();
        await Assert.That(cut.FindAll("[data-testid='checkout-redirect']")).IsEmpty();
    }

    private static HalResourceOfRegistrationPaymentDto CreatePayment(string status, params string[] relations) => new()
    {
        StatusCode = status,
        StatusName = status.Replace('_', ' '),
        LastUpdatedAt = DateTimeOffset.UtcNow,
        _links = relations.ToDictionary(
            relation => relation,
            relation => new HalLink
            {
                Href = relation == "checkout-redirect"
                    ? "bff/registration-payments/events/018e4e5c-7f00-7000-8000-000000000001/orders/018e4e5c-7f00-7000-8000-000000000002/checkout-ticket"
                    : "/api/payment",
                Method = relation == "payment-status" ? "GET" : "POST"
            })
    };

    private sealed class RecordingAnnouncer : IAccessibilityAnnouncerService
    {
        public List<string> PoliteMessages { get; } = [];
        public List<string> AssertiveMessages { get; } = [];
        public Task AnnouncePoliteAsync(string message) { PoliteMessages.Add(message); return Task.CompletedTask; }
        public Task AnnounceAssertiveAsync(string message) { AssertiveMessages.Add(message); return Task.CompletedTask; }
    }

    private sealed class SubpathNavigationManager : NavigationManager
    {
        public SubpathNavigationManager() => Initialize("https://event.example/events/", "https://event.example/events/");

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            Uri = ToAbsoluteUri(uri).AbsoluteUri;
    }

    private sealed class RecordingFocus : IAccessibilityFocusService
    {
        public List<string> Selectors { get; } = [];
        public Task FocusAsync(string cssSelector, bool preventScroll = false) { Selectors.Add(cssSelector); return Task.CompletedTask; }
        public Task FocusByIdAsync(string elementId, bool preventScroll = false) => Task.CompletedTask;
        public Task FocusMainContentAsync() => Task.CompletedTask;
        public Task FocusOnNavigateAsync() => Task.CompletedTask;
        public Task SaveFocusAsync() => Task.CompletedTask;
        public Task RestoreFocusAsync(string? fallbackSelector = null) => Task.CompletedTask;
        public Task<string> GetPreferredMotionAsync() => Task.FromResult("reduce");
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
