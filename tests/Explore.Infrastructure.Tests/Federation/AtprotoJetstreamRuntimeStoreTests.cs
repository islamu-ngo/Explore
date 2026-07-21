// ABOUTME: Verifies scoped Jetstream persistence and recovery dispatch through the Infrastructure host boundary.
// ABOUTME: Ensures only successful discovery mutations evict public ATProto response caches.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Requests.Commands;
using Explore.Infrastructure.Services.Federation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Federation;

public sealed class AtprotoJetstreamRuntimeStoreTests
{
    [Test]
    public async Task ReconcilePdsSnapshotsDispatchesExactCommandAndCancellationThroughFreshAsyncScope()
    {
        IMediator mediator = Substitute.For<IMediator>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        var command = RecoveryCommand();
        using var cancellation = new CancellationTokenSource();
        var expected = new AtprotoPdsRecoveryResult(AtprotoPdsRecoveryOutcome.Unchanged, new string('a', 64));
        mediator.Send(command, cancellation.Token).Returns(expected);
        var scopeProbe = new AsyncScopeProbe();
        (AtprotoJetstreamRuntimeStore Store, ServiceProvider Provider) fixture =
            CreateRecoveryStore(mediator, invalidator, scopeProbe);
        await using ServiceProvider provider = fixture.Provider;

        AtprotoPdsRecoveryResult result = await fixture.Store.ReconcilePdsSnapshotsAsync(
            command,
            cancellation.Token);

        await Assert.That(result).IsEqualTo(expected);
        await mediator.Received(1).Send(
            Arg.Is<ReconcileAtprotoPdsSnapshotsCommand>(actual => ReferenceEquals(actual, command)),
            cancellation.Token);
        await Assert.That(scopeProbe.IsDisposed).IsTrue();
    }

    [Test]
    public async Task CompletedRecoveryInvalidatesDiscoveryCacheOnceAfterMediatorCompletes()
    {
        IMediator mediator = Substitute.For<IMediator>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        var command = RecoveryCommand();
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<AtprotoPdsRecoveryResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        mediator.Send(command, cancellation.Token).Returns(completion.Task);
        (AtprotoJetstreamRuntimeStore Store, ServiceProvider Provider) fixture =
            CreateRecoveryStore(mediator, invalidator);
        await using ServiceProvider provider = fixture.Provider;

        Task<AtprotoPdsRecoveryResult> recovery = fixture.Store.ReconcilePdsSnapshotsAsync(
            command,
            cancellation.Token);
        await invalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
        var expected = new AtprotoPdsRecoveryResult(
            AtprotoPdsRecoveryOutcome.Completed,
            new string('c', 64),
            AppliedDids: 1);
        completion.SetResult(expected);

        AtprotoPdsRecoveryResult result = await recovery;

        await Assert.That(result).IsEqualTo(expected);
        await invalidator.Received(1).InvalidateAsync(cancellation.Token);
    }

    [Test]
    [Arguments(AtprotoPdsRecoveryOutcome.Disabled)]
    [Arguments(AtprotoPdsRecoveryOutcome.DowntimeOnly)]
    [Arguments(AtprotoPdsRecoveryOutcome.ScopeRejected)]
    [Arguments(AtprotoPdsRecoveryOutcome.Unchanged)]
    [Arguments(AtprotoPdsRecoveryOutcome.PartialFailure)]
    [Arguments(AtprotoPdsRecoveryOutcome.FenceRejected)]
    public async Task NonCompletedRecoveryDoesNotInvalidateDiscoveryCache(
        AtprotoPdsRecoveryOutcome outcome)
    {
        IMediator mediator = Substitute.For<IMediator>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        var command = RecoveryCommand();
        var expected = new AtprotoPdsRecoveryResult(outcome, new string('d', 64));
        mediator.Send(command, CancellationToken.None).Returns(expected);
        (AtprotoJetstreamRuntimeStore Store, ServiceProvider Provider) fixture =
            CreateRecoveryStore(mediator, invalidator);
        await using ServiceProvider provider = fixture.Provider;

        AtprotoPdsRecoveryResult result = await fixture.Store.ReconcilePdsSnapshotsAsync(
            command,
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(expected);
        await invalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
    }

    [Test]
    public async Task CompletedRecoveryWithoutRegisteredInvalidatorReturnsResult()
    {
        IMediator mediator = Substitute.For<IMediator>();
        var command = RecoveryCommand();
        var expected = new AtprotoPdsRecoveryResult(
            AtprotoPdsRecoveryOutcome.Completed,
            new string('e', 64));
        mediator.Send(command, CancellationToken.None).Returns(expected);
        (AtprotoJetstreamRuntimeStore Store, ServiceProvider Provider) fixture =
            CreateRecoveryStore(mediator);
        await using ServiceProvider provider = fixture.Provider;

        AtprotoPdsRecoveryResult result = await fixture.Store.ReconcilePdsSnapshotsAsync(
            command,
            CancellationToken.None);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task MediatorFailureDoesNotInvalidateDiscoveryCache()
    {
        IMediator mediator = Substitute.For<IMediator>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        var command = RecoveryCommand();
        var expected = new InvalidOperationException("simulated_mediator_failure");
        mediator.Send(command, CancellationToken.None)
            .Returns(Task.FromException<AtprotoPdsRecoveryResult>(expected));
        (AtprotoJetstreamRuntimeStore Store, ServiceProvider Provider) fixture =
            CreateRecoveryStore(mediator, invalidator);
        await using ServiceProvider provider = fixture.Provider;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Store.ReconcilePdsSnapshotsAsync(command, CancellationToken.None));

        await Assert.That(exception).IsSameReferenceAs(expected);
        await invalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
    }

    [Test]
    public async Task CanceledMediatorSendDoesNotInvalidateDiscoveryCache()
    {
        IMediator mediator = Substitute.For<IMediator>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        var command = RecoveryCommand();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        mediator.Send(command, cancellation.Token)
            .Returns(Task.FromCanceled<AtprotoPdsRecoveryResult>(cancellation.Token));
        (AtprotoJetstreamRuntimeStore Store, ServiceProvider Provider) fixture =
            CreateRecoveryStore(mediator, invalidator);
        await using ServiceProvider provider = fixture.Provider;

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => fixture.Store.ReconcilePdsSnapshotsAsync(command, cancellation.Token));

        await mediator.Received(1).Send(command, cancellation.Token);
        await invalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
    }

