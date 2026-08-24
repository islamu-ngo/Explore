// ABOUTME: Tests authorized evidence-based resolution of parked IntegrationSync provider outcomes.
// ABOUTME: Verifies tenant fencing, validation, and refusal before repository mutation.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Integrations;
using Explore.Application.Features.Integrations.Listmonk.Handlers.Commands;
using Explore.Application.Features.Integrations.Listmonk.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Integrations;

public sealed class ResolveIntegrationSyncAmbiguityCommandHandlerTests
{
    [Test]
    public async Task AuthorizedTenantAdminResolvesOnlyTheCurrentTenantWithEvidence()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        Guid outboxId = Guid.CreateVersion7();
        var repository = Substitute.For<IIntegrationSyncOutboxRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserService>();
        tenantContext.TenantId.Returns(tenantId);
        currentUser.UserId.Returns(actorId);
        adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(true);
        repository.ResolveAmbiguousAsync(Arg.Any<IntegrationSyncRecoveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new IntegrationSyncOutbox { Id = outboxId, TenantId = tenantId });
        var handler = new ResolveIntegrationSyncAmbiguityCommandHandler(
            repository,
            adminContext,
            tenantContext,
            currentUser,
            TimeProvider.System);

        var result = await handler.Handle(
            new ResolveIntegrationSyncAmbiguityCommand(
                outboxId,
                new ResolveIntegrationSyncAmbiguityDto
                {
                    Decision = IntegrationSyncRecoveryDecision.ConfirmAccepted,
                    EvidenceReference = " incident-42 "
                }),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await repository.Received(1).ResolveAmbiguousAsync(
            Arg.Is<IntegrationSyncRecoveryRequest>(request =>
                request.TenantId == tenantId &&
                request.OutboxId == outboxId &&
                request.ActorId == actorId &&
                request.EvidenceReference == "incident-42"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NonAdminCannotResolveAmbiguousWork()
    {
        var repository = Substitute.For<IIntegrationSyncOutboxRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserService>();
        tenantContext.TenantId.Returns(Guid.CreateVersion7());
        currentUser.UserId.Returns(Guid.CreateVersion7());
        adminContext.IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new ResolveIntegrationSyncAmbiguityCommandHandler(
            repository,
            adminContext,
            tenantContext,
            currentUser,
            TimeProvider.System);

        var result = await handler.Handle(
            new ResolveIntegrationSyncAmbiguityCommand(
                Guid.CreateVersion7(),
                new ResolveIntegrationSyncAmbiguityDto
                {
                    Decision = IntegrationSyncRecoveryDecision.DeadLetter,
                    EvidenceReference = "incident-43"
                }),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await repository.DidNotReceive().ResolveAmbiguousAsync(
            Arg.Any<IntegrationSyncRecoveryRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvalidRecoveryRequestIsRejectedBeforeAuthorizationAndPersistence()
    {
        var repository = Substitute.For<IIntegrationSyncOutboxRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var handler = new ResolveIntegrationSyncAmbiguityCommandHandler(
            repository,
            adminContext,
            Substitute.For<ITenantContext>(),
            Substitute.For<ICurrentUserService>(),
            TimeProvider.System);

        var result = await handler.Handle(
            new ResolveIntegrationSyncAmbiguityCommand(
                Guid.Empty,
                new ResolveIntegrationSyncAmbiguityDto
                {
                    Decision = (IntegrationSyncRecoveryDecision)999,
                    EvidenceReference = ""
                }),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await adminContext.DidNotReceive().IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().ResolveAmbiguousAsync(
            Arg.Any<IntegrationSyncRecoveryRequest>(),
            Arg.Any<CancellationToken>());
    }
}
