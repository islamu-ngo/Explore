// ABOUTME: Tests the linked-account-only ATProto bootstrap transaction and post-commit JWT issuance.
// ABOUTME: Proves verification failures write nothing and post-commit issuance failures are safely retryable.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Authentication.Atproto.Handlers.Commands;
using Explore.Application.Features.Authentication.Atproto.Models;
using Explore.Application.Features.Authentication.Atproto.Requests.Commands;
using Explore.Application.Features.Authentication.Atproto.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Authentication.Atproto;

public sealed class BootstrapAtprotoSessionCommandHandlerTests
{
    private const string Did = "did:plc:linked-user";
    private static readonly Uri PdsUri = new("https://pds.example/");
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IAtprotoOAuthSecurityGateway _securityGateway = Substitute.For<IAtprotoOAuthSecurityGateway>();
    private readonly IAtprotoSessionTokenIssuer _tokenIssuer = Substitute.For<IAtprotoSessionTokenIssuer>();
    private readonly IUserExternalLoginRepository _externalLogins = Substitute.For<IUserExternalLoginRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IActorRepository _actors = Substitute.For<IActorRepository>();
    private readonly IActorTypeRepository _actorTypes = Substitute.For<IActorTypeRepository>();
    private readonly IAtprotoIdentityRepository _atprotoIdentities = Substitute.For<IAtprotoIdentityRepository>();
    private readonly IActorReferenceConsolidationRepository _references = Substitute.For<IActorReferenceConsolidationRepository>();
    private readonly IGenericRepository<ActorMerge, Guid> _merges = Substitute.For<IGenericRepository<ActorMerge, Guid>>();
    private readonly ITenantUserRepository _tenantUsers = Substitute.For<ITenantUserRepository>();
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrants = Substitute.For<ITenantUserRoleGrantRepository>();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationTenantRepository _organizationTenants = Substitute.For<IOrganizationTenantRepository>();
    private readonly IOrganizationMemberRepository _organizationMembers = Substitute.For<IOrganizationMemberRepository>();
    private readonly IGroupRepository _groups = Substitute.For<IGroupRepository>();
    private readonly IGroupTenantRepository _groupTenants = Substitute.For<IGroupTenantRepository>();
    private readonly IGroupMemberRepository _groupMembers = Substitute.For<IGroupMemberRepository>();
    private readonly IAdminCacheInvalidator _adminCacheInvalidator = Substitute.For<IAdminCacheInvalidator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private TenantUser? _storedTenantUser;
    private OrganizationTenant? _storedOrganizationTenant;
    private GroupTenant? _storedGroupTenant;
    private bool _organizationMemberExists;
    private bool _groupMemberExists;

    public BootstrapAtprotoSessionCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _unitOfWork
            .ExecuteSerializableAsync<AtprotoSubjectOnboardingResult>(
                Arg.Any<Func<CancellationToken, Task<AtprotoSubjectOnboardingResult>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<AtprotoSubjectOnboardingResult>>>()(
                call.Arg<CancellationToken>()));

