// ABOUTME: Unit tests for the minimal instance onboarding profile persistence command.
// ABOUTME: Proves the guard rails, exact allowed writes, and audit emission shape/order.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public sealed class SaveInstanceOnboardingProfileCommandHandlerTests
{
    private readonly IInstanceBootstrapStateRepository _bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
    private readonly ISystemSettingRepository _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
    private readonly ISetupSecretProvider _setupSecretProvider = Substitute.For<ISetupSecretProvider>();
    private readonly IInstanceBootstrapAuditLogger _bootstrapAuditLogger = Substitute.For<IInstanceBootstrapAuditLogger>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SaveInstanceOnboardingProfileCommandHandler _handler;
    private readonly List<SystemSetting> _capturedUpserts = [];
    private readonly InstanceBootstrapState _currentBootstrap = new()
    {
        Id = Guid.NewGuid(),
        IsCompleted = false,
        CreatedAt = DateTime.UtcNow
    };

    public SaveInstanceOnboardingProfileCommandHandlerTests()
    {
        _bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var token = callInfo.ArgAt<CancellationToken>(0);
                token.ThrowIfCancellationRequested();
                return _currentBootstrap;
            });

        _setupSecretProvider.IsSetupModeActive.Returns(true);

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.ArgAt<Func<CancellationToken, Task>>(0);
                var token = callInfo.ArgAt<CancellationToken>(1);
                return operation(token);
            });

        _systemSettingRepository
            .UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _capturedUpserts.Add(callInfo.ArgAt<SystemSetting>(0));
                return Task.FromResult<string?>(null);
            });

        _handler = new SaveInstanceOnboardingProfileCommandHandler(
            _bootstrapRepository,
            _systemSettingRepository,
            _setupSecretProvider,
            _bootstrapAuditLogger,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenSetupModeActive_WritesNormalizedAllowedSettingsAndExcludesRestrictedKeys()
    {
        _capturedUpserts.Clear();

        var result = await _handler.Handle(new SaveInstanceOnboardingProfileCommand
        {
            Profile = new SelfHostOnboardingProfileDto
            {
                SiteName = "  Community Events  ",
                SupportEmail = " support@example.org ",
                CanonicalUrl = "https://Events.Example.Org/onboarding",
                Locale = " EN ",
                TimeZone = "UTC",
                Purpose = "Transient"
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_currentBootstrap.Id);
        await Assert.That(_capturedUpserts.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Branding.DisplayName,
            GovernanceSettingKeys.Email.FromAddress,
            GovernanceSettingKeys.Domains.InstanceBaseDomain,
            GovernanceSettingKeys.Localization.DefaultLanguage]);
        await Assert.That(_capturedUpserts.Count).IsEqualTo(4);
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Branding.DisplayName).Value)
            .IsEqualTo(JsonSerializer.Serialize("Community Events"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Email.FromAddress).Value)
            .IsEqualTo(JsonSerializer.Serialize("support@example.org"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Domains.InstanceBaseDomain).Value)
            .IsEqualTo(JsonSerializer.Serialize("events.example.org"));
        await Assert.That(_capturedUpserts.Single(setting => setting.SettingKey == GovernanceSettingKeys.Localization.DefaultLanguage).Value)
            .IsEqualTo(JsonSerializer.Serialize("en"));

        foreach (var forbiddenKey in new[]
                 {
                     GovernanceSettingKeys.Deployment.Mode,
                     GovernanceSettingKeys.Domains.AdminHost,
                     "time_zone",
                     "purpose",
                     GovernanceSettingKeys.PublicExperience.Mode,
                     GovernanceSettingKeys.PublicExperience.EventCatalogLabel,
                     GovernanceSettingKeys.PublicExperience.HomeBlocks,
                     GovernanceSettingKeys.PublicExperience.Ctas,
                     GovernanceSettingKeys.Routing.DefaultPublicHomePage
                 })
        {
            await Assert.That(_capturedUpserts.Select(setting => setting.SettingKey)).DoesNotContain(forbiddenKey);
        }

        _bootstrapAuditLogger.Received(1).Log(Arg.Is<InstanceBootstrapAuditEvent>(auditEvent =>
            auditEvent.EventType == InstanceBootstrapAuditEventType.SetupProfileSaved
            && auditEvent.Operation == "instance_onboarding_profile_save"
            && auditEvent.Outcome == "saved"
            && HasNoOnboardingPayloadShape(auditEvent)));
    }

    [Test]
    public async Task Handle_WhenSetupModeInactive_WritesNothing()
    {
        _setupSecretProvider.IsSetupModeActive.Returns(false);

        var result = await _handler.Handle(new SaveInstanceOnboardingProfileCommand
        {
            Profile = new SelfHostOnboardingProfileDto
            {
                SiteName = "Community Events",
                Locale = "en",
                TimeZone = "UTC"
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Id).IsEqualTo(_currentBootstrap.Id);
        await Assert.That(result.Message).IsEqualTo("Setup mode is no longer active.");
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        await _systemSettingRepository.DidNotReceive().UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>());
        _bootstrapAuditLogger.DidNotReceive().Log(Arg.Any<InstanceBootstrapAuditEvent>());
    }

    [Test]
    public async Task Handle_WhenBootstrapCompleted_WritesNothing()
    {
        _currentBootstrap.IsCompleted = true;

        var result = await _handler.Handle(new SaveInstanceOnboardingProfileCommand
        {
            Profile = new SelfHostOnboardingProfileDto
            {
                SiteName = "Community Events",
                Locale = "en",
                TimeZone = "UTC"
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Id).IsEqualTo(_currentBootstrap.Id);
        await Assert.That(result.Message).IsEqualTo("Setup mode is no longer active.");
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        await _systemSettingRepository.DidNotReceive().UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>());
        _bootstrapAuditLogger.DidNotReceive().Log(Arg.Any<InstanceBootstrapAuditEvent>());
    }

    [Test]
    public async Task Handle_WithInvalidProfile_WritesNothing()
    {
        var result = await _handler.Handle(new SaveInstanceOnboardingProfileCommand
        {
            Profile = new SelfHostOnboardingProfileDto
            {
                SiteName = string.Empty,
                SupportEmail = "not-an-email",
                CanonicalUrl = "ftp://example.org",
                Locale = string.Empty,
                TimeZone = string.Empty
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Count).IsGreaterThan(0);
        await _bootstrapRepository.Received(1).GetCurrent(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        await _systemSettingRepository.DidNotReceive().UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>());
        _bootstrapAuditLogger.DidNotReceive().Log(Arg.Any<InstanceBootstrapAuditEvent>());
    }

    [Test]
    public async Task Handle_WithBlankOptionalFields_OmitsOptionalWrites()
    {
        var result = await _handler.Handle(new SaveInstanceOnboardingProfileCommand
        {
            Profile = new SelfHostOnboardingProfileDto
            {
                SiteName = " Community Events ",
                Locale = " en "
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _systemSettingRepository.Received(2).UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>());
        _ = _systemSettingRepository.DidNotReceive().UpsertAsync(Arg.Is<SystemSetting>(setting =>
            setting.SettingKey == GovernanceSettingKeys.Email.FromAddress), Arg.Any<CancellationToken>());
        _ = _systemSettingRepository.DidNotReceive().UpsertAsync(Arg.Is<SystemSetting>(setting =>
            setting.SettingKey == GovernanceSettingKeys.Domains.InstanceBaseDomain), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithCancelledToken_StopsBeforeWrites()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var caught = false;
        try
        {
            await _handler.Handle(new SaveInstanceOnboardingProfileCommand
            {
                Profile = new SelfHostOnboardingProfileDto
                {
                    SiteName = "Community Events"
                }
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            caught = true;
        }

        await Assert.That(caught).IsTrue();
        await _bootstrapRepository.Received(1).GetCurrent(cts.Token);
        await _systemSettingRepository.DidNotReceive().UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>());
        _bootstrapAuditLogger.DidNotReceive().Log(Arg.Any<InstanceBootstrapAuditEvent>());
    }

    [Test]
    public async Task Handle_PropagatesCancellationTokenToTransactionAndSettings()
    {
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var result = await _handler.Handle(new SaveInstanceOnboardingProfileCommand
        {
            Profile = new SelfHostOnboardingProfileDto
            {
                SiteName = "Community Events",
                SupportEmail = "support@example.org",
                CanonicalUrl = "https://events.example.org/onboarding",
                Locale = "en",
                TimeZone = "UTC"
            }
        }, cancellationToken);

        await Assert.That(result.IsSuccess).IsTrue();
        await _bootstrapRepository.Received(1).GetCurrent(cancellationToken);
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), cancellationToken);
        await _systemSettingRepository.Received(4).UpsertAsync(
            Arg.Any<SystemSetting>(), cancellationToken);
    }

    [Test]
    public async Task Handle_AuditEmittedOnlyAfterSuccess_AndEventHasNoPayloadShape()
    {
        var transactionCompleted = false;
        var observedAuditEvent = default(InstanceBootstrapAuditEvent);

        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.ArgAt<Func<CancellationToken, Task>>(0);
                var token = callInfo.ArgAt<CancellationToken>(1);

                return Task.Run(async () =>
                {
                    await operation(token);
                    transactionCompleted = true;
                }, token);
            });

        _bootstrapAuditLogger.When(logger => logger.Log(Arg.Any<InstanceBootstrapAuditEvent>()))
            .Do(callInfo =>
            {
                if (!transactionCompleted)
                {
                    throw new InvalidOperationException("Audit fired before transaction completed.");
                }

                observedAuditEvent = callInfo.Arg<InstanceBootstrapAuditEvent>();
            });

        var result = await _handler.Handle(new SaveInstanceOnboardingProfileCommand
        {
            Profile = new SelfHostOnboardingProfileDto
            {
                SiteName = "Community Events",
                SupportEmail = "support@example.org",
                CanonicalUrl = "https://events.example.org/onboarding",
                Locale = "en",
                TimeZone = "UTC"
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(transactionCompleted).IsTrue();
        await Assert.That(observedAuditEvent).IsNotNull();
        await Assert.That(HasNoOnboardingPayloadShape(observedAuditEvent!)).IsTrue();
    }

    private static bool HasNoOnboardingPayloadShape(InstanceBootstrapAuditEvent auditEvent)
    {
        var forbiddenPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Profile",
            "Payload",
            "Site",
            "SiteName",
            "Email",
            "Url",
            "Locale",
            "TimeZone",
            "Purpose"
        };

        var eventPropertyNames = typeof(InstanceBootstrapAuditEvent)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        return forbiddenPropertyNames.All(propertyName => !eventPropertyNames.Contains(propertyName))
            && string.IsNullOrWhiteSpace(auditEvent.RouteName)
            && string.IsNullOrWhiteSpace(auditEvent.TraceId)
            && string.IsNullOrWhiteSpace(auditEvent.FailureCode)
            && string.IsNullOrWhiteSpace(auditEvent.Provider)
            && string.IsNullOrWhiteSpace(auditEvent.Mode)
            && string.IsNullOrWhiteSpace(auditEvent.Realm)
            && string.IsNullOrWhiteSpace(auditEvent.ClientId)
            && string.IsNullOrWhiteSpace(auditEvent.DeploymentMode);
    }
}
