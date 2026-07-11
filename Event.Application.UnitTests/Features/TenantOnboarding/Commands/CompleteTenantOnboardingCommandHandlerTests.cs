// ABOUTME: Unit tests for CompleteTenantOnboardingCommandHandler authorization enforcement.
// ABOUTME: Verifies that only tenant admins (Owner/Admin) or instance admins can complete onboarding.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Features.TenantOnboarding.Handlers.Commands;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Settings.Documents;
using MediatR;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.TenantOnboarding.Commands;

public class CompleteTenantOnboardingCommandHandlerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestUserId = Guid.NewGuid();

    private readonly ITenantContext _tenantContext;
    private readonly ITenantOnboardingStateRepository _onboardingStateRepository;
    private readonly IAdminContext _adminContext;
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHierarchicalSettingsResolver _hierarchicalSettingsResolver;
    private readonly IMediator _mediator;
    private readonly CompleteTenantOnboardingCommandHandler _handler;

    public CompleteTenantOnboardingCommandHandlerTests()
    {
        _tenantContext = Substitute.For<ITenantContext>();
        _onboardingStateRepository = Substitute.For<ITenantOnboardingStateRepository>();
        _adminContext = Substitute.For<IAdminContext>();
        _policySettingService = Substitute.For<ITenantPolicySettingService>();
        _tenantBrandingProvisioningService = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _tenantBrandingProvisioningService
            .EnsureTenantBrandingDocumentAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantBrandingSettingsDocumentDefaults.Create(call.ArgAt<Guid>(0), call.ArgAt<string?>(1))));
        _hierarchicalSettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _mediator = Substitute.For<IMediator>();

        _tenantContext.TenantId.Returns(TestTenantId);

        // Execute the lambda so inner repo logic runs in tests
        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<(Guid OnboardingStateId, IReadOnlyList<SettingChangedNotification> Notifications)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<CancellationToken, Task<(Guid OnboardingStateId, IReadOnlyList<SettingChangedNotification> Notifications)>>>();
                return op(callInfo.ArgAt<CancellationToken>(1));
            });
        _policySettingService.ApplyTenantSettingsAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid?>(),
            Arg.Any<UpdateTenantPolicyRequest>(),
            Arg.Any<CancellationToken>()).Returns([]);

        _handler = new CompleteTenantOnboardingCommandHandler(
            _tenantContext,
            _onboardingStateRepository,
            _adminContext,
            _policySettingService,
            _tenantBrandingProvisioningService,
            _unitOfWork,
            _hierarchicalSettingsResolver,
            _mediator);
    }

    private static CompleteTenantOnboardingCommand CreateCommand() => new()
    {
        UserId = TestUserId,
        Settings = new UpdateTenantPolicyRequest()
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
        _onboardingStateRepository.GetByTenantId(TestTenantId).Returns((TenantOnboardingState?)null);
        _onboardingStateRepository.Create(Arg.Any<TenantOnboardingState>()).Returns(callInfo =>
        {
            var state = callInfo.Arg<TenantOnboardingState>();
            state.Id = Guid.NewGuid();
            return state;
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
        await _tenantBrandingProvisioningService.Received(1).EnsureTenantBrandingDocumentAsync(TestTenantId, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenInstanceAdmin_ReturnsSuccess()
    {
        // Arrange: user is not tenant admin but is instance admin
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _onboardingStateRepository.GetByTenantId(TestTenantId).Returns((TenantOnboardingState?)null);
        _onboardingStateRepository.Create(Arg.Any<TenantOnboardingState>()).Returns(callInfo =>
        {
            var state = callInfo.Arg<TenantOnboardingState>();
            state.Id = Guid.NewGuid();
            return state;
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
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<UpdateTenantPolicyRequest>(), Arg.Any<CancellationToken>());
        await _tenantBrandingProvisioningService.DidNotReceive().EnsureTenantBrandingDocumentAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExistingOnboardingState_UpdatesInsteadOfCreating()
    {
        // Arrange
        _adminContext.IsTenantAdminAsync(TestTenantId, Arg.Any<CancellationToken>()).Returns(true);
        var existingState = new TenantOnboardingState
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            Tenant = null!,
            IsCompleted = false
        };
        _onboardingStateRepository.GetByTenantId(TestTenantId).Returns(existingState);

        // Act
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existingState.Id);
        await _onboardingStateRepository.Received(1).Update(Arg.Is<TenantOnboardingState>(s => s.IsCompleted));
        await _onboardingStateRepository.DidNotReceive().Create(Arg.Any<TenantOnboardingState>());
    }
}
