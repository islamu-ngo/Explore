// ABOUTME: Verifies ordinary setting locks join the canonical manifest mutation fence.
// ABOUTME: Prevents an instance lock from racing a fresh manifest preflight on the same key.

namespace Event.Application.UnitTests.Features.Settings;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Settings.Handlers.Commands;
using Explore.Application.Features.Settings.Requests.Commands;
using Explore.Application.Settings;
using Explore.Domain.Settings;
using Explore.Domain.Settings.Definitions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

public sealed class LockSettingCommandHandlerTests
{
    [Test]
    public async Task UnguardedInstanceLock_UsesCanonicalSettingMutationKey()
    {
        Guid actorId = Guid.CreateVersion7();
        IHierarchicalSettingsResolver resolver =
            Substitute.For<IHierarchicalSettingsResolver>();
        ITenantContext tenantContext = Substitute.For<ITenantContext>();
        ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();
        IAdminContext adminContext = Substitute.For<IAdminContext>();
        IMediator mediator = Substitute.For<IMediator>();
        var mutationLock = new RecordingSettingMutationLock();
        currentUser.UserId.Returns(actorId);
        currentUser.IsAuthenticated.Returns(true);
        adminContext.IsInstanceAdminAsync(
                actorId,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new LockSettingCommandHandler(
            resolver,
            tenantContext,
            currentUser,
            adminContext,
            mediator,
            Substitute.For<ILogger<LockSettingCommandHandler>>(),
            Substitute.For<IPublicationPolicyMutationBoundary>(),
            Substitute.For<IUnitOfWork>(),
            mutationLock);

        var result = await handler.Handle(
            new LockSettingCommand
            {
                Key = PublicExperienceSettingDefinitions.EventCatalogLabel.Key,
                Scope = SettingScope.Instance
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(mutationLock.Keys)
            .IsEquivalentTo(
            [
                PublicExperienceSettingDefinitions.EventCatalogLabel.Key
            ]);
        await resolver.Received(1).LockAsync(
            PublicExperienceSettingDefinitions.EventCatalogLabel.Key,
            SettingScope.Instance,
            Guid.Empty,
            actorId,
            Arg.Any<CancellationToken>());
    }

    private sealed class RecordingSettingMutationLock : ISettingMutationLock
    {
        public IReadOnlyList<string> Keys { get; private set; } = [];

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys = [canonicalSettingKey];
            return operation(cancellationToken);
        }

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys = canonicalSettingKeys.ToArray();
            return operation(cancellationToken);
        }
    }
}
