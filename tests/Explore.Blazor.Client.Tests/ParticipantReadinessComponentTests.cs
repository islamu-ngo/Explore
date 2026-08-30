// ABOUTME: Defines RED HAL, bounded-state, and accessibility contracts for participant readiness UI.
// ABOUTME: Uses dynamic bUnit rendering so absent production types fail by assertion rather than compilation.

using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Explore.Blazor.Client.Tests;

public sealed class ParticipantReadinessComponentTests :
    IDisposable
{
    private const string ComponentName =
        "Explore.Blazor.Client.Components.Admissions." +
        "ParticipantReadinessPanel";
    private const string ServiceName =
        "Explore.Blazor.Client.Contracts.Services.Admissions." +
        "IParticipantReadinessService";
    private readonly BlazorTestContext _context = new();

    public void Dispose() => _context.Dispose();

    [Test]
    public async Task UnavailableReadinessIsGenericAndActionless()
    {
        var proxy = new ReadinessServiceProxy();
        RenderedReadiness cut = Render(proxy);

        await Assert.That(cut.FindAll(
                "[data-testid='participant-readiness']"))
            .HasSingleItem();
        await Assert.That(cut.FindAll(
                "[role='alert']" +
                "[data-readiness-state='unavailable']"))
            .HasSingleItem();
        await Assert.That(cut.FindAll("button")).IsEmpty();
    }

    [Test]
    public async Task PendingStateUsesBoundedCopyWithoutLocalActions()
    {
        var proxy = new ReadinessServiceProxy
        {
            Resource = Resource(
                "participant_completion_pending",
                "action_required"),
        };
        RenderedReadiness cut = Render(proxy);

        await Assert.That(cut.FindAll(
                "section" +
                "[aria-labelledby^=" +
                "'participant-readiness-title-']"))
            .HasSingleItem();
        await Assert.That(cut.FindAll(
                "[role='status']" +
                "[data-readiness-code=" +
                "'participant_completion_pending']"))
            .HasSingleItem();
        await Assert.That(cut.Markup)
            .DoesNotContain(
                ">participant_completion_pending<");
        await Assert.That(cut.FindAll("button")).IsEmpty();
    }

    [Test]
    public async Task HalRelationsAloneControlRenderedActions()
    {
        var proxy = new ReadinessServiceProxy
        {
            Resource = Resource(
                "participant_completion_pending",
                "action_required",
                "complete-participant-readiness"),
        };
        RenderedReadiness cut = Render(proxy);

        await Assert.That(cut.FindAll(
                "button" +
                "[data-relation=" +
                "'complete-participant-readiness']"))
            .HasSingleItem();
        await Assert.That(cut.FindAll(
                "button[data-relation=" +
                "'approve-participant-readiness']"))
            .IsEmpty();
        await Assert.That(cut.FindAll(
                "button[data-relation=" +
                "'revoke-participant-readiness']"))
            .IsEmpty();
    }

    [Test]
    public async Task ActionDisablesUntilExactCompletionThenFocusesError()
    {
        var completion = new TaskCompletionSource<
            HalResourceOfParticipantReadinessDto?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var proxy = new ReadinessServiceProxy
        {
            Resource = Resource(
                "participant_completion_pending",
                "action_required",
                "complete-participant-readiness"),
            ActionCompletion = completion,
            ActionEntered = entered,
        };
        RenderedReadiness cut = Render(proxy);
        IAccessibilityFocusService focus =
            _context.Services.GetRequiredService<
                IAccessibilityFocusService>();
        var focused = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        focus.FocusAsync(
                Arg.Any<string>(),
                Arg.Any<bool>())
            .Returns(_ =>
            {
                focused.TrySetResult();
                return Task.CompletedTask;
            });
        IElement button = cut.Find(
            "button[data-relation=" +
            "'complete-participant-readiness']");

        Task click = cut.InvokeAsync(() => button.Click());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(cut.Find(
                "button[data-relation=" +
                "'complete-participant-readiness']")
                .HasAttribute("disabled"))
            .IsTrue();
        completion.SetResult(null);
        await focused.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await click;

        await Assert.That(cut.Markup).Contains(
            "participant-readiness-outcome-");
        await Assert.That(cut.Markup).Contains(
            "Admission readiness could not be updated.");
        await Assert.That(cut.FindAll(
                "[id^='participant-readiness-outcome-']"))
            .HasSingleItem();
        IElement outcome = cut.Find(
            "[id^='participant-readiness-outcome-']");
        await Assert.That(outcome.GetAttribute("role"))
            .IsEqualTo("alert");
        await Assert.That(outcome.GetAttribute("aria-live"))
            .IsEqualTo("assertive");
        await Assert.That(outcome.GetAttribute("tabindex"))
            .IsEqualTo("-1");
        await focus.Received(1).FocusAsync(
            Arg.Is<string>(selector =>
                selector.StartsWith(
                    "#participant-readiness-outcome-",
                    StringComparison.Ordinal)));
    }

    [Test]
    public async Task RevokedStateOffersDignifiedSupportWithoutDetails()
    {
        var proxy = new ReadinessServiceProxy
        {
            Resource = Resource(
                "revoked",
                "contact_organizer"),
        };
        RenderedReadiness cut = Render(proxy);

        await Assert.That(cut.FindAll(
                "[data-support-code='contact_organizer']"))
            .HasSingleItem();
        await Assert.That(cut.FindAll("button")).IsEmpty();
        await Assert.That(cut.Markup)
            .DoesNotContain("consent");
        await Assert.That(cut.Markup)
            .DoesNotContain("payment");
    }

    private RenderedReadiness Render(
        ReadinessServiceProxy proxy)
    {
        Assembly clientAssembly =
            typeof(HalResourceOfParticipantReadinessDto)
                .Assembly;
        Type? component =
            clientAssembly.GetType(ComponentName);
        Type? service =
            clientAssembly.GetType(ServiceName);
        Assert.That(component).IsNotNull()
            .GetAwaiter().GetResult();
        Assert.That(service).IsNotNull()
            .GetAwaiter().GetResult();
        object serviceProxy =
            proxy.Create(service!);
        _context.Services.AddSingleton(
            service!,
            serviceProxy);
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        Guid participantId = Guid.CreateVersion7();
        Guid assignmentId = Guid.CreateVersion7();
        string capability = Guid.CreateVersion7()
            .ToString("N");
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent(0, component!);
            builder.AddAttribute(1, "EventId", eventId);
            builder.AddAttribute(2, "OrderId", orderId);
            builder.AddAttribute(
                3,
                "ParticipantId",
                participantId);
            builder.AddAttribute(
                4,
                "AssignmentId",
                assignmentId);
            builder.AddAttribute(
                5,
                "GuestCapability",
                capability);
            builder.CloseComponent();
        };
        var rendered =
            _context.Render(fragment);
        return new RenderedReadiness(
            selector => rendered.FindAll(selector).ToArray(),
            selector => rendered.Find(selector),
            () => rendered.Markup,
            action => rendered.InvokeAsync(action));
    }

    private static HalResourceOfParticipantReadinessDto
        Resource(
            string statusCode,
            string supportCode,
            params string[] relations)
    {
        var links = relations.ToDictionary(
            relation => relation,
            relation => new HalLink
            {
                Href = $"/readiness/{relation}",
                Method = "POST",
            });
        return new HalResourceOfParticipantReadinessDto
        {
            RegistrationTicketAssignmentId =
                Guid.CreateVersion7(),
            StatusCode = statusCode,
            SupportCode = supportCode,
            ActiveAdmissionAvailable = false,
            _links = links,
        };
    }

    private sealed class RenderedReadiness(
        Func<string, IReadOnlyList<IElement>> findAll,
        Func<string, IElement> find,
        Func<string> markup,
        Func<Action, Task> invokeAsync)
    {
        public string Markup => markup();

        public IReadOnlyList<IElement> FindAll(
            string selector) =>
            findAll(selector);

        public IElement Find(string selector) =>
            find(selector);

        public Task InvokeAsync(Action action) =>
            invokeAsync(action);
    }

    private class ReadinessServiceProxy :
        DispatchProxy
    {
        public HalResourceOfParticipantReadinessDto?
            Resource { get; set; }

        public TaskCompletionSource<
            HalResourceOfParticipantReadinessDto?>?
            ActionCompletion { get; set; }

        public TaskCompletionSource?
            ActionEntered { get; set; }

        public object Create(Type serviceType)
        {
            object instance = Create(
                serviceType,
                typeof(ReadinessServiceProxy));
            var proxy =
                (ReadinessServiceProxy)instance;
            proxy.Resource = Resource;
            proxy.ActionCompletion = ActionCompletion;
            proxy.ActionEntered = ActionEntered;
            return instance;
        }

        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? args)
        {
            string method = targetMethod?.Name
                ?? throw new InvalidOperationException(
                    "Readiness service method is required.");
            if (method == "GetAsync")
            {
                return Task.FromResult(Resource);
            }
            if (method is
                "CompleteAsync"
                or "ApproveAsync"
                or "RevokeAsync")
            {
                ActionEntered?.TrySetResult();
                return ActionCompletion?.Task
                    ?? Task.FromResult(Resource);
            }

            throw new InvalidOperationException(
                $"Unexpected readiness service method: {method}.");
        }
    }
}