    [Test]
    public async Task SuccessfulApplyInvalidatesDiscoveryCacheAfterRepositoryCommit()
    {
        IAtprotoJetstreamRepository repository = Substitute.For<IAtprotoJetstreamRepository>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        repository.TryApplyAndAdvanceAsync(Arg.Any<AtprotoJetstreamApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(true);
        AtprotoJetstreamRuntimeStore store = CreateStore(repository, invalidator);

        bool applied = await store.TryApplyAndAdvanceAsync(Request(affectsDiscovery: true), CancellationToken.None);

        await Assert.That(applied).IsTrue();
        await invalidator.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RejectedApplyDoesNotInvalidateDiscoveryCache()
    {
        IAtprotoJetstreamRepository repository = Substitute.For<IAtprotoJetstreamRepository>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        repository.TryApplyAndAdvanceAsync(Arg.Any<AtprotoJetstreamApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(false);
        AtprotoJetstreamRuntimeStore store = CreateStore(repository, invalidator);

        bool applied = await store.TryApplyAndAdvanceAsync(Request(affectsDiscovery: true), CancellationToken.None);

        await Assert.That(applied).IsFalse();
        await invalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
    }

    [Test]
    public async Task SuccessfulUnrelatedApplyDoesNotInvalidateDiscoveryCache()
    {
        IAtprotoJetstreamRepository repository = Substitute.For<IAtprotoJetstreamRepository>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        repository.TryApplyAndAdvanceAsync(Arg.Any<AtprotoJetstreamApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(true);
        AtprotoJetstreamRuntimeStore store = CreateStore(repository, invalidator);

        bool applied = await store.TryApplyAndAdvanceAsync(Request(affectsDiscovery: false), CancellationToken.None);

        await Assert.That(applied).IsTrue();
        await invalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
    }

    private static AtprotoJetstreamRuntimeStore CreateStore(
        IAtprotoJetstreamRepository repository,
        IAtprotoDiscoveryCacheInvalidator invalidator)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => repository);
        services.AddScoped(_ => invalidator);
        ServiceProvider provider = services.BuildServiceProvider();
        return new AtprotoJetstreamRuntimeStore(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private static (AtprotoJetstreamRuntimeStore Store, ServiceProvider Provider) CreateRecoveryStore(
        IMediator mediator,
        IAtprotoDiscoveryCacheInvalidator? invalidator = null,
        AsyncScopeProbe? scopeProbe = null)
    {
        var services = new ServiceCollection();
        if (invalidator is not null)
        {
            services.AddScoped(_ => invalidator);
        }

        services.AddScoped(_ => scopeProbe ?? new AsyncScopeProbe());
        services.AddScoped<IMediator>(provider =>
        {
            _ = provider.GetRequiredService<AsyncScopeProbe>();
            return mediator;
        });
        ServiceProvider provider = services.BuildServiceProvider();
        return (
            new AtprotoJetstreamRuntimeStore(provider.GetRequiredService<IServiceScopeFactory>()),
            provider);
    }

    private static ReconcileAtprotoPdsSnapshotsCommand RecoveryCommand() =>
        new(
            new AtprotoJetstreamClaim(
                Guid.CreateVersion7(),
                "wss://jetstream.example/subscribe",
                41,
                Guid.CreateVersion7(),
                7),
            ["did:plc:recovery"],
            DateTime.UtcNow,
            new string('b', 64));

    private static AtprotoJetstreamApplyRequest Request(bool affectsDiscovery)
    {
        var claim = new AtprotoJetstreamClaim(
            Guid.CreateVersion7(),
            "wss://jetstream.example/subscribe",
            41,
            Guid.CreateVersion7(),
            7);
        return new AtprotoJetstreamApplyRequest(
            claim,
            41,
            42,
            null,
            [],
            null,
            DateTime.UtcNow,
            EventProjectionInvalidation: affectsDiscovery
                ? new AtprotoEventProjectionInvalidation(
                    "did:plc:discovery",
                    AtprotoJetstreamConstants.EventCollection,
                    "event-record",
                    42)
                : null);
    }

    private sealed class AsyncScopeProbe : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
