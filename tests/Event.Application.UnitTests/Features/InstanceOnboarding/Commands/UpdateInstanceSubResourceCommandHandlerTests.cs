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
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public sealed class UpdateInstanceSubResourceCommandHandlerTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly IInstanceGovernanceSettingService _service = Substitute.For<IInstanceGovernanceSettingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public UpdateInstanceSubResourceCommandHandlerTests()
    {
        _adminContext.IsInstanceAdminAsync(UserId, Arg.Any<CancellationToken>()).Returns(true);
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _service.ReadSettingsAsync().Returns(CreateSettings());
    }

    [Test]
    public async Task ModulePatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateModuleSettingsCommandHandler(_adminContext, _service, _unitOfWork);

        var result = await handler.Handle(new UpdateModuleSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchModuleSettingsDto { EnableIslamicModule = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _service.Received(1).ApplyModuleSettingsAsync(null,
            Arg.Is<ModuleSettingsDto>(settings => !settings.EnableIslamicModule && settings.EnableTechModule), UserId);
    }

    [Test]
    public async Task EventPolicyPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateEventPolicyCommandHandler(_adminContext, _service, _unitOfWork);

        await handler.Handle(new UpdateEventPolicyCommand
        {
            UserId = UserId,
            Patch = new PatchEventPolicyDto { AllowUserSubmittedEvents = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyEventPolicyAsync(
            Arg.Is<EventPolicyDto>(settings => !settings.AllowUserSubmittedEvents && settings.AllowOrganizationSubmittedEvents), UserId);
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

        await _service.Received(1).ApplyOrganizationPolicyAsync(
            Arg.Is<OrganizationPolicyDto>(settings => !settings.RequireOrganizationVerification && settings.AllowOrganizationSelfRegistration), UserId);
    }

    [Test]
    public async Task BrandingPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var deploymentMode = Substitute.For<IDeploymentModeProvider>();
        deploymentMode.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(false);
        var provisioning = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        var handler = new UpdateBrandingSettingsCommandHandler(_adminContext, _service, deploymentMode, provisioning, _unitOfWork);

        await handler.Handle(new UpdateBrandingSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchBrandingSettingsDto { DefaultBrandLogoUrl = OptionalUpdate<string?>.Set("https://new.example/logo.svg") }
        }, CancellationToken.None);

        await _service.Received(1).ApplyBrandingSettingsAsync(
            Arg.Is<BrandingSettingsDto>(settings => settings.DefaultBrandLogoUrl == "https://new.example/logo.svg" && settings.DefaultBrandDisplayName == "Current brand"), UserId);
    }

    [Test]
    public async Task DomainPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateDomainSettingsCommandHandler(_adminContext, _service, _unitOfWork);

        await handler.Handle(new UpdateDomainSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchDomainSettingsDto { AdminHost = OptionalUpdate<string?>.Set("admin.new.example") }
        }, CancellationToken.None);

        await _service.Received(1).ApplyDomainSettingsAsync(
            Arg.Is<DomainSettingsDto>(settings => settings.AdminHost == "admin.new.example" && settings.InstanceBaseDomain == "current.example"), UserId);
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
        await storageService.Received(1).ApplySettingsAsync(Arg.Is<InstanceStorageSettingsDto>(settings =>
            settings.S3Endpoint == "https://new.example" &&
            settings.S3AccessKeyId == "persisted-access" &&
            settings.S3SecretAccessKey == "persisted-secret"));
    }

    [Test]
    public async Task TenantDelegationPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateTenantDelegationSettingsCommandHandler(_adminContext, _service, _unitOfWork);

        await handler.Handle(new UpdateTenantDelegationSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchTenantDelegationSettingsDto { LockTenantStorage = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyTenantDelegationSettingsAsync(
            Arg.Is<TenantDelegationSettingsDto>(settings => !settings.LockTenantStorage && settings.LockTenantSmtp), UserId);
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

        await _service.Received(1).ApplyAdminPortalSettingsAsync(
            Arg.Is<AdminPortalSettingsDto>(settings => !settings.Enabled && settings.PublicUrl == "https://admin.current.example"), UserId);
    }

    [Test]
    public async Task AiAssistantPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateAiAssistantGovernanceSettingsCommandHandler(_adminContext, _service, _unitOfWork);

        await handler.Handle(new UpdateAiAssistantGovernanceSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchAiAssistantGovernanceSettingsDto { ToolProposalsEnabled = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyAiAssistantGovernanceSettingsAsync(
            Arg.Is<AiAssistantGovernanceSettingsDto>(settings => !settings.ToolProposalsEnabled && settings.ModelId == "current-model"), UserId);
    }

    [Test]
    public async Task AiAssistantProviderPatch_ReplacesNonBlankApiKeyAsOneCoupledGroup()
    {
        var handler = new UpdateAiAssistantGovernanceSettingsCommandHandler(_adminContext, _service, _unitOfWork);

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

        await _service.Received(1).ApplyAiAssistantGovernanceSettingsAsync(
            Arg.Is<AiAssistantGovernanceSettingsDto>(settings => settings.ApiKey == "replacement-key"
                && settings.Provider == "openai-compatible"
                && settings.ModelId == "model-a"), UserId);
    }

    [Test]
    public async Task AiAssistantProviderPatch_WhenApiKeyIsBlank_PreservesConfiguredKey()
    {
        var handler = new UpdateAiAssistantGovernanceSettingsCommandHandler(_adminContext, _service, _unitOfWork);

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

        await _service.Received(1).ApplyAiAssistantGovernanceSettingsAsync(
            Arg.Is<AiAssistantGovernanceSettingsDto>(settings => settings.ApiKey == "current-key"), UserId);
    }

    [Test]
    public async Task McpPatch_WhenOneLeafProvided_PreservesSibling()
    {
        var handler = new UpdateMcpGovernanceSettingsCommandHandler(_adminContext, _service, _unitOfWork);

        await handler.Handle(new UpdateMcpGovernanceSettingsCommand
        {
            UserId = UserId,
            Patch = new PatchMcpGovernanceSettingsDto { Enabled = OptionalUpdate<bool>.Set(false) }
        }, CancellationToken.None);

        await _service.Received(1).ApplyMcpGovernanceSettingsAsync(
            Arg.Is<McpGovernanceSettingsDto>(settings => !settings.Enabled && settings.EnableLegacySse), UserId);
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

        await _service.Received(1).ApplyRenderPolicySettingsAsync(
            Arg.Is<RenderPolicySettingsDto>(settings => settings.GlobalPrerenderEnabled && settings.PublicSeoRenderMode == "InteractiveAuto"), UserId);
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
        await _service.DidNotReceive().ApplyRenderPolicySettingsAsync(Arg.Any<RenderPolicySettingsDto>(), Arg.Any<Guid?>());
    }

    [Test]
    public async Task ModulePatch_WhenEmpty_DoesNotReadOrWrite()
    {
        var handler = new UpdateModuleSettingsCommandHandler(_adminContext, _service, _unitOfWork);

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
