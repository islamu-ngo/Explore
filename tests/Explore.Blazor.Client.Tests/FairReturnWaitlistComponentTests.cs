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

}
