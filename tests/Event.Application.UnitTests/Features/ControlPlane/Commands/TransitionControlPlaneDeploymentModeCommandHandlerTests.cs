// ABOUTME: Verifies deployment-mode transitions and tenant provisioning share one transaction-scoped lock.
// ABOUTME: Proves tenant-count guards and persisted mode writes occur inside the canonical deployment lock.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.ControlPlane.Handlers.Commands;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ControlPlane.Commands;

public sealed class TransitionControlPlaneDeploymentModeCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenMultipleTenantsAreActive_ChecksAndBlocksInsideCanonicalLock()
    {
        var calls = new List<string>();
        var bootstrap = CreateBootstrap(DeploymentMode.MultiTenant);
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            calls.Add("bootstrap-read");
            return bootstrap;
        });
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetActiveTenantCountAsync().Returns(_ =>
        {
            calls.Add("tenant-count");
            return 2;
        });
        var modeProvider = Substitute.For<IDeploymentModeProvider>();
        var handler = new TransitionControlPlaneDeploymentModeCommandHandler(
            bootstrapRepository,
            tenantRepository,
            CreateCurrentUser(),
            modeProvider,
            new RecordingSettingMutationLock(calls));

        var result = await handler.Handle(
            new TransitionControlPlaneDeploymentModeCommand(
                DeploymentMode.SingleTenant,
                "consolidate instance",
                "SingleTenant"),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(FailureCodes.DeploymentModeChangeBlockedByActiveTenants);
        await Assert.That(calls.SequenceEqual(
            ["lock:deployment.mode:enter", "bootstrap-read", "tenant-count", "lock:deployment.mode:exit"]))
            .IsTrue();
        await bootstrapRepository.DidNotReceive().Update(Arg.Any<InstanceBootstrapState>());
        await modeProvider.DidNotReceive().InvalidateCacheAsync();
    }

    [Test]
    public async Task Handle_WhenTransitionSucceeds_CommitsPersistedModeBeforeCacheInvalidation()
    {
        var calls = new List<string>();
        var bootstrap = CreateBootstrap(DeploymentMode.SingleTenant);
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            calls.Add("bootstrap-read");
            return bootstrap;
        });
        bootstrapRepository.Update(Arg.Do<InstanceBootstrapState>(_ => calls.Add("bootstrap-update")))
            .Returns(Task.CompletedTask);
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetActiveTenantCountAsync().Returns(_ =>
        {
            calls.Add("tenant-count");
            return 1;
        });
        var modeProvider = Substitute.For<IDeploymentModeProvider>();
        modeProvider.InvalidateCacheAsync().Returns(_ =>
        {
            calls.Add("cache-invalidate");
            return Task.CompletedTask;
        });
        var handler = new TransitionControlPlaneDeploymentModeCommandHandler(
            bootstrapRepository,
            tenantRepository,
            CreateCurrentUser(),
            modeProvider,
            new RecordingSettingMutationLock(calls));

        var result = await handler.Handle(
            new TransitionControlPlaneDeploymentModeCommand(
                DeploymentMode.MultiTenant,
                "managed hosting",
                "MultiTenant"),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(bootstrap.SelectedDeploymentMode).IsEqualTo("MultiTenant");
        await Assert.That(calls.SequenceEqual(
            [
                "lock:deployment.mode:enter",
                "bootstrap-read",
                "tenant-count",
                "bootstrap-update",
                "lock:deployment.mode:exit",
                "cache-invalidate"
            ]))
            .IsTrue();
    }

    private static ICurrentUserService CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        currentUser.IsAuthenticated.Returns(true);
        return currentUser;
    }

    private static InstanceBootstrapState CreateBootstrap(DeploymentMode mode) => new()
    {
        Id = Guid.CreateVersion7(),
        IsCompleted = true,
        SelectedDeploymentMode = mode.ToString(),
        CreatedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow
    };

    private sealed class RecordingSettingMutationLock(List<string> calls) : ISettingMutationLock
    {
        public async Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            calls.Add($"lock:{canonicalSettingKey}:enter");
            T result = await operation(cancellationToken);
            calls.Add($"lock:{canonicalSettingKey}:exit");
            return result;
        }

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
}
