// ABOUTME: Verifies provider-aware SyncUser behavior for account creation, linking, and ATProto restrictions.
// ABOUTME: Covers email auto-match linking (Keycloak/Google) and DID-based explicit-link requirement for ATProto.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.User;
using Explore.Application.Features.Users.Handlers.Commands;
using Explore.Application.Features.Users.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Users.Commands;

public class SyncUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IActorRepository _actorRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SyncUserCommandHandler _handler;

    public SyncUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userExternalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _tenantRepository = Substitute.For<ITenantRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _configuration = new ConfigurationBuilder().Build();
        _cache = Substitute.For<HybridCache>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        // Execute the lambda so inner repo logic runs in tests
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<User>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<User>>>();
                return op(CancellationToken.None);
            });

        _handler = new SyncUserCommandHandler(
            _userRepository,
            _userExternalLoginRepository,
            _actorRepository,
            _tenantRepository,
            _tenantContext,
            _configuration,
            _cache,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_CreatesNewKeycloakUserAndExternalLogin_WhenNoExistingUser()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        var providerUserId = Guid.NewGuid();
        var dto = new UserDto
        {
            Id = providerUserId,
            Email = "new.user@example.com",
            FirstName = "New",
            LastName = "User",
            AuthProvider = "keycloak",
            AuthProviderId = providerUserId.ToString(),
            EmailVerified = true
        };

        _userExternalLoginRepository.GetByProviderAndKey("keycloak", providerUserId.ToString())
            .Returns((UserExternalLogin?)null);
        _userRepository.GetById(providerUserId).Returns((User?)null);
        _userRepository.GetUserByEmail("new.user@example.com").Returns((User?)null);

        _tenantRepository.GetAll().Returns([DataBuilder.Tenant.Generate()]);
        _userRepository.Create(Arg.Any<User>()).Returns(ci => ci.Arg<User>());
        _actorRepository.Create(Arg.Any<Actor>()).Returns(ci =>
        {
            var actor = ci.Arg<Actor>();
            actor.Id = Guid.NewGuid();
            return actor;
        });

        // Act
        var result = await _handler.Handle(new SyncUserCommand { UserDto = dto }, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await _userExternalLoginRepository.Received(1)
            .Create(Arg.Is<UserExternalLogin>(x =>
                x.Provider == "keycloak" &&
                x.ProviderKey == providerUserId.ToString() &&
                x.TenantId == tenantId));
    }

    [Test]
    public async Task Handle_LinksGoogleToExistingEmailUser_WhenEmailVerified()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        var existingUser = DataBuilder.User.Generate();
        existingUser.Id = Guid.NewGuid();
        existingUser.Email = "shared@example.com";

        var dto = new UserDto
        {
            Id = Guid.Empty,
            Email = "shared@example.com",
            FirstName = "Shared",
            LastName = "User",
            AuthProvider = "google",
            AuthProviderId = "google-sub-123",
            EmailVerified = true
        };

        _userExternalLoginRepository.GetByProviderAndKey("google", "google-sub-123")
            .Returns((UserExternalLogin?)null);
        _userRepository.GetUserByEmail("shared@example.com").Returns(existingUser);

        // Act
        var result = await _handler.Handle(new SyncUserCommand { UserDto = dto }, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existingUser.Id);
        await _userExternalLoginRepository.Received(1)
            .Create(Arg.Is<UserExternalLogin>(x =>
                x.UserId == existingUser.Id &&
                x.Provider == "google" &&
                x.ProviderKey == "google-sub-123" &&
                x.TenantId == tenantId));
    }

    [Test]
    public async Task Handle_ReturnsFailure_ForAtprotoWithoutEmailAndWithoutExplicitLink()
    {
        // Arrange
        var dto = new UserDto
        {
            Id = Guid.Empty,
            Email = string.Empty,
            FirstName = "Atproto",
            LastName = "User",
            AuthProvider = "atproto",
            AuthProviderId = "did:plc:abc123",
            EmailVerified = false
        };

        _userExternalLoginRepository.GetByProviderAndKey("atproto", "did:plc:abc123")
            .Returns((UserExternalLogin?)null);

        // Act
        var result = await _handler.Handle(new SyncUserCommand { UserDto = dto }, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).Contains("explicitly linked");
        await _userRepository.DidNotReceive().Create(Arg.Any<User>());
    }
}
