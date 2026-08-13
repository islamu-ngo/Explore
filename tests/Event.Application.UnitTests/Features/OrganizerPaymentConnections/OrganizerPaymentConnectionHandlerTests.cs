// ABOUTME: Proves organizer payment connection CQRS never substitutes admin or session recipients.
// ABOUTME: Covers actor ownership, scoped idempotency, replacement, disable, and safe queries.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizerPaymentConnections;
using Explore.Application.Features.OrganizerPaymentConnections;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.OrganizerPaymentConnections;

public sealed class OrganizerPaymentConnectionHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000010");
    private static readonly Guid ActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000020");
    private static readonly DateTime Now = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task RecordConnection_UnauthenticatedCallerIsDenied()
    {
        Harness harness = new(authenticated: false);

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(harness.Repository.Connections).IsEmpty();
    }

    [Test]
    public async Task RecordConnection_ExplicitActorNotControlledByCurrentUserIsDenied()
    {
        Harness harness = new(controlled: false);

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(harness.Repository.Connections).IsEmpty();
    }

    [Test]
    public async Task RecordConnection_TenantIneligibleActorIsDenied()
    {
        Harness harness = new(activeTenantUser: false);

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(harness.Repository.Connections).IsEmpty();
    }

    [Test]
    public async Task RecordConnection_AdminOrSessionUserNeverBecomesRecipient()
    {
        Harness harness = new();

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        OrganizerPaymentProviderConnection created = harness.Repository.Connections.Single();

        await Assert.That(result.Success).IsTrue();
        await Assert.That(created.OrganizerActorId).IsEqualTo(ActorId);
        await Assert.That(created.OrganizerActorId).IsNotEqualTo(UserId);
    }

    [Test]
    public async Task RecordConnection_SameActiveScopeAndExternalAccountReturnsExistingId()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection existing = harness.Repository.AddExisting("acct_1");

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existing.Id);
        await Assert.That(harness.Repository.Connections.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RecordConnection_SameActiveScopeDifferentAccountRequiresReplace()
    {
        Harness harness = new();
        harness.Repository.AddExisting("acct_1");

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_2"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("organizer_payment_connection_replace_required");
        await Assert.That(harness.Repository.Connections.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RecordConnection_ExternalAccountBoundToAnotherActorIsRejected()
    {
        Harness harness = new();
        harness.Repository.AddExisting("acct_1", organizerActorId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000099"));

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("organizer_payment_external_account_bound");
    }

    [Test]
    public async Task ReplaceConnection_IsFutureOnlyAndKeepsOldExternalAccountImmutable()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection current = harness.Repository.AddExisting("acct_old");

        BaseCommandResponse<Guid> result = await harness.ReplaceHandler.Handle(new ReplaceOrganizerPaymentConnectionCommand(TenantId, ActorId, current.Id, "acct_new"), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(current.ExternalAccountId).IsEqualTo("acct_old");
        await Assert.That(current.StatusId).IsEqualTo((int)OrganizerPaymentProviderConnectionStatusEnum.Replaced);
        await Assert.That(harness.Repository.Connections.Single(connection => connection.Id == result.Id).ReplacesConnectionId).IsEqualTo(current.Id);
    }

    [Test]
    public async Task ReplaceConnection_NewExternalAccountAlreadyBoundIsRejectedWithoutMutatingCurrent()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection current = harness.Repository.AddExisting("acct_old");
        harness.Repository.AddExisting("acct_new", organizerActorId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000099"));

        BaseCommandResponse<Guid> result = await harness.ReplaceHandler.Handle(new ReplaceOrganizerPaymentConnectionCommand(TenantId, ActorId, current.Id, "acct_new"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(current.StatusId).IsEqualTo((int)OrganizerPaymentProviderConnectionStatusEnum.PendingOnboarding);
        await Assert.That(current.ExternalAccountId).IsEqualTo("acct_old");
    }

    [Test]
    public async Task RecordConnection_HistoricalDisabledExternalAccountBoundToAnotherActorIsRejected()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection historical = harness.Repository.AddExisting("acct_1", organizerActorId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000099"));
        historical.Disable("operator_disabled", Now.AddMinutes(1));

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("organizer_payment_external_account_bound");
    }

    [Test]
    public async Task RecordConnection_HistoricalReplacedExternalAccountBoundToAnotherActorIsRejected()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection historical = harness.Repository.AddExisting("acct_1", organizerActorId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000099"));
        _ = historical.ReplaceWith(Guid.CreateVersion7(), "acct_other", Now.AddMinutes(1));

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("organizer_payment_external_account_bound");
    }

    [Test]
    public async Task RecordConnection_HistoricalDisabledExternalAccountInSameScopeIsRejected()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection historical = harness.Repository.AddExisting("acct_1");
        historical.Disable("operator_disabled", Now.AddMinutes(1));

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("organizer_payment_external_account_bound");
    }

    [Test]
    public async Task RecordConnection_HistoricalReplacedExternalAccountInSameScopeIsRejected()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection historical = harness.Repository.AddExisting("acct_1");
        _ = historical.ReplaceWith(Guid.CreateVersion7(), "acct_other", Now.AddMinutes(1));

        BaseCommandResponse<Guid> result = await harness.RecordHandler.Handle(harness.RecordCommand("acct_1"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("organizer_payment_external_account_bound");
    }

    [Test]
    public async Task ReplaceConnection_HistoricalDisabledExternalAccountBoundToAnotherActorIsRejected()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection current = harness.Repository.AddExisting("acct_old");
        OrganizerPaymentProviderConnection historical = harness.Repository.AddExisting("acct_new", organizerActorId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000099"));
        historical.Disable("operator_disabled", Now.AddMinutes(1));

        BaseCommandResponse<Guid> result = await harness.ReplaceHandler.Handle(new ReplaceOrganizerPaymentConnectionCommand(TenantId, ActorId, current.Id, "acct_new"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("organizer_payment_external_account_bound");
        await Assert.That(current.StatusId).IsEqualTo((int)OrganizerPaymentProviderConnectionStatusEnum.PendingOnboarding);
    }

    [Test]
    public async Task ReplaceConnection_HistoricalReplacedExternalAccountBoundToAnotherActorIsRejected()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection current = harness.Repository.AddExisting("acct_old");
        OrganizerPaymentProviderConnection historical = harness.Repository.AddExisting("acct_new", organizerActorId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000099"));
        _ = historical.ReplaceWith(Guid.CreateVersion7(), "acct_other", Now.AddMinutes(1));

        BaseCommandResponse<Guid> result = await harness.ReplaceHandler.Handle(new ReplaceOrganizerPaymentConnectionCommand(TenantId, ActorId, current.Id, "acct_new"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("organizer_payment_external_account_bound");
        await Assert.That(current.StatusId).IsEqualTo((int)OrganizerPaymentProviderConnectionStatusEnum.PendingOnboarding);
    }

    [Test]
    public async Task DisableConnection_RequiresOwnershipAndUsesBoundedReasonCode()
    {
        Harness denied = new(controlled: false);
        OrganizerPaymentProviderConnection deniedConnection = denied.Repository.AddExisting("acct_1");
        Harness allowed = new();
        OrganizerPaymentProviderConnection allowedConnection = allowed.Repository.AddExisting("acct_1");

        BaseCommandResponse<Guid> deniedResult = await denied.DisableHandler.Handle(new DisableOrganizerPaymentConnectionCommand(TenantId, ActorId, deniedConnection.Id, "operator_disabled"), CancellationToken.None);
        BaseCommandResponse<Guid> allowedResult = await allowed.DisableHandler.Handle(new DisableOrganizerPaymentConnectionCommand(TenantId, ActorId, allowedConnection.Id, "operator_disabled"), CancellationToken.None);

        await Assert.That(deniedResult.Success).IsFalse();
        await Assert.That(allowedResult.Success).IsTrue();
        await Assert.That(allowedConnection.StatusId).IsEqualTo((int)OrganizerPaymentProviderConnectionStatusEnum.Disabled);
        await Assert.That(allowedConnection.DisabledReasonCode).IsEqualTo("operator_disabled");
    }

    [Test]
    public async Task QueriesAreScopedToExplicitTenantAndActorAndExposeBoundedDtoOnly()
    {
        Harness harness = new();
        OrganizerPaymentProviderConnection owned = harness.Repository.AddExisting("acct_owned");
        harness.Repository.AddExisting("acct_other", organizerActorId: Guid.Parse("018e4e5c-7f00-7000-8000-000000000099"));

        IReadOnlyList<OrganizerPaymentConnectionDto> rows = await harness.ListHandler.Handle(new ListOrganizerPaymentConnectionsQuery(TenantId, ActorId), CancellationToken.None);
        OrganizerPaymentConnectionDto? detail = await harness.GetHandler.Handle(new GetOrganizerPaymentConnectionQuery(TenantId, ActorId, owned.Id), CancellationToken.None);

        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That(rows.Single().ExternalAccountId).IsEqualTo("acct_owned");
        await Assert.That(detail).IsNotNull();
        await Assert.That(detail!.OrganizerActorId).IsEqualTo(ActorId);
        await Assert.That(typeof(OrganizerPaymentConnectionDto).GetProperties().Any(property => property.Name.Contains("Secret", StringComparison.Ordinal))).IsFalse();
        await Assert.That(typeof(OrganizerPaymentConnectionDto).GetProperties().Any(property => property.Name.Contains("Raw", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task Queries_UnauthenticatedCallerReturnsEmptyAndDoesNotHitRepository()
    {
        Harness harness = new(authenticated: false);
        harness.Repository.AddExisting("acct_owned");

        IReadOnlyList<OrganizerPaymentConnectionDto> rows = await harness.ListHandler.Handle(new ListOrganizerPaymentConnectionsQuery(TenantId, ActorId), CancellationToken.None);
        OrganizerPaymentConnectionDto? detail = await harness.GetHandler.Handle(new GetOrganizerPaymentConnectionQuery(TenantId, ActorId, Guid.CreateVersion7()), CancellationToken.None);

        await Assert.That(rows).IsEmpty();
        await Assert.That(detail).IsNull();
        await Assert.That(harness.Repository.ReadCount).IsEqualTo(0);
    }

    [Test]
    public async Task Queries_UncontrolledActorReturnsEmptyAndDoesNotHitRepository()
    {
        Harness harness = new(controlled: false);
        harness.Repository.AddExisting("acct_owned");

        IReadOnlyList<OrganizerPaymentConnectionDto> rows = await harness.ListHandler.Handle(new ListOrganizerPaymentConnectionsQuery(TenantId, ActorId), CancellationToken.None);
        OrganizerPaymentConnectionDto? detail = await harness.GetHandler.Handle(new GetOrganizerPaymentConnectionQuery(TenantId, ActorId, Guid.CreateVersion7()), CancellationToken.None);

        await Assert.That(rows).IsEmpty();
        await Assert.That(detail).IsNull();
        await Assert.That(harness.Repository.ReadCount).IsEqualTo(0);
    }

    private sealed class Harness
    {
        public Harness(bool authenticated = true, bool controlled = true, bool activeTenantUser = true)
        {
            Repository = new FakeOrganizerPaymentConnectionRepository();
            IActorRepository actorRepository = Substitute.For<IActorRepository>();
            actorRepository.GetActorWithDetails(ActorId, Arg.Any<CancellationToken>()).Returns(UserActor());
            ITenantUserRepository tenantUserRepository = Substitute.For<ITenantUserRepository>();
            tenantUserRepository.IsActiveTenantUserAsync(TenantId, UserId, Arg.Any<CancellationToken>()).Returns(activeTenantUser);
            IOrganizationTenantRepository organizationTenantRepository = Substitute.For<IOrganizationTenantRepository>();
            IGroupTenantRepository groupTenantRepository = Substitute.For<IGroupTenantRepository>();
            IOrganizationMemberRepository organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
            IGroupMemberRepository groupMemberRepository = Substitute.For<IGroupMemberRepository>();
            ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();
            currentUser.UserId.Returns(authenticated ? UserId : null);
            currentUser.IsAuthenticated.Returns(authenticated);
            if (!controlled)
            {
                actorRepository.GetActorWithDetails(ActorId, Arg.Any<CancellationToken>()).Returns(UserActor(Guid.Parse("018e4e5c-7f00-7000-8000-000000000088")));
            }

            ITenantContext tenantContext = Substitute.For<ITenantContext>();
            tenantContext.TenantId.Returns(TenantId);
            IUnitOfWork unitOfWork = new InlineSerializableUnitOfWork();
            TimeProvider timeProvider = new FixedTimeProvider(Now);
            RecordHandler = new RecordOrganizerPaymentConnectionCommandHandler(Repository, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, unitOfWork, tenantContext, currentUser, timeProvider);
            ReplaceHandler = new ReplaceOrganizerPaymentConnectionCommandHandler(Repository, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, unitOfWork, tenantContext, currentUser, timeProvider);
            DisableHandler = new DisableOrganizerPaymentConnectionCommandHandler(Repository, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, unitOfWork, tenantContext, currentUser, timeProvider);
            ListHandler = new ListOrganizerPaymentConnectionsQueryHandler(Repository, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, tenantContext, currentUser);
            GetHandler = new GetOrganizerPaymentConnectionQueryHandler(Repository, actorRepository, tenantUserRepository, organizationTenantRepository, groupTenantRepository, organizationMemberRepository, groupMemberRepository, tenantContext, currentUser);
        }

        public FakeOrganizerPaymentConnectionRepository Repository { get; }
        public RecordOrganizerPaymentConnectionCommandHandler RecordHandler { get; }
        public ReplaceOrganizerPaymentConnectionCommandHandler ReplaceHandler { get; }
        public DisableOrganizerPaymentConnectionCommandHandler DisableHandler { get; }
        public ListOrganizerPaymentConnectionsQueryHandler ListHandler { get; }
        public GetOrganizerPaymentConnectionQueryHandler GetHandler { get; }

        public RecordOrganizerPaymentConnectionCommand RecordCommand(string externalAccountId) =>
            new(TenantId, ActorId, "stripe", "platform-live-eu", externalAccountId);
    }

    private sealed class InlineSerializableUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }

    private sealed class FakeOrganizerPaymentConnectionRepository : IOrganizerPaymentProviderConnectionRepository
    {
        public List<OrganizerPaymentProviderConnection> Connections { get; } = [];
        public int ReadCount { get; private set; }

        public OrganizerPaymentProviderConnection AddExisting(string externalAccountId, Guid? organizerActorId = null)
        {
            OrganizerPaymentProviderConnection connection = OrganizerPaymentProviderConnection.Create(
                Guid.CreateVersion7(),
                TenantId,
                organizerActorId ?? ActorId,
                "stripe",
                "platform-live-eu",
                externalAccountId,
                Now);
            Connections.Add(connection);
            return connection;
        }

        public Task<OrganizerPaymentProviderConnection?> GetActiveByScopeAsync(Guid tenantId, Guid organizerActorId, string providerCode, string connectPlatformId, CancellationToken cancellationToken) =>
            Task.FromResult(Connections.SingleOrDefault(connection =>
                connection.TenantId == tenantId
                && connection.OrganizerActorId == organizerActorId
                && connection.ProviderCode == providerCode
                && connection.ConnectPlatformId == connectPlatformId
                && connection.StatusId is not (int)OrganizerPaymentProviderConnectionStatusEnum.Disabled and not (int)OrganizerPaymentProviderConnectionStatusEnum.Replaced));

        public Task<OrganizerPaymentProviderConnection?> GetHistoricalByExternalAccountAsync(string providerCode, string connectPlatformId, string externalAccountId, CancellationToken cancellationToken) =>
            Task.FromResult(Connections.SingleOrDefault(connection =>
                connection.ProviderCode == providerCode
                && connection.ConnectPlatformId == connectPlatformId
                && connection.ExternalAccountId == externalAccountId));

        public Task<OrganizerPaymentProviderConnection?> GetByTenantAndIdForUpdateAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(Connections.SingleOrDefault(connection => connection.TenantId == tenantId && connection.Id == connectionId));
        }

        public Task<IReadOnlyList<OrganizerPaymentProviderConnection>> ListByTenantAndActorAsync(Guid tenantId, Guid organizerActorId, CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult<IReadOnlyList<OrganizerPaymentProviderConnection>>(Connections.Where(connection => connection.TenantId == tenantId && connection.OrganizerActorId == organizerActorId).ToArray());
        }

        public Task CreateAsync(OrganizerPaymentProviderConnection connection, CancellationToken cancellationToken)
        {
            Connections.Add(connection);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static Actor UserActor(Guid? userId = null) => new()
    {
        Id = ActorId,
        UserId = userId ?? UserId,
        ActorTypeId = (int)ActorTypeEnum.User,
        ActorType = null!,
        Pii = new ActorPii { DisplayName = "Organizer" }
    };
}
