// ABOUTME: Unit tests for UpdateTenantPolicySettingsCommandHandler authorization enforcement.
// ABOUTME: Verifies that only tenant admins (Owner/Admin) or instance admins can update tenant settings.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Exceptions;
using Explore.Application.Features.TenantOnboarding.Handlers.Commands;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.TenantOnboarding.Commands;

public class UpdateTenantPolicySettingsCommandHandlerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestUserId = Guid.NewGuid();

    private readonly ITenantContext _tenantContext;
    private readonly ITenantOnboardingStateRepository _onboardingStateRepository;
    private readonly IAdminContext _adminContext;
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly UpdateTenantPolicySettingsCommandHandler _handler;

    public UpdateTenantPolicySettingsCommandHandlerTests()
    {
        _tenantContext = Substitute.For<ITenantContext>();
        _onboardingStateRepository = Substitute.For<ITenantOnboardingStateRepository>();
        _adminContext = Substitute.For<IAdminContext>();
        _policySettingService = Substitute.For<ITenantPolicySettingService>();

        _tenantContext.TenantId.Returns(TestTenantId);

        _handler = new UpdateTenantPolicySettingsCommandHandler(
            _tenantContext,
            _onboardingStateRepository,
            _adminContext,
            _policySettingService);

        _policySettingService.ReadEffectiveTenantSettingsAsync(TestTenantId).Returns(new TenantPolicySettingsDto
        {
            RequireOrganizationVerification = true,
            PreferredHomePage = "EventList",
            Subdomain = "tenant",
            CustomDomain = string.Empty,
            BrandDisplayName = "ISLAMU Explore",
            BrandLogoUrl = string.Empty,
            BrandFaviconUrl = string.Empty,
            BrandCustomCssUrl = string.Empty,
            CanTenantOmitVerification = true,
            CanOverrideHomePagePreference = true,
            CanOverrideSubdomain = true,
            CanOverrideCustomDomain = true,
            CanOverrideBrandDisplayName = true,
            CanOverrideBrandLogoUrl = true,
            CanOverrideBrandFaviconUrl = true,
            CanOverrideBrandCustomCssUrl = true
        });
    }

    private static UpdateTenantPolicySettingsCommand CreateCommand() => new()
    {
        UserId = TestUserId,
        Settings = new TenantPolicySettingsDto()
    };

    [Test]
    public async Task Handle_WhenRegularMember_ReturnsUnauthorized()
    {
        // Arrange: user is neither tenant admin nor instance admin
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("tenant administrators");
    }

    [Test]
    public async Task Handle_WhenTenantAdmin_ReturnsSuccess()
    {
        // Arrange: user is tenant admin
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _onboardingStateRepository.GetByTenantId(TestTenantId).Returns(new TenantOnboardingState
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            Tenant = null!,
            IsCompleted = true
        });

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _policySettingService.Received(1).ApplyTenantSettingsAsync(TestTenantId, TestUserId, Arg.Any<TenantPolicySettingsDto>());
    }

    [Test]
    public async Task Handle_WhenInstanceAdmin_ReturnsSuccess()
    {
        // Arrange: user is not tenant admin but is instance admin
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _onboardingStateRepository.GetByTenantId(TestTenantId).Returns(new TenantOnboardingState
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            Tenant = null!,
            IsCompleted = true
        });

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_WhenRegularMember_DoesNotApplySettings()
    {
        // Arrange: regular member (not admin)
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert: settings should NOT be applied
        await _policySettingService.DidNotReceive().ApplyTenantSettingsAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<TenantPolicySettingsDto>());
    }

    [Test]
    public async Task Handle_WhenLockedBrandDisplayNameIsModified_ThrowsValidationException()
    {
        // Arrange
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _policySettingService.ReadEffectiveTenantSettingsAsync(TestTenantId).Returns(new TenantPolicySettingsDto
        {
            BrandDisplayName = "Locked Brand",
            CanOverrideBrandDisplayName = false,
            CanTenantOmitVerification = true,
            CanOverrideHomePagePreference = true,
            CanOverrideSubdomain = true,
            CanOverrideCustomDomain = true,
            CanOverrideBrandLogoUrl = true,
            CanOverrideBrandFaviconUrl = true,
            CanOverrideBrandCustomCssUrl = true
        });

        var command = CreateCommand();
        command.Settings.BrandDisplayName = "Modified Brand";

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
        await _policySettingService.DidNotReceive().ApplyTenantSettingsAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<TenantPolicySettingsDto>());
    }
}