        _tenantUsers.GetByTenantAndUserAsync(_tenantId, _userId, Arg.Any<CancellationToken>())
            .Returns(_ => _storedTenantUser);
        _tenantUsers.Create(Arg.Any<TenantUser>()).Returns(call =>
        {
            _storedTenantUser = WithId(call.Arg<TenantUser>());
            return _storedTenantUser;
        });
        _organizations.Create(Arg.Any<Organization>()).Returns(call => WithId(call.Arg<Organization>()));
        _groups.Create(Arg.Any<Group>()).Returns(call => WithId(call.Arg<Group>()));
        _actors.Create(Arg.Any<Actor>()).Returns(call => WithId(call.Arg<Actor>()));
        _actorTypes.GetById((int)ActorTypeEnum.Organization).Returns(new ActorType { Id = (int)ActorTypeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization" });
        _actorTypes.GetById((int)ActorTypeEnum.Group).Returns(new ActorType { Id = (int)ActorTypeEnum.Group, MasterCode = "GROUP", FullName = "Group" });
        _organizationTenants.GetByOrganizationAndTenant(
                Arg.Any<Guid>(),
                _tenantId,
                Arg.Any<CancellationToken>())
            .Returns(_ => _storedOrganizationTenant);
        _organizationTenants.Create(Arg.Any<OrganizationTenant>()).Returns(call =>
        {
            _storedOrganizationTenant = WithId(call.Arg<OrganizationTenant>());
            return _storedOrganizationTenant;
        });
        _groupTenants.GetByGroupAndTenant(Arg.Any<Guid>(), _tenantId, Arg.Any<CancellationToken>())
            .Returns(_ => _storedGroupTenant);
        _groupTenants.Create(Arg.Any<GroupTenant>()).Returns(call =>
        {
            _storedGroupTenant = WithId(call.Arg<GroupTenant>());
            return _storedGroupTenant;
        });
        _organizationMembers.Exists(Arg.Any<Guid>(), _userId).Returns(_ => _organizationMemberExists);
        _organizationMembers.Create(Arg.Any<OrganizationMember>()).Returns(call =>
        {
            _organizationMemberExists = true;
            return WithId(call.Arg<OrganizationMember>());
        });
        _groupMembers.Exists(Arg.Any<Guid>(), _userId).Returns(_ => _groupMemberExists);
        _groupMembers.Create(Arg.Any<GroupMember>()).Returns(call =>
        {
            _groupMemberExists = true;
            return WithId(call.Arg<GroupMember>());
        });
    }

    [Test]
    public async Task VerificationMismatchPerformsZeroLocalWrites()
    {
        _securityGateway.VerifyAsync(Arg.Any<AtprotoOAuthVerificationInput>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoOAuthVerificationResult.Failed("session_binding_mismatch"));

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("session_binding_mismatch");
        await _externalLogins.DidNotReceiveWithAnyArgs().GetByProviderAndKey(default!, default!);
        await _actors.DidNotReceiveWithAnyArgs().Update(default!);
        await _atprotoIdentities.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistPreparedAsync(default!, default);
        await _tokenIssuer.DidNotReceiveWithAnyArgs().IssueAsync(default, default, default!, default);
    }

    [Test]
    public async Task MissingLinkedLoginPerformsZeroIdentityOrSessionWrites()
    {
        ConfigureVerifiedSession();
        _externalLogins.GetByProviderAndKey("atproto", Did).Returns((UserExternalLogin?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("account_not_linked");
        await _users.DidNotReceiveWithAnyArgs().GetById(default);
        await _actors.DidNotReceiveWithAnyArgs().Update(default!);
        await _atprotoIdentities.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistPreparedAsync(default!, default);
        await _tokenIssuer.DidNotReceiveWithAnyArgs().IssueAsync(default, default, default!, default);
    }

    [Test]
    public async Task ConflictingTenantUserActorPerformsZeroIdentityOrSessionWrites()
    {
        ConfigureVerifiedSession();
        var linked = ConfigureLinkedIdentity();
        _storedTenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Tenant = null!,
            UserId = _userId,
            User = linked.User,
            ActorId = Guid.NewGuid()
        };

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("linked_identity_incomplete");
        await _atprotoIdentities.DidNotReceiveWithAnyArgs().Create(default!);
        await _atprotoIdentities.DidNotReceiveWithAnyArgs().Update(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistPreparedAsync(default!, default);
        await _tokenIssuer.DidNotReceiveWithAnyArgs().IssueAsync(default, default, default!, default);
    }

    [Test]
    public async Task LinkedLoginRemovedBeforeTransactionPerformsZeroWrites()
    {
        ConfigureVerifiedSession();
        var identity = ConfigureLinkedIdentity();
        _externalLogins.GetByProviderAndKey("atproto", Did)
            .Returns(identity.Login, (UserExternalLogin?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("account_not_linked");
        await _actors.DidNotReceiveWithAnyArgs().Update(default!);
        await _atprotoIdentities.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistPreparedAsync(default!, default);
        await _tokenIssuer.DidNotReceiveWithAnyArgs().IssueAsync(default, default, default!, default);
    }

    [Test]
    public async Task SuccessfulBootstrapCommitsIdentityIndexAndSessionBeforeIssuingPlatformToken()
    {
        ConfigureVerifiedSession();
        var linked = ConfigureLinkedIdentity();
        AtprotoIdentity? createdIdentity = null;
        _atprotoIdentities.GetByDid(Did, Arg.Any<CancellationToken>()).Returns(_ => createdIdentity);
        _atprotoIdentities.Create(Arg.Any<AtprotoIdentity>()).Returns(call =>
        {
            createdIdentity = call.Arg<AtprotoIdentity>();
            return createdIdentity;
        });
        var issued = new AtprotoIssuedSessionToken("platform-jwt", DateTimeOffset.UtcNow.AddMinutes(15));
        _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>()).Returns(issued);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.UserId).IsEqualTo(_userId);
        await Assert.That(result.ActorId).IsEqualTo(linked.Actor.Id);
        await Assert.That(result.ParticipationId).IsEqualTo(_storedTenantUser!.Id);
        await Assert.That(result.Classification).IsEqualTo(AtprotoSubjectClassification.Person);
        await Assert.That(result.Token).IsEqualTo("platform-jwt");
        await Assert.That(createdIdentity).IsNotNull();
        await Assert.That(createdIdentity!.Did).IsEqualTo(Did);
        await Assert.That(createdIdentity.Handle).IsEqualTo("linked.example");
        await Assert.That(createdIdentity.ActorId).IsEqualTo(linked.Actor.Id);
        await _securityGateway.Received(1).PreparePersistenceAsync(
            Arg.Is<AtprotoVerifiedOAuthSession>(session => session.Did == Did),
            _tenantId,
            _userId,
            Arg.Any<CancellationToken>());
        await _securityGateway.Received(1).PersistPreparedAsync(
            Arg.Is<AtprotoPreparedOAuthSession>(session => session.SubjectDid == Did),
            Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _securityGateway.PreparePersistenceAsync(Arg.Any<AtprotoVerifiedOAuthSession>(), _tenantId, _userId, Arg.Any<CancellationToken>());
            _securityGateway.PersistPreparedAsync(Arg.Any<AtprotoPreparedOAuthSession>(), Arg.Any<CancellationToken>());
            _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task OrganizationClassificationCreatesOneGlobalSubjectAndReplaysCurrentTenantParticipation()
    {
        ConfigureVerifiedSession();
        var linked = ConfigureLinkedIdentity();
        linked.Login.TenantId = Guid.NewGuid();
        AtprotoIdentity? storedIdentity = null;
        _atprotoIdentities.GetByDid(Did, Arg.Any<CancellationToken>()).Returns(_ => storedIdentity);
        _atprotoIdentities.Create(Arg.Any<AtprotoIdentity>()).Returns(call =>
        {
            storedIdentity = call.Arg<AtprotoIdentity>();
            return storedIdentity;
        });
        _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>())
            .Returns(new AtprotoIssuedSessionToken("organization-jwt", DateTimeOffset.UtcNow.AddMinutes(15)));

        var first = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization),
            CancellationToken.None);
        var replay = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization),
            CancellationToken.None);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(replay.Success).IsTrue();
        await Assert.That(first.ActorId).IsEqualTo(storedIdentity!.ActorId);
        await Assert.That(first.ParticipationId).IsEqualTo(_storedOrganizationTenant!.Id);
        await Assert.That(first.Classification).IsEqualTo(AtprotoSubjectClassification.Organization);
        await _organizations.Received(1).Create(Arg.Any<Organization>());
        await _actors.Received(1).Create(Arg.Is<Actor>(candidate =>
            candidate.ActorTypeId == (int)ActorTypeEnum.Organization));
        await _organizationTenants.Received(1).Create(Arg.Any<OrganizationTenant>());
        await _organizationMembers.Received(1).Create(Arg.Is<OrganizationMember>(member =>
            member.UserId == _userId && member.RoleId == (int)RoleEnum.OrgAdmin));
        await _tenantUsers.Received(1).Create(Arg.Any<TenantUser>());
    }

