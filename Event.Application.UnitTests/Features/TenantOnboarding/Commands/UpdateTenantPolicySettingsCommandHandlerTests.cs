// ABOUTME: Unit tests for UpdateTenantPolicySettingsCommandHandler authorization enforcement.
// ABOUTME: Verifies that only tenant admins (Owner/Admin) or instance admins can update tenant settings.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Exceptions;
using Explore.Application.Features.TenantOnboarding.Handlers.Commands;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;
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
    private readonly ConfigurableUnitOfWork _unitOfWork;
    private readonly IHierarchicalSettingsResolver _hierarchicalSettingsResolver;
    private readonly IMediator _mediator;
    private readonly UpdateTenantPolicySettingsCommandHandler _handler;

    public UpdateTenantPolicySettingsCommandHandlerTests()
    {
        _tenantContext = Substitute.For<ITenantContext>();
        _onboardingStateRepository = Substitute.For<ITenantOnboardingStateRepository>();
        _adminContext = Substitute.For<IAdminContext>();
        _policySettingService = Substitute.For<ITenantPolicySettingService>();
        _unitOfWork = new ConfigurableUnitOfWork();
        _hierarchicalSettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _mediator = Substitute.For<IMediator>();

        _tenantContext.TenantId.Returns(TestTenantId);

        // Execute the lambda so inner service logic runs in tests
        _policySettingService.ApplyTenantSettingsAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid?>(),
            Arg.Any<UpdateTenantPolicyRequest>(),
            Arg.Any<CancellationToken>()).Returns([]);

        _handler = new UpdateTenantPolicySettingsCommandHandler(
            _tenantContext,
            _onboardingStateRepository,
            _adminContext,
            _policySettingService,
            _unitOfWork,
            _hierarchicalSettingsResolver,
            _mediator);

        _policySettingService.ReadEffectiveTenantSettingsAsync(TestTenantId).Returns(new TenantPolicySettingsDto
        {
            RequireOrganizationVerification = true,
            PreferredHomePage = "EventList",
            Subdomain = "tenant",
            CustomDomain = string.Empty,
            CanTenantOmitVerification = true,
            CanOverrideHomePagePreference = true,
            CanOverrideSubdomain = true,
            CanOverrideCustomDomain = true
        });
    }

    private static UpdateTenantPolicySettingsCommand CreateCommand() => new()
    {
        UserId = TestUserId,
        Settings = new UpdateTenantPolicyRequest()
    };

    [Test]
    public async Task Handle_WhenRegularMember_ReturnsUnauthorized()
    {
        // Arrange: user is neither tenant admin nor instance admin
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>());
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(false);

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
        var notification = new SettingChangedNotification(
            GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled,
            "false",
            "true",
            SettingSource.TenantOverride,
            TestTenantId,
            TestUserId,
            DateTime.UtcNow);
        _policySettingService.ApplyTenantSettingsAsync(
            TestTenantId,
            TestUserId,
            Arg.Any<UpdateTenantPolicyRequest>(),
            Arg.Any<CancellationToken>()).Returns([notification]);
        bool publishedInsideTransaction = false;
        _mediator.Publish(notification, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                publishedInsideTransaction = _unitOfWork.IsInsideTransaction;
                return Task.CompletedTask;
            });

        // Act
        using var cancellation = new CancellationTokenSource();
        var result = await _handler.Handle(CreateCommand(), cancellation.Token);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _policySettingService.Received(1).ApplyTenantSettingsAsync(
            TestTenantId,
            TestUserId,
            Arg.Any<UpdateTenantPolicyRequest>(),
            cancellation.Token);
        await Assert.That(publishedInsideTransaction).IsFalse();
        _hierarchicalSettingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, TestTenantId);
        await _mediator.Received(1).Publish(notification, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenResolvedUserIsTenantAdmin_ReturnsSuccess()
    {
        // Arrange: Keycloak subjects are resolved to an internal user before command authorization.
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(new[] { TestTenantId });
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
        await _policySettingService.Received(1).ApplyTenantSettingsAsync(
            TestTenantId,
            TestUserId,
            Arg.Any<UpdateTenantPolicyRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenInstanceAdmin_ReturnsSuccess()
    {
        // Arrange: user is not tenant admin but is instance admin
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>());
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(true);
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
        _adminContext.GetAdminTenantIdsAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>());
        _adminContext.IsInstanceAdminAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert: settings should NOT be applied
        await _policySettingService.DidNotReceive().ApplyTenantSettingsAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<UpdateTenantPolicyRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenLockedPreferredHomePageIsModified_ThrowsValidationException()
    {
        // Arrange
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        _policySettingService.ReadEffectiveTenantSettingsAsync(TestTenantId).Returns(new TenantPolicySettingsDto
        {
            PreferredHomePage = "EventList",
            CanOverrideHomePagePreference = false,
            CanTenantOmitVerification = true,
            CanOverrideSubdomain = true,
            CanOverrideCustomDomain = true
        });

        var command = CreateCommand();
        command.Settings.PreferredHomePage = "LandingPage";

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
        await _policySettingService.DidNotReceive().ApplyTenantSettingsAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<UpdateTenantPolicyRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenOuterTransactionRollsBack_DoesNotPublishOrInvalidate()
    {
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        var notification = new SettingChangedNotification(
            GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled,
            "false",
            "true",
            SettingSource.TenantOverride,
            TestTenantId,
            TestUserId,
            DateTime.UtcNow);
        _policySettingService.ApplyTenantSettingsAsync(
            TestTenantId,
            TestUserId,
            Arg.Any<UpdateTenantPolicyRequest>(),
            Arg.Any<CancellationToken>()).Returns([notification]);
        _unitOfWork.RollbackAfterOperation = true;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(CreateCommand(), CancellationToken.None));

        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
        _hierarchicalSettingsResolver.DidNotReceive().InvalidateCache(
            Arg.Any<SettingScope>(),
            Arg.Any<Guid?>());
    }

    private sealed class ConfigurableUnitOfWork : IUnitOfWork
    {
        public bool RollbackAfterOperation { get; set; }
        public bool IsInsideTransaction { get; private set; }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            IsInsideTransaction = true;
            try
            {
                await operation(ct);
            }
            finally
            {
                IsInsideTransaction = false;
            }
            ThrowIfRollingBack();
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            IsInsideTransaction = true;
            T result;
            try
            {
                result = await operation(ct);
            }
            finally
            {
                IsInsideTransaction = false;
            }
            ThrowIfRollingBack();
            return result;
        }

        private void ThrowIfRollingBack()
        {
            if (RollbackAfterOperation)
            {
                throw new InvalidOperationException("rollback");
            }
        }
    }
}
