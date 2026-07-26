// ABOUTME: Focused unit tests for presence-aware ordinary instance settings command handlers.
// ABOUTME: Proves one-leaf patches preserve read siblings and invalid render-policy candidates never write.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Notifications;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public sealed class UpdateInstanceSubResourceCommandHandlerTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly IInstanceGovernanceSettingService _service = Substitute.For<IInstanceGovernanceSettingService>();
    private readonly IDeploymentModeProvider _deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public UpdateInstanceSubResourceCommandHandlerTests()
    {
        _adminContext.IsInstanceAdminAsync(UserId, Arg.Any<CancellationToken>()).Returns(true);
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _deploymentModeProvider.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(true);
        _service.ReadSettingsAsync().Returns(CreateSettings());
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task ModulePatch_WhenSingleTenant_UsesDefaultTenantForCapabilitySync()
    {
        var handler = new UpdateModuleSettingsCommandHandler(
            _adminContext,
            _service,
            _deploymentModeProvider,
            _unitOfWork);

        var result = await handler.Handle(new UpdateModuleSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchModuleSettingsDto { EnableIslamicModule = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _service.Received(1).ApplyModuleSettingsPatchAsync(
            PlatformDefaults.DefaultTenantId,
            Arg.Is<PatchModuleSettingsDto>(patch => patch.EnableIslamicModule.HasValue),
            Arg.Is<ModuleSettingsDto>(settings => !settings.EnableIslamicModule && settings.EnableTechModule),
            UserId,
            CancellationToken.None);
    }

    [Test]
    public async Task ModulePatch_WhenMultiTenant_DoesNotPassDefaultTenantForCapabilitySync()
    {
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        deploymentModeProvider.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateModuleSettingsCommandHandler(
            _adminContext,
            _service,
            deploymentModeProvider,
            _unitOfWork);

        await handler.Handle(new UpdateModuleSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchModuleSettingsDto { EnableTechModule = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyModuleSettingsPatchAsync(
            null,
            Arg.Is<PatchModuleSettingsDto>(patch => patch.EnableTechModule.HasValue),
            Arg.Is<ModuleSettingsDto>(settings => settings.EnableIslamicModule && !settings.EnableTechModule),
            UserId,
            CancellationToken.None);
    }

    [Test]
    public async Task ModulePatch_PassesTransactionCancellationTokenToService()
    {
        using var transactionCancellation = new CancellationTokenSource();
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(transactionCancellation.Token));
        var handler = new UpdateModuleSettingsCommandHandler(
            _adminContext,
            _service,
            _deploymentModeProvider,
            _unitOfWork);

        await handler.Handle(new UpdateModuleSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchModuleSettingsDto { EnableIslamicModule = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyModuleSettingsPatchAsync(
            PlatformDefaults.DefaultTenantId,
            Arg.Any<PatchModuleSettingsDto>(),
            Arg.Any<ModuleSettingsDto>(),
            UserId,
            transactionCancellation.Token);
    }

    [Test]
    public async Task EventPolicyPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateEventPolicyCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateEventPolicyCommand
        {
            UserId = UserId,
            Patch = new PatchEventPolicyDto { AllowUserSubmittedEvents = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyEventPolicyPatchAsync(
            Arg.Is<PatchEventPolicyDto>(patch => patch.AllowUserSubmittedEvents.HasValue),
            Arg.Is<EventPolicyDto>(settings => !settings.AllowUserSubmittedEvents && settings.AllowOrganizationSubmittedEvents),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EventPolicyLockPatch_PublishesOnlyAfterTransactionCommits()
    {
        var events = new List<string>();
        var notification = new SettingChangedNotification(
            GovernanceSettingKeys.Events.CardClickOpensDetailPage,
            "false",
            "false",
            SettingSource.SystemLocked,
            null,
            UserId,
            DateTime.UtcNow);
        _service.ApplyEventPolicyPatchAsync(
                Arg.Any<PatchEventPolicyDto>(),
                Arg.Any<EventPolicyDto>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                events.Add("service");
                return Task.FromResult<IReadOnlyList<SettingChangedNotification>>([notification]);
            });
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None);
                events.Add("commit");
            });
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                events.Add("publish");
                return Task.CompletedTask;
            });
        var handler = new UpdateEventPolicyCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateEventPolicyCommand
        {
            UserId = UserId,
            Patch = new PatchEventPolicyDto
            {
                LockTenantEventCardClickBehavior = OptionalUpdate<bool>.Set(true)
            }
        }, CancellationToken.None);

        await Assert.That(events.Count).IsEqualTo(3);
        await Assert.That(events[0]).IsEqualTo("service");
        await Assert.That(events[1]).IsEqualTo("commit");
        await Assert.That(events[2]).IsEqualTo("publish");
    }

    [Test]
    public async Task EventPolicyLockPatch_WhenTransactionRollsBack_PublishesNothing()
    {
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("rollback")));
        var handler = new UpdateEventPolicyCommandHandler(_adminContext, _service, _unitOfWork, _mediator);
        Exception? exception = null;

        try
        {
            await handler.Handle(new UpdateEventPolicyCommand
            {
                UserId = UserId,
                Patch = new PatchEventPolicyDto
                {
                    LockTenantEventCardClickBehavior = OptionalUpdate<bool>.Set(true)
                }
            }, CancellationToken.None);
        }
        catch (Exception caught)
        {
            exception = caught;
        }

        await Assert.That(exception).IsNotNull();
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EventPolicyLockPatch_WhenTransactionRetries_PublishesOnlyFinalCommittedNotifications()
    {
        var first = new SettingChangedNotification("first", null, "first", SettingSource.SystemLocked, null, UserId, DateTime.UtcNow);
        var final = new SettingChangedNotification("final", null, "final", SettingSource.SystemLocked, null, UserId, DateTime.UtcNow);
        var attempt = 0;
        _service.ApplyEventPolicyPatchAsync(
                Arg.Any<PatchEventPolicyDto>(),
                Arg.Any<EventPolicyDto>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<SettingChangedNotification>>(
                [++attempt == 1 ? first : final]));
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                Func<CancellationToken, Task> transaction = call.Arg<Func<CancellationToken, Task>>();
                await transaction(CancellationToken.None);
                await transaction(CancellationToken.None);
            });
        var handler = new UpdateEventPolicyCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateEventPolicyCommand
        {
            UserId = UserId,
            Patch = new PatchEventPolicyDto
            {
                LockTenantEventCardClickBehavior = OptionalUpdate<bool>.Set(true)
            }
        }, CancellationToken.None);

        SettingChangedNotification[] published = _mediator.ReceivedCalls()
            .Select(call => call.GetArguments().FirstOrDefault())
            .OfType<SettingChangedNotification>()
            .ToArray();
        await Assert.That(published.Length).IsEqualTo(1);
        await Assert.That(published[0].Key).IsEqualTo(final.Key);
    }

    [Test]
    public async Task OrganizationPolicyPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateOrganizationPolicyCommandHandler(_adminContext, _service, _unitOfWork);

        await handler.Handle(new UpdateOrganizationPolicyCommand
        {
            UserId = UserId,
            Patch = new PatchOrganizationPolicyDto { RequireOrganizationVerification = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyOrganizationPolicyPatchAsync(
            Arg.Is<PatchOrganizationPolicyDto>(patch => patch.RequireOrganizationVerification.HasValue),
            Arg.Is<OrganizationPolicyDto>(settings => !settings.RequireOrganizationVerification && settings.AllowOrganizationSelfRegistration),
            UserId);
    }

    [Test]
    public async Task BrandingPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var deploymentMode = Substitute.For<IDeploymentModeProvider>();
        deploymentMode.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(false);
        var provisioning = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        var handler = new UpdateBrandingSettingsCommandHandler(_adminContext, _service, deploymentMode, provisioning, _unitOfWork, _mediator);

        await handler.Handle(new UpdateBrandingSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchBrandingSettingsDto { DefaultBrandLogoUrl = OptionalUpdate<string?>.Set("https://new.example/logo.svg") }
        }, CancellationToken.None);

        await _service.Received(1).ApplyBrandingSettingsPatchAsync(
            Arg.Is<PatchBrandingSettingsDto>(patch => patch.DefaultBrandLogoUrl.HasValue),
            Arg.Is<BrandingSettingsDto>(settings => settings.DefaultBrandLogoUrl == "https://new.example/logo.svg" && settings.DefaultBrandDisplayName == "Current brand"),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BrandingPatch_WhenOnlyLogoIsProvided_DoesNotProvisionSingleTenantBrandingDocument()
    {
        var deploymentMode = Substitute.For<IDeploymentModeProvider>();
        deploymentMode.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(true);
        var provisioning = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        var handler = new UpdateBrandingSettingsCommandHandler(_adminContext, _service, deploymentMode, provisioning, _unitOfWork, _mediator);

        await handler.Handle(new UpdateBrandingSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchBrandingSettingsDto
            {
                DefaultBrandLogoUrl = OptionalUpdate<string?>.Set("https://new.example/logo.svg")
            }
        }, CancellationToken.None);

        await provisioning.DidNotReceive().EnsureTenantBrandingDocumentAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DomainPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateDomainSettingsCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateDomainSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchDomainSettingsDto { AdminHost = OptionalUpdate<string?>.Set("admin.new.example") }
        }, CancellationToken.None);

        await _service.Received(1).ApplyDomainSettingsPatchAsync(
            Arg.Is<PatchDomainSettingsDto>(patch => patch.AdminHost.HasValue),
            Arg.Is<DomainSettingsDto>(settings => settings.AdminHost == "admin.new.example" && settings.InstanceBaseDomain == "current.example"),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StorageS3Patch_WhenCredentialsAreRedacted_PreservesPersistedSecrets()
    {
        var storageService = Substitute.For<IInstanceStorageSettingService>();
        var s3Resolver = Substitute.For<IS3ConfigResolver>();
        storageService.ReadSettingsAsync(Arg.Any<CancellationToken>()).Returns(new InstanceStorageSettingsDto
        {
            Provider = "s3_compatible",
            DefaultMaxUploadBytes = 10,
            DefaultTenantQuotaBytes = 100,
            InstanceMaxUploadBytes = 100,
            S3Endpoint = "https://old.example",
            S3AccessKeyId = "persisted-access",
            S3SecretAccessKey = "persisted-secret",
            S3UploadUrlExpirationMinutes = 60
        });
        var handler = new UpdateInstanceStorageSettingsCommandHandler(_adminContext, storageService, s3Resolver);

        var result = await handler.Handle(new UpdateInstanceStorageSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchInstanceStorageSettingsDto
            {
                S3Configuration = OptionalUpdate<InstanceS3ConfigurationWriteDto>.Set(new InstanceS3ConfigurationWriteDto
                {
                    Endpoint = "https://new.example",
                    AccessKeyId = string.Empty,
                    SecretAccessKey = string.Empty,
                    UploadUrlExpirationMinutes = 60
                })
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await storageService.Received(1).ApplySettingsAsync(
            Arg.Is<InstanceStorageSettingsDto>(settings =>
                settings.S3Endpoint == "https://new.example" &&
                settings.S3AccessKeyId == "persisted-access" &&
                settings.S3SecretAccessKey == "persisted-secret"),
            Arg.Is<PatchInstanceStorageSettingsDto>(patch => patch.S3Configuration.HasValue));
    }

    [Test]
    public async Task StoragePatch_WhenMergedCandidateIsInvalid_DoesNotWrite()
    {
        var storageService = Substitute.For<IInstanceStorageSettingService>();
        var s3Resolver = Substitute.For<IS3ConfigResolver>();
        storageService.ReadSettingsAsync(Arg.Any<CancellationToken>()).Returns(new InstanceStorageSettingsDto
        {
            Provider = "local",
            DefaultMaxUploadBytes = 10,
            DefaultTenantQuotaBytes = 100,
            InstanceMaxUploadBytes = 100
        });
        var handler = new UpdateInstanceStorageSettingsCommandHandler(_adminContext, storageService, s3Resolver);

        var result = await handler.Handle(new UpdateInstanceStorageSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchInstanceStorageSettingsDto
            {
                Policy = OptionalUpdate<InstanceStoragePolicyWriteDto>.Set(new InstanceStoragePolicyWriteDto
                {
                    Provider = "unsupported",
                    DefaultMaxUploadBytes = 10,
                    DefaultTenantQuotaBytes = 100,
                    InstanceMaxUploadBytes = 100
                })
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await storageService.DidNotReceive().ApplySettingsAsync(
            Arg.Any<InstanceStorageSettingsDto>(),
            Arg.Any<PatchInstanceStorageSettingsDto>());
        s3Resolver.DidNotReceive().InvalidateCache();
    }

    [Test]
    public async Task ResolverPatch_WhenOneLeafIsProvided_PassesPatchAndPreservesSibling()
    {
        var resolver = Substitute.For<IResolverConfigService>();
        resolver.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(new ResolverConfigurationDto
        {
            HeaderEnabled = true,
            SubdomainEnabled = true,
            CustomDomainEnabled = false,
            PathEnabled = true,
            PathPrefix = "/current",
            InstanceBaseDomain = "current.example",
            AllowTenantCustomDomains = true
        });
        var handler = new UpdateResolverConfigurationCommandHandler(_adminContext, resolver);
        var patch = new PatchResolverConfigurationDto
        {
            PathPrefix = OptionalUpdate<string?>.Set("/new")
        };

        var result = await handler.Handle(new UpdateResolverConfigurationCommand
        {
            UserId = UserId,
            Patch = patch
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await resolver.Received(1).ApplyConfigurationAsync(
            Arg.Is<PatchResolverConfigurationDto>(value => value.PathPrefix.HasValue && value.PathPrefix.Value == "/new"),
            Arg.Is<ResolverConfigurationDto>(value =>
                value.PathPrefix == "/new" &&
                value.SubdomainEnabled &&
                value.InstanceBaseDomain == "current.example"),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolverPatch_WhenMergedCandidateIsInvalid_DoesNotWrite()
    {
        var resolver = Substitute.For<IResolverConfigService>();
        resolver.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(new ResolverConfigurationDto
        {
            HeaderEnabled = true,
            PathEnabled = true,
            PathPrefix = "/current"
        });
        var handler = new UpdateResolverConfigurationCommandHandler(_adminContext, resolver);

        var result = await handler.Handle(new UpdateResolverConfigurationCommand
        {
            UserId = UserId,
            Patch = new PatchResolverConfigurationDto
            {
                PathPrefix = OptionalUpdate<string?>.Set("invalid/")
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await resolver.DidNotReceive().ApplyConfigurationAsync(
            Arg.Any<PatchResolverConfigurationDto>(),
            Arg.Any<ResolverConfigurationDto>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantDelegationPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateTenantDelegationSettingsCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateTenantDelegationSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchTenantDelegationSettingsDto { LockTenantStorage = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyTenantDelegationSettingsPatchAsync(
            false,
            Arg.Is<PatchTenantDelegationSettingsDto>(patch => patch.LockTenantStorage.HasValue),
            Arg.Is<TenantDelegationSettingsDto>(settings => !settings.LockTenantStorage && settings.LockTenantSmtp),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantDelegationHomepageLockPatch_PublishesCommittedNotificationWithRequestToken()
    {
        var notification = new SettingChangedNotification(
            GovernanceSettingKeys.Routing.DefaultPublicHomePage,
            "\"EventList\"",
            "\"EventList\"",
            SettingSource.SystemLocked,
            null,
            UserId,
            DateTime.UtcNow);
        _service.ApplyTenantDelegationSettingsPatchAsync(
                Arg.Any<bool>(),
                Arg.Any<PatchTenantDelegationSettingsDto>(),
                Arg.Any<TenantDelegationSettingsDto>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SettingChangedNotification>>([notification]));
        var handler = new UpdateTenantDelegationSettingsCommandHandler(_adminContext, _service, _unitOfWork, _mediator);
        using var requestCancellation = new CancellationTokenSource();

        await handler.Handle(new UpdateTenantDelegationSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchTenantDelegationSettingsDto
            {
                LockTenantHomePagePreference = OptionalUpdate<bool>.Set(true)
            }
        }, requestCancellation.Token);

        await _mediator.Received(1).Publish(notification, requestCancellation.Token);
    }

    [Test]
    public async Task AdminPortalPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateAdminPortalSettingsCommandHandler(_adminContext, _service, _unitOfWork);

        await handler.Handle(new UpdateAdminPortalSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchAdminPortalSettingsDto { Enabled = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyAdminPortalSettingsPatchAsync(
            Arg.Is<PatchAdminPortalSettingsDto>(patch => patch.Enabled.HasValue),
            Arg.Is<AdminPortalSettingsDto>(settings => !settings.Enabled && settings.PublicUrl == "https://admin.current.example"),
            UserId);
    }

    [Test]
    public async Task AiAssistantPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateAiAssistantGovernanceSettingsCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateAiAssistantGovernanceSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchAiAssistantGovernanceSettingsDto { ToolProposalsEnabled = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyAiAssistantGovernanceSettingsPatchAsync(
            Arg.Is<PatchAiAssistantGovernanceSettingsDto>(patch => patch.ToolProposalsEnabled.HasValue),
            Arg.Is<AiAssistantGovernanceSettingsDto>(settings => !settings.ToolProposalsEnabled && settings.ModelId == "current-model"),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AiAssistantProviderPatch_ReplacesNonBlankApiKeyAsOneCoupledGroup()
    {
        var handler = new UpdateAiAssistantGovernanceSettingsCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateAiAssistantGovernanceSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchAiAssistantGovernanceSettingsDto
            {
                ProviderConfiguration = OptionalUpdate<AiAssistantProviderConfigurationWriteDto>.Set(new()
                {
                    Provider = "openai-compatible",
                    EndpointUrl = "https://ai.example.test/v1",
                    ApiKey = "replacement-key",
                    ModelId = "model-a",
                    AllowedModelIds = ["model-a"]
                })
            }
        }, CancellationToken.None);

        await _service.Received(1).ApplyAiAssistantGovernanceSettingsPatchAsync(
            Arg.Is<PatchAiAssistantGovernanceSettingsDto>(patch => patch.ProviderConfiguration.HasValue),
            Arg.Is<AiAssistantGovernanceSettingsDto>(settings => settings.ApiKey == "replacement-key"
                && settings.Provider == "openai-compatible"
                && settings.ModelId == "model-a"),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AiAssistantProviderPatch_WhenApiKeyIsBlank_PreservesConfiguredKey()
    {
        var handler = new UpdateAiAssistantGovernanceSettingsCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateAiAssistantGovernanceSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchAiAssistantGovernanceSettingsDto
            {
                ProviderConfiguration = OptionalUpdate<AiAssistantProviderConfigurationWriteDto>.Set(new()
                {
                    Provider = "openai-compatible",
                    EndpointUrl = "https://ai.example.test/v1",
                    ApiKey = string.Empty,
                    ModelId = "model-a",
                    AllowedModelIds = ["model-a"]
                })
            }
        }, CancellationToken.None);

        await _service.Received(1).ApplyAiAssistantGovernanceSettingsPatchAsync(
            Arg.Is<PatchAiAssistantGovernanceSettingsDto>(patch => patch.ProviderConfiguration.HasValue),
            Arg.Is<AiAssistantGovernanceSettingsDto>(settings => settings.ApiKey == "current-key"),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task McpPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateMcpGovernanceSettingsCommandHandler(_adminContext, _service, _unitOfWork, _mediator);

        await handler.Handle(new UpdateMcpGovernanceSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchMcpGovernanceSettingsDto { Enabled = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyMcpGovernanceSettingsPatchAsync(
            Arg.Is<PatchMcpGovernanceSettingsDto>(patch => patch.Enabled.HasValue),
            Arg.Is<McpGovernanceSettingsDto>(settings => !settings.Enabled && settings.EnableLegacySse),
            UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BrandingLockPatch_WhenProvisioningFails_PublishesNothing()
    {
        var deploymentMode = Substitute.For<IDeploymentModeProvider>();
        deploymentMode.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(true);
        var provisioning = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        provisioning.EnsureTenantBrandingDocumentAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Explore.Domain.Settings.Documents.TenantSettingsDocument>(
                new InvalidOperationException("provisioning failed")));
        _service.ApplyBrandingSettingsPatchAsync(
                Arg.Any<PatchBrandingSettingsDto>(),
                Arg.Any<BrandingSettingsDto>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SettingChangedNotification>>([
                new(GovernanceSettingKeys.Branding.DisplayName, "\"Current brand\"", "\"Current brand\"", SettingSource.SystemLocked, null, UserId, DateTime.UtcNow)
            ]));
        var handler = new UpdateBrandingSettingsCommandHandler(
            _adminContext,
            _service,
            deploymentMode,
            provisioning,
            _unitOfWork,
            _mediator);
        Exception? exception = null;

        try
        {
            await handler.Handle(new UpdateBrandingSettingsCommand
            {
                UserId = UserId,
                Patch = new PatchBrandingSettingsDto
                {
                    DefaultBrandDisplayName = OptionalUpdate<string?>.Set("Replacement"),
                    LockTenantBrandDisplayName = OptionalUpdate<bool>.Set(true)
                }
            }, CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        await Assert.That(exception).IsNotNull();
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RenderPolicyPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateRenderPolicySettingsCommandHandler(_adminContext, _service, _unitOfWork);

        await handler.Handle(new UpdateRenderPolicySettingsCommand
        {
            UserId = UserId,
            Patch = new PatchRenderPolicySettingsDto { GlobalPrerenderEnabled = OptionalUpdate<bool>.Set(true) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyRenderPolicySettingsPatchAsync(
            Arg.Is<PatchRenderPolicySettingsDto>(patch => patch.GlobalPrerenderEnabled.HasValue),
            Arg.Is<RenderPolicySettingsDto>(settings => settings.GlobalPrerenderEnabled && settings.PublicSeoRenderMode == "InteractiveAuto"),
            UserId);
    }

    [Test]
    public async Task RenderPolicyPatch_WhenMergedCandidateIsInvalid_DoesNotWrite()
    {
        var handler = new UpdateRenderPolicySettingsCommandHandler(_adminContext, _service, _unitOfWork);

        var result = await handler.Handle(new UpdateRenderPolicySettingsCommand
        {
            UserId = UserId,
            Patch = new PatchRenderPolicySettingsDto { RenderPolicyPreset = OptionalUpdate<string?>.Set("CustomAdvanced") }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        await _service.DidNotReceive().ApplyRenderPolicySettingsPatchAsync(
            Arg.Any<PatchRenderPolicySettingsDto>(), Arg.Any<RenderPolicySettingsDto>(), Arg.Any<Guid?>());
    }

    [Test]
    public async Task ModulePatch_WhenEmpty_DoesNotReadOrWrite()
    {
        var handler = new UpdateModuleSettingsCommandHandler(
            _adminContext,
            _service,
            _deploymentModeProvider,
            _unitOfWork);

        var result = await handler.Handle(new UpdateModuleSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchModuleSettingsDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _service.DidNotReceive().ReadSettingsAsync();
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    private static InstanceGovernanceSettings CreateSettings() => new()
    {
        DeploymentMode = new DeploymentModeDto { Mode = DeploymentMode.SingleTenant },
        Modules = new ModuleSettingsDto { EnableIslamicModule = true, EnableTechModule = true },
        EventPolicy = new EventPolicyDto
        {
            AllowUserSubmittedEvents = true,
            AllowOrganizationSubmittedEvents = true,
            AllowGroupSubmittedEvents = true,
            EventCardClickOpensDetailPage = true
        },
        OrganizationPolicy = new OrganizationPolicyDto
        {
            RequireOrganizationVerification = true,
            AllowOrganizationSelfRegistration = true,
            AllowGroupSelfRegistration = true
        },
        Branding = new BrandingSettingsDto { DefaultBrandDisplayName = "Current brand", DefaultBrandLogoUrl = "https://current.example/logo.svg" },
        Domains = new DomainSettingsDto { InstanceBaseDomain = "current.example", AdminHost = "admin.current.example" },
        TenantDelegation = new TenantDelegationSettingsDto { LockTenantSmtp = true, LockTenantStorage = true },
        AdminPortal = new AdminPortalSettingsDto { Enabled = true, PublicUrl = "https://admin.current.example" },
        AiAssistant = new AiAssistantGovernanceSettingsDto { ApiKey = "current-key", ModelId = "current-model", ToolProposalsEnabled = true },
        Mcp = new McpGovernanceSettingsDto { Enabled = true, EnableLegacySse = true },
        RenderPolicy = new RenderPolicySettingsDto
        {
            RenderPolicyPreset = "SeoBalanced",
            EnableAdvancedRenderPolicyOverrides = false,
            GlobalRenderMode = "InteractiveAuto",
            PublicSeoRenderMode = "InteractiveAuto",
            OperationalRenderMode = "InteractiveAuto",
            AdminRenderMode = "InteractiveAuto",
            OnboardingRenderMode = "InteractiveAuto"
        }
    };
}