    [Test]
    public async Task GroupClassificationCreatesOneGlobalSubjectAndFounderMembership()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        AtprotoIdentity? storedIdentity = null;
        _atprotoIdentities.GetByDid(Did, Arg.Any<CancellationToken>()).Returns(_ => storedIdentity);
        _atprotoIdentities.Create(Arg.Any<AtprotoIdentity>()).Returns(call =>
        {
            storedIdentity = call.Arg<AtprotoIdentity>();
            return storedIdentity;
        });
        _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>())
            .Returns(new AtprotoIssuedSessionToken("group-jwt", DateTimeOffset.UtcNow.AddMinutes(15)));

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Group),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.ActorId).IsEqualTo(storedIdentity!.ActorId);
        await Assert.That(result.ParticipationId).IsEqualTo(_storedGroupTenant!.Id);
        await Assert.That(result.Classification).IsEqualTo(AtprotoSubjectClassification.Group);
        await _groups.Received(1).Create(Arg.Any<Group>());
        await _groupTenants.Received(1).Create(Arg.Any<GroupTenant>());
        await _groupMembers.Received(1).Create(Arg.Is<GroupMember>(member =>
            member.UserId == _userId && member.RoleId == (int)RoleEnum.GroupAdmin));
    }

    [Test]
    public async Task ExistingDifferentKindIdentityFailsBeforeParticipationOrSessionWrites()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        var group = new Group { Id = Guid.NewGuid(), FullName = "Existing group" };
        var groupActor = new Actor
        {
            Id = Guid.NewGuid(),
            ActorTypeId = (int)ActorTypeEnum.Group,
            ActorType = null!,
            GroupId = group.Id,
            Group = group,
            Pii = new ActorPii { DisplayName = group.FullName }
        };
        _atprotoIdentities.GetByDid(Did, Arg.Any<CancellationToken>()).Returns(new AtprotoIdentity
        {
            Did = Did,
            ActorId = groupActor.Id,
            Actor = groupActor,
            PdsHost = PdsUri.AbsoluteUri
        });

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("classification_conflict");
        await _tenantUsers.DidNotReceiveWithAnyArgs().Create(default!);
        await _organizations.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistPreparedAsync(default!, default);
        await _tokenIssuer.DidNotReceiveWithAnyArgs().IssueAsync(default, default, default!, default);
    }

    [Test]
    public async Task ExternalOrganizationPromotionPreservesActorAndIdentityIds()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        var identity = ConfigureExternalIdentity();
        ConfigureIssuedToken();

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.ActorId).IsEqualTo(identity.ActorId);
        await Assert.That(identity.ActorId).IsEqualTo(identity.Actor.Id);
        await Assert.That(identity.Actor.ActorTypeId).IsEqualTo((int)ActorTypeEnum.Organization);
        await Assert.That(identity.Actor.OrganizationId).IsNotNull();
        await Assert.That(identity.Actor.ExternalActorSubjectId).IsNull();
        await _actors.Received(1).Update(Arg.Is<Actor>(actor => actor.Id == identity.ActorId));
        await _references.DidNotReceiveWithAnyArgs().MoveMutableReferencesAsync(default, default, default, default);
    }

    [Test]
    public async Task ExternalGroupPromotionPreservesActorAndIdentityIds()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        var identity = ConfigureExternalIdentity();
        ConfigureIssuedToken();

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Group),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.ActorId).IsEqualTo(identity.ActorId);
        await Assert.That(identity.Actor.ActorTypeId).IsEqualTo((int)ActorTypeEnum.Group);
        await Assert.That(identity.Actor.GroupId).IsNotNull();
        await Assert.That(identity.Actor.ExternalActorSubjectId).IsNull();
    }

    [Test]
    public async Task AuthorizedSameKindConsolidationMovesReferencesAndRecordsBoundedEvidence()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        var identity = ConfigureExternalIdentity();
        var canonical = ConfigureCanonicalOrganizationAuthority();
        _references.MoveMutableReferencesAsync(identity.ActorId, canonical.Id, (int)ActorTypeEnum.Organization, Arg.Any<CancellationToken>())
            .Returns(true);
        ConfigureIssuedToken();

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization, canonical.Id, canonical.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.ActorId).IsEqualTo(canonical.Id);
        await Assert.That(identity.ActorId).IsEqualTo(canonical.Id);
        await Assert.That(identity.Actor.Id).IsEqualTo(canonical.Id);
        await Assert.That(identity.Actor.IsDeleted).IsFalse();
        await _references.Received(1).MoveMutableReferencesAsync(
            Arg.Any<Guid>(),
            canonical.Id,
            (int)ActorTypeEnum.Organization,
            Arg.Any<CancellationToken>());
        await _merges.Received(1).Create(Arg.Is<ActorMerge>(merge =>
            merge.CanonicalActorId == canonical.Id
            && merge.EvidenceReference.StartsWith($"atproto-identity:{identity.Id:D};did-sha256:", StringComparison.Ordinal)
            && merge.EvidenceReference.Length < 256));
    }

    [Test]
    public async Task ConsolidationWithoutCurrentTenantAdminAuthorityFailsBeforeMutation()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        var identity = ConfigureExternalIdentity();
        var canonical = CreateCanonicalOrganizationActor();
        _actors.GetById(canonical.Id).Returns(canonical);

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization, canonical.Id, canonical.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("classification_conflict");
        await _references.DidNotReceiveWithAnyArgs().MoveMutableReferencesAsync(default, default, default, default);
        await _merges.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistPreparedAsync(default!, default);
    }

    [Test]
    public async Task ConsolidationWithStaleCanonicalStampFailsBeforeMutation()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        ConfigureExternalIdentity();
        var canonical = ConfigureCanonicalOrganizationAuthority();

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization, canonical.Id, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("classification_conflict");
        await _references.DidNotReceiveWithAnyArgs().MoveMutableReferencesAsync(default, default, default, default);
    }

    [Test]
    public async Task CompletedConsolidationReplayUsesMergeEvidenceWithoutMovingReferencesAgain()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        var canonical = ConfigureCanonicalOrganizationAuthority();
        var identity = new AtprotoIdentity
        {
            Id = Guid.NewGuid(),
            Did = Did,
            ActorId = canonical.Id,
            Actor = canonical,
            PdsHost = PdsUri.AbsoluteUri,
            IsActive = true
        };
        _atprotoIdentities.GetByDid(Did, Arg.Any<CancellationToken>()).Returns(identity);
        _references.HasCompletedConsolidationAsync(identity.Id, canonical.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        ConfigureIssuedToken();

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization, canonical.Id, canonical.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.ActorId).IsEqualTo(canonical.Id);
        await _references.DidNotReceiveWithAnyArgs().MoveMutableReferencesAsync(default, default, default, default);
        await _merges.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    public async Task SuspendedAtprotoIdentityFailsBeforeMutation()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        var identity = ConfigureExternalIdentity();
        identity.IsSuspended = true;

        var result = await CreateHandler().Handle(
            CreateCommand(AtprotoSubjectClassification.Organization),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("classification_conflict");
        await _organizations.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistPreparedAsync(default!, default);
    }

    [Test]
    public async Task PostCommitIssuerFailureCanRetryThroughIdempotentUpsert()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        AtprotoIdentity? storedIdentity = null;
        _atprotoIdentities.GetByDid(Did, Arg.Any<CancellationToken>()).Returns(_ => storedIdentity);
        _atprotoIdentities.Create(Arg.Any<AtprotoIdentity>()).Returns(call =>
        {
            storedIdentity = call.Arg<AtprotoIdentity>();
            return storedIdentity;
        });
        var issuerAttempts = 0;
        _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            issuerAttempts++;
            return issuerAttempts == 1
                ? throw new InvalidOperationException("signing unavailable")
                : new AtprotoIssuedSessionToken("retry-jwt", DateTimeOffset.UtcNow.AddMinutes(15));
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateHandler().Handle(CreateCommand(), CancellationToken.None));
        var retry = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(retry.Success).IsTrue();
        await Assert.That(retry.Token).IsEqualTo("retry-jwt");
        await _atprotoIdentities.Received(1).Create(Arg.Any<AtprotoIdentity>());
        await _atprotoIdentities.Received(1).Update(Arg.Any<AtprotoIdentity>());
        await _securityGateway.Received(2).PreparePersistenceAsync(
            Arg.Any<AtprotoVerifiedOAuthSession>(),
            _tenantId,
            _userId,
            Arg.Any<CancellationToken>());
        await _securityGateway.Received(2).PersistPreparedAsync(
            Arg.Any<AtprotoPreparedOAuthSession>(),
            Arg.Any<CancellationToken>());
        await _users.Received(2).GetById(_userId);
        await _actors.Received(2).GetTrackedActorByUserId(_userId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryableTransactionPreparesOnceAndPersistsPreparedSessionAfterCommit()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        _unitOfWork
            .ExecuteSerializableAsync<AtprotoSubjectOnboardingResult>(
                Arg.Any<Func<CancellationToken, Task<AtprotoSubjectOnboardingResult>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<AtprotoSubjectOnboardingResult>>>();
                var cancellationToken = call.Arg<CancellationToken>();
                await operation(cancellationToken);
                return await operation(cancellationToken);
            });
        _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>())
            .Returns(new AtprotoIssuedSessionToken("retry-jwt", DateTimeOffset.UtcNow.AddMinutes(15)));

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _securityGateway.Received(1).PreparePersistenceAsync(
            Arg.Any<AtprotoVerifiedOAuthSession>(),
            _tenantId,
            _userId,
            Arg.Any<CancellationToken>());
        await _securityGateway.Received(2).PersistPreparedAsync(
            Arg.Any<AtprotoPreparedOAuthSession>(),
            Arg.Any<CancellationToken>());
    }

    private void ConfigureVerifiedSession()
    {
        _securityGateway.VerifyAsync(Arg.Any<AtprotoOAuthVerificationInput>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoOAuthVerificationResult.Verified(new AtprotoVerifiedOAuthSession(
                Did,
                "linked.example",
                PdsUri,
                "oauth-active",
                [1, 2, 3])));
        _securityGateway.PreparePersistenceAsync(
                Arg.Any<AtprotoVerifiedOAuthSession>(),
                _tenantId,
                _userId,
                Arg.Any<CancellationToken>())
            .Returns(new AtprotoPreparedOAuthSession(
                [1, 2, 3],
                "encryption-active",
                1,
                _tenantId,
                _userId,
                Did,
                PdsUri.AbsoluteUri,
                "oauth-active",
                DateTime.UtcNow.AddHours(1)));
    }

    private (UserExternalLogin Login, User User, Actor Actor) ConfigureLinkedIdentity()
    {
        var user = new User
        {
            Id = _userId,
            Pii = new UserPii { Email = "linked@example.test", FirstName = "Linked", LastName = "User" }
        };
        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Pii = new ActorPii { DisplayName = "Linked User" },
            ActorType = null!
        };
        var login = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TenantId = _tenantId,
            Provider = "atproto",
            ProviderKey = Did,
            User = user,
            Tenant = null!
        };
        _externalLogins.GetByProviderAndKey("atproto", Did).Returns(login);
        _users.GetById(_userId).Returns(user);
        _actors.GetTrackedActorByUserId(_userId, Arg.Any<CancellationToken>()).Returns(actor);
        return (login, user, actor);
    }

    private BootstrapAtprotoSessionCommandHandler CreateHandler() => new(
        _securityGateway,
        _tokenIssuer,
        _externalLogins,
        _users,
        _actors,
        new AtprotoSubjectOnboardingOperation(_externalLogins, _atprotoIdentities, _actors, _actorTypes, _tenantUsers, _tenantUserRoleGrants, _organizations, _organizationTenants, _organizationMembers, _groups, _groupTenants, _groupMembers, _references, _merges),
        _unitOfWork,
        _adminCacheInvalidator,
        _tenantContext,
        TimeProvider.System);

    private static BootstrapAtprotoSessionCommand CreateCommand(
        AtprotoSubjectClassification classification = AtprotoSubjectClassification.Person,
        Guid? canonicalActorId = null,
        Guid? expectedCanonicalActorConcurrencyStamp = null) => new(
        Did,
        PdsUri.AbsoluteUri,
        "oauth-active",
        classification,
        [1, 2, 3],
        canonicalActorId,
        expectedCanonicalActorConcurrencyStamp);

    private AtprotoIdentity ConfigureExternalIdentity()
    {
        var external = new ExternalActorSubject { Id = Guid.NewGuid(), FirstObservedAt = DateTime.UtcNow, LastObservedAt = DateTime.UtcNow };
        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            ActorTypeId = (int)ActorTypeEnum.ExternalUnclassified,
            ActorType = null!,
            ExternalActorSubjectId = external.Id,
            ExternalActorSubject = external,
            Pii = new ActorPii { DisplayName = "External subject" }
        };
        external.Actor = actor;
        var identity = new AtprotoIdentity
        {
            Id = Guid.NewGuid(),
            Did = Did,
            ActorId = actor.Id,
            Actor = actor,
            PdsHost = PdsUri.AbsoluteUri,
            IsActive = true
        };
        _atprotoIdentities.GetByDid(Did, Arg.Any<CancellationToken>()).Returns(identity);
        return identity;
    }

    private Actor ConfigureCanonicalOrganizationAuthority()
    {
        var canonical = CreateCanonicalOrganizationActor();
        _actors.GetById(canonical.Id).Returns(canonical);
        var participation = new OrganizationTenant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Tenant = null!,
            OrganizationId = canonical.OrganizationId!.Value,
            Organization = canonical.Organization!,
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
            ApprovalStatus = null!
        };
        _organizationTenants.GetByOrganizationAndTenant(canonical.OrganizationId.Value, _tenantId, Arg.Any<CancellationToken>())
            .Returns(participation);
        _organizationMembers.GetByOrganizationAndUser(canonical.OrganizationId.Value, _userId).Returns(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationTenantId = participation.Id,
            OrganizationTenant = participation,
            UserId = _userId,
            User = null!,
            RoleId = (int)RoleEnum.OrgAdmin,
            Role = null!,
            TenantId = _tenantId,
            Tenant = null!
        });
        return canonical;
    }

    private static Actor CreateCanonicalOrganizationActor()
    {
        var organization = new Organization { Id = Guid.NewGuid(), Pii = new OrganizationPii { FullName = "Canonical organization" } };
        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            ActorTypeId = (int)ActorTypeEnum.Organization,
            ActorType = null!,
            OrganizationId = organization.Id,
            Organization = organization,
            Pii = new ActorPii { DisplayName = "Canonical organization" },
            ConcurrencyStamp = Guid.NewGuid()
        };
        organization.Actor = actor;
        return actor;
    }

    private void ConfigureIssuedToken() =>
        _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>())
            .Returns(new AtprotoIssuedSessionToken("platform-jwt", DateTimeOffset.UtcNow.AddMinutes(15)));

    private static TenantUser WithId(TenantUser entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.CreateVersion7() : entity.Id;
        return entity;
    }

    private static Organization WithId(Organization entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.CreateVersion7() : entity.Id;
        return entity;
    }

    private static Group WithId(Group entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.CreateVersion7() : entity.Id;
        return entity;
    }

    private static Actor WithId(Actor entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.CreateVersion7() : entity.Id;
        return entity;
    }

    private static OrganizationTenant WithId(OrganizationTenant entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.CreateVersion7() : entity.Id;
        return entity;
    }

    private static GroupTenant WithId(GroupTenant entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.CreateVersion7() : entity.Id;
        return entity;
    }

    private static OrganizationMember WithId(OrganizationMember entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.CreateVersion7() : entity.Id;
        return entity;
    }

    private static GroupMember WithId(GroupMember entity)
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.CreateVersion7() : entity.Id;
        return entity;
    }
}
