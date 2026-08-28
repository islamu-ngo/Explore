// ABOUTME: Defines RED HAL, secret-handling, bounded-state, and accessibility contracts for transfer UI.
// ABOUTME: Pins semantic live regions, deterministic pending actions, localization, focus, and RTL-safe CSS.

using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Admissions;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.Admissions;
using Explore.Blazor.Client.Services.Admissions;
using Explore.Blazor.Client.Services.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Explore.Blazor.Client.Tests;

public sealed class TicketTransferComponentTests :
    IDisposable
{
    private const string ComponentTypeName =
        "Explore.Blazor.Client.Components.Admissions." +
        "TicketTransferPanel";
    private const string ServiceTypeName =
        "Explore.Blazor.Client.Contracts.Services.Admissions." +
        "ITicketTransferService";
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();
    private readonly BlazorTestContext _context = new();

    public void Dispose() => _context.Dispose();

    [Test]
    public async Task TransferComponentAndServiceExist()
    {
        Assembly client =
            typeof(EventApiClient).Assembly;
        Type? component =
            client.GetType(ComponentTypeName);
        Type? service =
            client.GetType(ServiceTypeName);

        await Assert.That(component).IsNotNull();
        await Assert.That(service).IsNotNull();
        await Assert.That(component!
                .GetProperty("EventId"))
            .IsNotNull();
        await Assert.That(component
                .GetProperty("AdmissionTicketId"))
            .IsNotNull();
        await Assert.That(component
                .GetProperty("TransferId"))
            .IsNotNull();
        await Assert.That(component
                .GetProperty("Capability"))
            .IsNotNull();
    }

    [Test]
    public async Task MarkupUsesHalOnlyActionsAndBoundedSupportStates()
    {
        string source = await ReadComponentAsync(
            "TicketTransferPanel.razor");

        await Assert.That(source).Contains(
            "accept-ticket-transfer");
        await Assert.That(source).Contains(
            "cancel-ticket-transfer");
        await Assert.That(source).Contains(
            "correct-ticket-transfer");
        await Assert.That(source).Contains(
            "reissue-transferred-ticket");
        await Assert.That(source).DoesNotContain(
            "IsInRole");
        await Assert.That(source).DoesNotContain(
            "Claims");
        await Assert.That(source).Contains(
            "recipient_action_required");
        await Assert.That(source).Contains(
            "contact_sender");
        await Assert.That(source).DoesNotContain(
            "Consent");
        await Assert.That(source).DoesNotContain(
            "Payment");
    }

    [Test]
    public async Task PendingAndErrorStatesUseExactAccessibleSignals()
    {
        string source = await ReadComponentAsync(
            "TicketTransferPanel.razor");

        await Assert.That(source).Contains(
            "disabled=\"@_isBusy\"");
        await Assert.That(source).Contains(
            "aria-busy=\"@_isBusy");
        await Assert.That(source).Contains(
            "role=\"status\"");
        await Assert.That(source).Contains(
            "role=\"@(_outcomeIsError ? " +
            "\"alert\" : \"status\")\"");
        await Assert.That(source).Contains(
            "aria-live=\"@(_outcomeIsError ? " +
            "\"assertive\" : \"polite\")\"");
        await Assert.That(source).Contains(
            "tabindex=\"-1\"");
        await Assert.That(source).Contains(
            "FocusAsync");
    }

    [Test]
    public async Task OneTimeSecretsRemainLocalAndDiagnosticsAreRedacted()
    {
        string source = await ReadComponentAsync(
            "TicketTransferPanel.razor");
        string service = await ReadServiceAsync();

        await Assert.That(source).Contains(
            "ClaimCapability");
        await Assert.That(source).Contains(
            "Credential");
        await Assert.That(source).DoesNotContain(
            "Console.");
        await Assert.That(source).DoesNotContain(
            "Logger");
        await Assert.That(service).DoesNotContain(
            "QueryHelpers.AddQueryString");
        await Assert.That(service).DoesNotContain(
            "Uri.EscapeDataString");
        await Assert.That(service).Contains(
            "X-Ticket-Transfer-Capability");
    }

    [Test]
    public async Task CopyIsLocalizedAndCssUsesLogicalProperties()
    {
        string source = await ReadComponentAsync(
            "TicketTransferPanel.razor");
        string css = await ReadComponentAsync(
            "TicketTransferPanel.razor.css");

        await Assert.That(source).Contains(
            "ITranslationService");
        await Assert.That(source).Contains(
            "ticket_transfer_");
        await Assert.That(css).Contains(
            "margin-block");
        await Assert.That(css).Contains(
            "padding-inline");
        await Assert.That(css).Contains(
            "border-inline-start");
        await Assert.That(css).DoesNotContain(
            "margin-left");
        await Assert.That(css).DoesNotContain(
            "margin-right");
        await Assert.That(css).DoesNotContain(
            "padding-left");
        await Assert.That(css).DoesNotContain(
            "padding-right");
    }

    [Test]
    public async Task RenderedActionsComeOnlyFromHalRelations()
    {
        ITicketTransferService service =
            Substitute.For<ITicketTransferService>();
        _context.Services.AddSingleton(service);
        HalResourceOfTicketTransferDto resource =
            Resource(
                "offered",
                "recipient_action_required",
                "accept-ticket-transfer");

        var cut = _context.Render<TicketTransferPanel>(
            parameters => parameters
                .Add(
                    value => value.EventId,
                    Guid.CreateVersion7())
                .Add(
                    value =>
                        value.AdmissionTicketId,
                    resource.AdmissionTicketId)
                .Add(
                    value => value.TransferId,
                    resource.Id)
                .Add(
                    value =>
                        value.RecipientParticipantId,
                    Guid.CreateVersion7())
                .Add(
                    value => value.InitialResource,
                    resource));

        await Assert.That(cut.FindAll(
                "button[data-relation=" +
                "'accept-ticket-transfer']"))
            .HasSingleItem();
        await Assert.That(cut.FindAll(
                "button[data-relation=" +
                "'cancel-ticket-transfer']"))
            .IsEmpty();
        await Assert.That(cut.FindAll(
                "[data-support-code=" +
                "'recipient_action_required']"))
            .HasSingleItem();
        await Assert.That(cut.Markup)
            .DoesNotContain(">offered<");
    }

    [Test]
    public async Task ActionDisablesUntilCompletionThenFocusesRenderedError()
    {
        var completion = new TaskCompletionSource<
            TicketTransferCredentialResponse?>(
            TaskCreationOptions
                .RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(
            TaskCreationOptions
                .RunContinuationsAsynchronously);
        ITicketTransferService service =
            Substitute.For<ITicketTransferService>();
        service.AcceptAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                entered.TrySetResult();
                return completion.Task;
            });
        _context.Services.AddSingleton(service);
        IAccessibilityFocusService focus =
            _context.Services.GetRequiredService<
                IAccessibilityFocusService>();
        var focused = new TaskCompletionSource(
            TaskCreationOptions
                .RunContinuationsAsynchronously);
        focus.FocusAsync(
                Arg.Any<string>(),
                Arg.Any<bool>())
            .Returns(_ =>
            {
                focused.TrySetResult();
                return Task.CompletedTask;
            });
        HalResourceOfTicketTransferDto resource =
            Resource(
                "offered",
                "recipient_action_required",
                "accept-ticket-transfer");
        var cut = _context.Render<TicketTransferPanel>(
            parameters => parameters
                .Add(
                    value => value.EventId,
                    Guid.CreateVersion7())
                .Add(
                    value =>
                        value.AdmissionTicketId,
                    resource.AdmissionTicketId)
                .Add(
                    value => value.TransferId,
                    resource.Id)
                .Add(
                    value =>
                        value.RecipientParticipantId,
                    Guid.CreateVersion7())
                .Add(
                    value => value.Capability,
                    Guid.CreateVersion7()
                        .ToString("N"))
                .Add(
                    value => value.InitialResource,
                    resource));
        IElement button = cut.Find(
            "button[data-relation=" +
            "'accept-ticket-transfer']");

        Task click =
            cut.InvokeAsync(() => button.Click());
        await entered.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        await Assert.That(cut.Find(
                "button[data-relation=" +
                "'accept-ticket-transfer']")
                .HasAttribute("disabled"))
            .IsTrue();
        completion.SetResult(null);
        await focused.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        await click;

        IElement outcome = cut.Find(
            "[id^='ticket-transfer-outcome-']");
        await Assert.That(
                outcome.GetAttribute("role"))
            .IsEqualTo("alert");
        await Assert.That(
                outcome.GetAttribute("aria-live"))
            .IsEqualTo("assertive");
        await Assert.That(
                outcome.GetAttribute("tabindex"))
            .IsEqualTo("-1");
        await focus.Received(1).FocusAsync(
            Arg.Is<string>(value =>
                value.StartsWith(
                    "#ticket-transfer-outcome-",
                    StringComparison.Ordinal)));
    }

    [Test]
    public async Task OneTimeClaimRendersOnlyAfterSuccessfulOffer()
    {
        string claim =
            Guid.CreateVersion7().ToString("N");
        ITicketTransferService service =
            Substitute.For<ITicketTransferService>();
        HalResourceOfTicketTransferDto resource =
            Resource(
                "offered",
                "recipient_action_required",
                "cancel-ticket-transfer");
        service.OfferAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(new TicketTransferOfferResponse
            {
                Transfer = resource,
                ClaimCapability = claim,
            });
        _context.Services.AddSingleton(service);
        var links = new Dictionary<string, HalLink>
        {
            ["offer-ticket-transfer"] =
                new()
                {
                    Href = "/transfer-offer",
                    Method = HttpMethod.Post.Method,
                },
        };
        var cut = _context.Render<TicketTransferPanel>(
            parameters => parameters
                .Add(
                    value => value.EventId,
                    Guid.CreateVersion7())
                .Add(
                    value =>
                        value.AdmissionTicketId,
                    resource.AdmissionTicketId)
                .Add(
                    value => value.TicketLinks,
                    links));

        await cut.InvokeAsync(() => cut.Find(
                "button[data-relation=" +
                "'offer-ticket-transfer']")
            .Click());

        await Assert.That(cut.FindAll(
                ".ticket-transfer__secret output"))
            .HasSingleItem();
        await Assert.That(cut.Find(
                ".ticket-transfer__secret output")
                .TextContent)
            .IsEqualTo(claim);
        await Assert.That(cut.Markup)
            .Contains("shown only now");
    }

    [Test]
    public async Task ServiceFailsClosedButPropagatesCancellation()
    {
        IBffClient bff =
            Substitute.For<IBffClient>();
        bff.GetWithTicketTransferCapabilityAsync<
                HalResourceOfTicketTransferDto>(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<
                HalResourceOfTicketTransferDto?>(
                    new HttpRequestException()));
        var service =
            new TicketTransferService(bff);

        HalResourceOfTicketTransferDto? failed =
            await service.GetAsync(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7()
                    .ToString("N"),
                CancellationToken.None);

        await Assert.That(failed).IsNull();

        using var cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();
        bff.GetWithTicketTransferCapabilityAsync<
                HalResourceOfTicketTransferDto>(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                cancellation.Token)
            .Returns(Task.FromCanceled<
                HalResourceOfTicketTransferDto?>(
                    cancellation.Token));

        await Assert.ThrowsAsync<
            OperationCanceledException>(async () =>
            await service.GetAsync(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                null,
                cancellation.Token));
    }

    private static Task<string> ReadComponentAsync(
        string fileName) =>
        ReadExpectedFileAsync(Path.Combine(
            RepositoryRoot,
            "src",
            "Explore.Blazor.Client",
            "Components",
            "Admissions",
            fileName));

    private static Task<string> ReadServiceAsync() =>
        ReadExpectedFileAsync(Path.Combine(
            RepositoryRoot,
            "src",
            "Explore.Blazor.Client",
            "Services",
            "Admissions",
            "TicketTransferService.cs"));

    private static Task<string> ReadExpectedFileAsync(
        string path) =>
        File.Exists(path)
            ? File.ReadAllTextAsync(path)
            : Task.FromResult(string.Empty);

    private static HalResourceOfTicketTransferDto
        Resource(
            string statusCode,
            string supportCode,
            params string[] relations) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            AdmissionTicketId =
                Guid.CreateVersion7(),
            StatusCode = statusCode,
            SupportCode = supportCode,
            TransferHop = 1,
            ExpiresAt = DateTimeOffset.UtcNow
                .AddHours(1),
            CredentialGeneration = 1,
            _links = relations.ToDictionary(
                relation => relation,
                relation => new HalLink
                {
                    Href = $"/transfer/{relation}",
                    Method =
                        HttpMethod.Post.Method,
                }),
        };

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(
            AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "Explore.slnx")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root was not found.");
    }
}
