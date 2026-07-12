// ABOUTME: Unit tests for post-onboarding auth provider update handler authorization and lockout guardrails.
// ABOUTME: Verifies instance-admin requirement, validation path, and provider lockout prevention behavior.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public class UpdateAuthProviderConfigurationCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000221");

    private readonly IAdminContext _adminContext;
    private readonly IUserRepository _userRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IAuthProviderConfigurationService _configurationService;
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier;
    private readonly UpdateAuthProviderConfigurationCommandHandler _handler;

    public UpdateAuthProviderConfigurationCommandHandlerTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _userRepository = Substitute.For<IUserRepository>();
        _userExternalLoginRepository = Substitute.For<IUserExternalLoginRepository>();
        _configurationService = Substitute.For<IAuthProviderConfigurationService>();
        _jwtAuthorityRefreshNotifier = Substitute.For<IJwtAuthorityRefreshNotifier>();
        _configurationService.ReadConfigurationAsync().Returns(new AuthProviderConfigurationDto());

        _handler = new UpdateAuthProviderConfigurationCommandHandler(
            _adminContext,
            _userRepository,
            _userExternalLoginRepository,
            _configurationService,
            _jwtAuthorityRefreshNotifier);
    }

    [Test]
    public async Task Handle_WhenUserIsNotInstanceAdmin_ReturnsUnauthorized()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(CreateCommand(CreateValidConfiguration()), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Only instance administrators");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
    }

    [Test]
    public async Task Handle_WhenConfigurationIsInvalid_ReturnsValidationFailure()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);

        var invalidConfiguration = new AuthProviderConfigurationDto
        {
            KeycloakEnabled = false,
            AtprotoLoginEnabled = false,
            GoogleSsoEnabled = false
        };

        var result = await _handler.Handle(CreateCommand(invalidConfiguration), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Invalid auth provider configuration");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
    }

    [Test]
    public async Task Handle_WhenUpdateWouldDisableAllCurrentAdminProviders_ReturnsFailure()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _userRepository.GetById(TestUserId).Returns(CreateUser("keycloak"));
        _userExternalLoginRepository.GetByUser(TestUserId).Returns(new List<UserExternalLogin>());

        var lockoutConfiguration = new AuthProviderConfigurationDto
        {
            KeycloakEnabled = false,
            AtprotoLoginEnabled = false,
            GoogleSsoEnabled = true,
            GoogleClientId = "client",
            GoogleClientSecret = "secret"
        };

        var result = await _handler.Handle(CreateCommand(lockoutConfiguration), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Cannot disable all authentication providers linked");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
    }

    [Test]
    public async Task Handle_WhenAdminKeepsAtLeastOneLinkedProvider_AppliesConfiguration()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _userRepository.GetById(TestUserId).Returns(CreateUser("keycloak"));
        _userExternalLoginRepository.GetByUser(TestUserId).Returns(new List<UserExternalLogin>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                User = CreateUser("keycloak"),
                TenantId = Guid.NewGuid(),
                Tenant = new Tenant
                {
                    FullName = "Tenant",
                    Slug = "tenant",
                    TenantStatusId = 1,
                    TenantStatus = new TenantStatus
                    {
                        Id = 1,
                        MasterCode = "ACTIVE",
                        FullName = "Active"
                    }
                },
                Provider = "google",
                ProviderKey = "key"
            }
        });

        var configuration = new AuthProviderConfigurationDto
        {
            KeycloakEnabled = false,
            AtprotoLoginEnabled = false,
            GoogleSsoEnabled = true,
            GoogleClientId = "client",
            GoogleClientSecret = "secret"
        };

        var result = await _handler.Handle(CreateCommand(configuration), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _configurationService.Received(1).ApplyConfigurationAsync(configuration);
    }

    [Test]
    public async Task Handle_WithRedactedConfiguredKeycloakSecret_AppliesConfiguration()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _configurationService.ReadConfigurationAsync().Returns(new AuthProviderConfigurationDto
        {
            KeycloakAuthority = "https://keycloak.example.com/realms/test",
            KeycloakClientId = "client-id",
            KeycloakClientSecretOwnership = { Configured = true }
        });
        _userRepository.GetById(TestUserId).Returns(CreateUser("keycloak"));
        _userExternalLoginRepository.GetByUser(TestUserId).Returns(new List<UserExternalLogin>());

        var configuration = CreateValidConfiguration();
        configuration.KeycloakClientSecret = string.Empty;

        var result = await _handler.Handle(CreateCommand(configuration), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _configurationService.Received(1).ApplyConfigurationAsync(configuration);
    }

    [Test]
    public async Task Handle_WithForgedConfiguredOwnershipAndNoServerSecret_ReturnsValidationFailure()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        var configuration = CreateValidConfiguration();
        configuration.KeycloakClientSecret = string.Empty;
        configuration.KeycloakClientSecretOwnership.Configured = true;

        var result = await _handler.Handle(CreateCommand(configuration), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Invalid auth provider configuration");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
    }

    [Test]
    public async Task Handle_WithConfiguredSecretForDifferentClient_ReturnsValidationFailure()
    {
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
        _configurationService.ReadConfigurationAsync().Returns(new AuthProviderConfigurationDto
        {
            KeycloakAuthority = "https://keycloak.example.com/realms/test",
            KeycloakClientId = "current-client",
            KeycloakClientSecretOwnership = { Configured = true }
        });
        var configuration = CreateValidConfiguration();
        configuration.KeycloakClientSecret = string.Empty;

        var result = await _handler.Handle(CreateCommand(configuration), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Invalid auth provider configuration");
        await _configurationService.DidNotReceive().ApplyConfigurationAsync(Arg.Any<AuthProviderConfigurationDto>());
    }

    private static UpdateAuthProviderConfigurationCommand CreateCommand(AuthProviderConfigurationDto configuration)
    {
        return new UpdateAuthProviderConfigurationCommand
        {
            UserId = TestUserId,
            Configuration = configuration
        };
    }

    private static AuthProviderConfigurationDto CreateValidConfiguration()
    {
        return new AuthProviderConfigurationDto
        {
            KeycloakEnabled = true,
            KeycloakAuthority = "https://keycloak.example.com/realms/test",
            KeycloakClientId = "client-id",
            KeycloakClientSecret = "secret",
            AtprotoLoginEnabled = false,
            GoogleSsoEnabled = false
        };
    }

    private static User CreateUser(string authProvider)
    {
        return new User
        {
            Id = TestUserId,
            Pii = new UserPii
            {
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User"
            },
            AuthProvider = authProvider,
            AuthProviderId = "provider-subject"
        };
    }
}
