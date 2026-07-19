// ABOUTME: Tests the linked-account-only ATProto bootstrap transaction and post-commit JWT issuance.
// ABOUTME: Proves verification failures write nothing and post-commit issuance failures are safely retryable.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Authentication.Atproto.Handlers.Commands;
using Explore.Application.Features.Authentication.Atproto.Models;
using Explore.Application.Features.Authentication.Atproto.Requests.Commands;
using Explore.Domain;
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
    private readonly IIndexedDidRepository _indexedDids = Substitute.For<IIndexedDidRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public BootstrapAtprotoSessionCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _unitOfWork
            .ExecuteInTransactionAsync<AtprotoSessionBootstrapResult?>(
                Arg.Any<Func<CancellationToken, Task<AtprotoSessionBootstrapResult?>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<AtprotoSessionBootstrapResult?>>>()(
                call.Arg<CancellationToken>()));
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
        await _indexedDids.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistAsync(default!, default, default, default);
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
        await _indexedDids.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistAsync(default!, default, default, default);
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
        await _indexedDids.DidNotReceiveWithAnyArgs().Create(default!);
        await _securityGateway.DidNotReceiveWithAnyArgs().PersistAsync(default!, default, default, default);
        await _tokenIssuer.DidNotReceiveWithAnyArgs().IssueAsync(default, default, default!, default);
    }

    [Test]
    public async Task SuccessfulBootstrapCommitsIdentityIndexAndSessionBeforeIssuingPlatformToken()
    {
        ConfigureVerifiedSession();
        var identity = ConfigureLinkedIdentity();
        IndexedDid? createdDid = null;
        _indexedDids.GetById(Did).Returns(_ => createdDid);
        _indexedDids.Create(Arg.Any<IndexedDid>()).Returns(call =>
        {
            createdDid = call.Arg<IndexedDid>();
            return createdDid;
        });
        var issued = new AtprotoIssuedSessionToken("platform-jwt", DateTimeOffset.UtcNow.AddMinutes(15));
        _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>()).Returns(issued);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.UserId).IsEqualTo(_userId);
        await Assert.That(result.Token).IsEqualTo("platform-jwt");
        await Assert.That(identity.Actor.Did).IsEqualTo(Did);
        await Assert.That(identity.Actor.Handle).IsEqualTo("linked.example");
        await Assert.That(createdDid).IsNotNull();
        await _actors.Received(1).Update(identity.Actor);
        await _securityGateway.Received(1).PersistAsync(
            Arg.Is<AtprotoVerifiedOAuthSession>(session => session.Did == Did),
            _tenantId,
            _userId,
            Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _securityGateway.PersistAsync(Arg.Any<AtprotoVerifiedOAuthSession>(), _tenantId, _userId, Arg.Any<CancellationToken>());
            _tokenIssuer.IssueAsync(_userId, _tenantId, Did, Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task PostCommitIssuerFailureCanRetryThroughIdempotentUpsert()
    {
        ConfigureVerifiedSession();
        ConfigureLinkedIdentity();
        IndexedDid? storedDid = null;
        _indexedDids.GetById(Did).Returns(_ => storedDid);
        _indexedDids.Create(Arg.Any<IndexedDid>()).Returns(call =>
        {
            storedDid = call.Arg<IndexedDid>();
            return storedDid;
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
        await _indexedDids.Received(1).Create(Arg.Any<IndexedDid>());
        await _indexedDids.Received(1).Update(Arg.Any<IndexedDid>());
        await _securityGateway.Received(2).PersistAsync(
            Arg.Any<AtprotoVerifiedOAuthSession>(),
            _tenantId,
            _userId,
            Arg.Any<CancellationToken>());
    }

    private void ConfigureVerifiedSession() =>
        _securityGateway.VerifyAsync(Arg.Any<AtprotoOAuthVerificationInput>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoOAuthVerificationResult.Verified(new AtprotoVerifiedOAuthSession(
                Did,
                "linked.example",
                PdsUri,
                "oauth-active",
                [1, 2, 3])));

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
            TenantId = _tenantId,
            Pii = new ActorPii { DisplayName = "Linked User" },
            ActorType = null!,
            Tenant = null!
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
        _actors.GetActorByUserIdAndTenantId(_userId, _tenantId).Returns(actor);
        return (login, user, actor);
    }

    private BootstrapAtprotoSessionCommandHandler CreateHandler() => new(
        _securityGateway,
        _tokenIssuer,
        _externalLogins,
        _users,
        _actors,
        _indexedDids,
        _unitOfWork,
        _tenantContext,
        TimeProvider.System);

    private static BootstrapAtprotoSessionCommand CreateCommand() => new(
        Did,
        PdsUri.AbsoluteUri,
        "oauth-active",
        [1, 2, 3]);
}
