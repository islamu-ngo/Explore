// ABOUTME: Defines RED HAL, waitlist-state, accessibility, localization, and RTL contracts for fair return UI.
// ABOUTME: Requires link-driven actions, bounded position output, deterministic focus, and semantic live status.

using System.Reflection;
using Explore.Blazor.Client.Components.Waitlist;

namespace Explore.Blazor.Client.Tests;

public sealed class FairReturnWaitlistComponentTests
{
    private const string ComponentTypeName =
        "Explore.Blazor.Client.Components.Waitlist." +
        "FairReturnWaitlistPanel";
    private const string ServiceTypeName =
        "Explore.Blazor.Client.Contracts.Services." +
        "Waitlist.IFairReturnWaitlistService";
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();
    private static readonly string ComponentPath =
        Path.Combine(
            RepositoryRoot,
            "src",
            "Explore.Blazor.Client",
            "Components",
            "Waitlist",
            "FairReturnWaitlistPanel.razor");
    private static readonly string CssPath =
        ComponentPath + ".css";

    [Test]
    public async Task ComponentAndServiceAreExplicitlyTyped()
    {
        Type? component = typeof(
                Explore.Blazor.Client.Clients
                    .EventApiClient)
            .Assembly.GetType(ComponentTypeName);
        Type? service = typeof(
                Explore.Blazor.Client.Clients
                    .EventApiClient)
            .Assembly.GetType(ServiceTypeName);

        await Assert.That(component).IsNotNull();
        await Assert.That(service).IsNotNull();
        await Assert.That(component!
                .GetProperty(
                    "EventId",
                    BindingFlags.Public
                    | BindingFlags.Instance))
            .IsNotNull();
        await Assert.That(component.GetProperty(
                "RegistrationOrderId",
                BindingFlags.Public
                | BindingFlags.Instance))
            .IsNotNull();
        await Assert.That(component.GetProperty(
                "RegistrationOrderLineId",
                BindingFlags.Public
                | BindingFlags.Instance))
            .IsNotNull();
    }

    [Test]
    public async Task ActionsAreGatedOnlyByHalRelations()
    {
        await Assert.That(
                File.Exists(ComponentPath))
            .IsTrue();
        string source =
            await File.ReadAllTextAsync(
                ComponentPath);
        foreach (string relation in new[]
                 {
                     "join-fair-return-waitlist",
                     "leave-fair-return-waitlist",
                     "accept-fair-return-offer",
                     "withdraw-fair-return-supply",
                 })
        {
            await Assert.That(source)
                .Contains(relation);
        }
        foreach (string forbidden in new[]
                 {
                     ".CanJoin",
                     ".CanLeave",
                     ".CanAcceptOffer",
                     ".CanWithdrawSupply",
                     "ClaimsPrincipal",
                     "IsInRole",
                 })
        {
            await Assert.That(source)
                .DoesNotContain(forbidden);
        }
    }

    [Test]
    public async Task PositionAndConflictStatesStayBoundedAndPrivate()
    {
        string source =
            await File.ReadAllTextAsync(
                ComponentPath);
        await Assert.That(source).Contains(
            "PositionUnavailable");
        await Assert.That(source).Contains(
            "ReasonCode");
        await Assert.That(source).Contains(
            "StatusCode");
        foreach (string forbidden in new[]
                 {
                     "Email",
                     "Phone",
                     "Participant",
                     "Seller",
                     "PaymentInstrument",
                     "ProviderPayload",
                     "Priority",
                     "Amount",
                     "Currency",
                 })
        {
            await Assert.That(source)
                .DoesNotContain(forbidden);
        }
    }

    [Test]
    public async Task BusyAndOutcomeStateAreAnnouncedAndFocused()
    {
        string source =
            await File.ReadAllTextAsync(
                ComponentPath);
        await Assert.That(source).Contains(
            "aria-busy");
        await Assert.That(source).Contains(
            "role=\"status\"");
        await Assert.That(source).Contains(
            "aria-live=\"polite\"");
        await Assert.That(source).Contains(
            "IAccessibilityFocusService");
        await Assert.That(source).Contains(
            "FocusAsync");
        await Assert.That(source).Contains(
            "CancellationTokenSource");
    }

    [Test]
    public async Task CopyUsesLocalizationKeysAndCssIsRtlSafe()
    {
        string source =
            await File.ReadAllTextAsync(
                ComponentPath);
        await Assert.That(source).Contains(
            "ITranslationService");
        await Assert.That(source).Contains(
            "waitlist_");
        await Assert.That(
                File.Exists(CssPath))
            .IsTrue();
        string css =
            await File.ReadAllTextAsync(CssPath);
        await Assert.That(css).Contains(
            "margin-inline");
        await Assert.That(css).Contains(
            "padding-inline");
        await Assert.That(css)
            .DoesNotContain("margin-left");
        await Assert.That(css)
            .DoesNotContain("margin-right");
        await Assert.That(css)
            .DoesNotContain("padding-left");
        await Assert.That(css)
            .DoesNotContain("padding-right");
    }

    [Test]
    public async Task AmbiguousRetryReusesOperationUntilCompletion()
    {
        var lease =
            new WaitlistMutationOperationLease();

        Guid first = lease.Acquire(
            "event:order:line:join:");
        Guid replay = lease.Acquire(
            "event:order:line:join:");
        await Assert.That(replay).IsEqualTo(first);
        await Assert.That(first.Version)
            .IsEqualTo(7);

        Guid different = lease.Acquire(
            "event:order:line:leave:");
        await Assert.That(different)
            .IsNotEqualTo(first);
        lease.Complete("event:order:line:join:");
        await Assert.That(lease.Acquire(
                "event:order:line:leave:"))
            .IsEqualTo(different);

        lease.Complete(
            "event:order:line:leave:");
        await Assert.That(lease.Acquire(
                "event:order:line:leave:"))
            .IsNotEqualTo(different);
    }

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
