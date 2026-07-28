// ABOUTME: Unit tests for InstanceGovernanceSettingService runtime render-policy defaults and persistence behavior.
// ABOUTME: Verifies batch resolution via IHierarchicalSettingsResolver, safe defaults, and normalization.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public class InstanceGovernanceSettingServiceTests
{
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly SettingUpsertService _upsertService;
    private readonly IModuleCapabilityService _moduleCapabilityService;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IMediator _mediator;
    private readonly InstanceGovernanceSettingService _service;

    public InstanceGovernanceSettingServiceTests()
    {
        _resolver = Substitute.For<IHierarchicalSettingsResolver>();
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        _mediator = Substitute.For<IMediator>();
        _upsertService = new SettingUpsertService(_systemSettingRepository, _mediator);
        _moduleCapabilityService = Substitute.For<IModuleCapabilityService>();
        var logger = Substitute.For<ILogger<InstanceGovernanceSettingService>>();

        _service = new InstanceGovernanceSettingService(
            _resolver,
            _upsertService,
            _moduleCapabilityService,
            logger);
    }

    [Test]
    public async Task UpsertLockAsync_WhenSettingExists_ReturnsSystemLockedNotificationWithoutPublishing()
    {
        const string key = GovernanceSettingKeys.Events.CardClickOpensDetailPage;
        const string fallbackValue = "true";
        const string persistedValue = "false";
        var actorId = Guid.NewGuid();
        var definition = Explore.Domain.Settings.SettingRegistry.Get(key)
            ?? throw new InvalidOperationException($"Missing setting definition for {key}.");
        var lockWrites = CaptureLockWrites(persistedValue);

        SettingChangedNotification notification = await _upsertService.UpsertLockAsync(
            key,
            fallbackValue,
            true,
            actorId,
            CancellationToken.None);

        await Assert.That(lockWrites.Count).IsEqualTo(1);
        var candidate = lockWrites.Single();
        await Assert.That(candidate.SettingKey).IsEqualTo(key);
        await Assert.That(candidate.Value).IsEqualTo(fallbackValue);
        await Assert.That(candidate.ValueType).IsEqualTo(definition.ValueType);
        await Assert.That(candidate.AllowedValues).IsEqualTo(
            definition.AllowedValues is null ? null : SettingValueSerializer.Serialize(definition.AllowedValues));
        await Assert.That(candidate.Description).IsEqualTo(definition.Description);
        await Assert.That(candidate.Category).IsEqualTo(definition.Category);
        await Assert.That(candidate.DisplayOrder).IsEqualTo(0);
        await Assert.That(candidate.IsLocked).IsTrue();
        await Assert.That(candidate.CreatedBy).IsEqualTo(actorId);
        await Assert.That(candidate.UpdatedBy).IsEqualTo(actorId);
        await _systemSettingRepository.DidNotReceive().GetByKey(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await Assert.That(notification.Key).IsEqualTo(key);
        await Assert.That(notification.OldValue).IsEqualTo(persistedValue);
        await Assert.That(notification.NewValue).IsEqualTo(persistedValue);
        await Assert.That(notification.Scope).IsEqualTo(SettingSource.SystemLocked);
        await Assert.That(notification.ActorUserId).IsEqualTo(actorId);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpsertLockAsync_WhenSettingIsMissingAndUnlocked_ReturnsSystemDefaultNotificationWithoutPublishing()
    {
        const string key = GovernanceSettingKeys.Events.CardClickOpensDetailPage;
        const string fallbackValue = "true";
        var actorId = Guid.NewGuid();
        var definition = Explore.Domain.Settings.SettingRegistry.Get(key)
            ?? throw new InvalidOperationException($"Missing setting definition for {key}.");
        var lockWrites = CaptureLockWrites(persistedValue: null);

        SettingChangedNotification notification = await _upsertService.UpsertLockAsync(
            key,
            fallbackValue,
            false,
            actorId,
            CancellationToken.None);

        await Assert.That(lockWrites.Count).IsEqualTo(1);
        var candidate = lockWrites.Single();
        await Assert.That(candidate.SettingKey).IsEqualTo(key);
        await Assert.That(candidate.Value).IsEqualTo(fallbackValue);
        await Assert.That(candidate.ValueType).IsEqualTo(definition.ValueType);
        await Assert.That(candidate.AllowedValues).IsEqualTo(
            definition.AllowedValues is null ? null : SettingValueSerializer.Serialize(definition.AllowedValues));
        await Assert.That(candidate.Description).IsEqualTo(definition.Description);
        await Assert.That(candidate.Category).IsEqualTo(definition.Category);
        await Assert.That(candidate.DisplayOrder).IsEqualTo(0);
        await Assert.That(candidate.IsLocked).IsFalse();
        await Assert.That(candidate.CreatedBy).IsEqualTo(actorId);
        await Assert.That(candidate.UpdatedBy).IsEqualTo(actorId);
        await _systemSettingRepository.DidNotReceive().GetByKey(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await Assert.That(notification.Key).IsEqualTo(key);
        await Assert.That(notification.OldValue).IsNull();
        await Assert.That(notification.NewValue).IsEqualTo(fallbackValue);
        await Assert.That(notification.Scope).IsEqualTo(SettingSource.SystemDefault);
        await Assert.That(notification.ActorUserId).IsEqualTo(actorId);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadSettingsAsync_WhenRenderPolicySettingsMissing_ReturnsSafeDefaults()
    {
        SetupEmptyBatchResolve();

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.RenderPolicy.RenderPolicyVersion).IsEqualTo(1);
        await Assert.That(result.RenderPolicy.RenderPolicyPreset).IsEqualTo("AllInteractiveServer");
        await Assert.That(result.RenderPolicy.PublicSeoRenderMode).IsEqualTo("InteractiveServer");
        await Assert.That(result.RenderPolicy.PublicSeoPrerenderEnabled).IsFalse();
        await Assert.That(result.RenderPolicy.OnboardingRenderMode).IsEqualTo("InteractiveAuto");
        await Assert.That(result.RenderPolicy.DisallowInteractiveServerOnOnboarding).IsTrue();
    }

    [Test]
    public async Task ReadSettingsAsync_UsesResolverBatchLoading_NotIndividualQueries()
    {
        SetupEmptyBatchResolve();

        await _service.ReadSettingsAsync();

        await _resolver.Received(1).ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySettingsAsync_WhenOnboardingInteractiveServerProvided_NormalizesAndPersistsGuardrail()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var settings = CreateValidSettings();
        settings.RenderPolicy.OnboardingRenderMode = "InteractiveServer";
        settings.RenderPolicy.DisallowInteractiveServerOnOnboarding = true;

        await _service.ApplySettingsAsync(Guid.NewGuid(), settings, Guid.NewGuid());

        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(
            s => s.SettingKey == GovernanceSettingKeys.Routing.RenderPolicy.Onboarding.RenderMode
                 && s.Value == "\"InteractiveAuto\""), Arg.Any<CancellationToken>());

        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(
            s => s.SettingKey == GovernanceSettingKeys.Routing.RenderPolicy.DisallowInteractiveServerOnOnboarding
                 && s.Value == "true"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySettingsAsync_InSingleTenantMode_UpsertsDefaultPublicHomePageOnlyOnce()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        var settings = CreateValidSettings();

        await _service.ApplySettingsAsync(Guid.NewGuid(), settings, Guid.NewGuid());

        await _systemSettingRepository.Received(1).UpsertAsync(Arg.Is<SystemSetting>(
            s => s.SettingKey == GovernanceSettingKeys.Routing.DefaultPublicHomePage
                 && s.Value == "\"EventList\""), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySettingsAsync_LocationPrivacy_DoesNotPublishBeforeTransactionOwnerCommits()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);

        InstanceGovernanceSettings settings = CreateValidSettings();

        InstanceGovernanceSettingApplyResult result =
            await _service.ApplySettingsAsync(Guid.NewGuid(), settings, Guid.NewGuid());

        string[] locationPrivacyKeys =
        [
            GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations,
            GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress,
            GovernanceSettingKeys.LocationPrivacy.AllowPublicCoordinates,
            GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience,
            GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset
        ];
        int publishedLocationChanges = _mediator.ReceivedCalls()
            .Count(call => call.GetArguments().FirstOrDefault() is
                Explore.Application.Notifications.SettingChangedNotification notification &&
                locationPrivacyKeys.Contains(notification.Key, StringComparer.Ordinal));

        await Assert.That(publishedLocationChanges).IsEqualTo(0);
        await Assert.That(result.DeferredNotifications.Select(notification => notification.Key))
            .IsEquivalentTo(locationPrivacyKeys);
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenOverrideDisabled_ReturnsInstanceSettings()
    {
        SetupEmptyBatchResolve();

        var tenantId = Guid.NewGuid();
        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicy.RenderPolicyPreset).IsEqualTo("AllInteractiveServer");
        await Assert.That(result.RenderPolicy.AllowTenantRenderPolicyOverride).IsFalse();
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenOverrideEnabled_AppliesTenantPreset()
    {
        var instanceResolved = CreateResolvedSettingsWithOverrideEnabled();
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId == null),
            Arg.Any<CancellationToken>())
            .Returns(instanceResolved);

        var tenantResolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.Preset, "\"SeoBalanced\"")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId != null),
            Arg.Any<CancellationToken>())
            .Returns(tenantResolved);

        var tenantId = Guid.NewGuid();
        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicy.RenderPolicyPreset).IsEqualTo("SeoBalanced");
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenRouteGroupLocked_IgnoresTenantOverride()
    {
        var instanceResolved = CreateResolvedSettingsWithOverrideEnabled();
        instanceResolved.Add(CreateResolvedSetting(
            GovernanceSettingKeys.Routing.RenderPolicy.LockTenantPublicSeo, "true"));
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId == null),
            Arg.Any<CancellationToken>())
            .Returns(instanceResolved);

        var tenantResolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.PublicSeo.RenderMode, "\"InteractiveWebAssembly\"")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId != null),
            Arg.Any<CancellationToken>())
            .Returns(tenantResolved);

        var tenantId = Guid.NewGuid();
        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicy.PublicSeoRenderMode).IsEqualTo("InteractiveServer");
    }

    [Test]
    public async Task ReadEffectiveSettingsForTenantAsync_WhenRouteGroupUnlocked_AppliesTenantOverride()
    {
        var instanceResolved = CreateResolvedSettingsWithOverrideEnabled();
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId == null),
            Arg.Any<CancellationToken>())
            .Returns(instanceResolved);

        var tenantResolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.Operational.RenderMode, "\"InteractiveWebAssembly\"")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<SettingContext>(c => c.TenantId != null),
            Arg.Any<CancellationToken>())
            .Returns(tenantResolved);

        var tenantId = Guid.NewGuid();
        var result = await _service.ReadEffectiveSettingsForTenantAsync(tenantId);

        await Assert.That(result.RenderPolicy.OperationalRenderMode).IsEqualTo("InteractiveWebAssembly");
    }

    [Test]
    public async Task ReadSettingsAsync_ReadsNewLockFields()
    {
        var resolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.AllowTenantOverride, "true"),
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantPublicSeo, "true")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>())
            .Returns(resolved);

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.RenderPolicy.AllowTenantRenderPolicyOverride).IsTrue();
        await Assert.That(result.RenderPolicy.LockTenantPublicSeoRenderPolicy).IsTrue();
        await Assert.That(result.RenderPolicy.LockTenantOperationalRenderPolicy).IsFalse();
        await Assert.That(result.RenderPolicy.LockTenantAdminRenderPolicy).IsFalse();
    }

    [Test]
    public async Task ReadSettingsAsync_ReadsAdminPortalSettings()
    {
        var resolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.AdminPortal.Enabled, "false"),
            CreateResolvedSetting(GovernanceSettingKeys.AdminPortal.PublicUrl, "\"https://admin.example.org\""),
            CreateResolvedSetting(GovernanceSettingKeys.AdminPortal.AllowTenantAdminAccess, "true")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>())
            .Returns(resolved);

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.AdminPortal.Enabled).IsFalse();
        await Assert.That(result.AdminPortal.PublicUrl).IsEqualTo("https://admin.example.org");
        await Assert.That(result.AdminPortal.AllowTenantAdminAccess).IsTrue();
    }

    [Test]
    public async Task ApplyAdminPortalSettingsAsync_UpsertsNormalizedAdminPortalKeys()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        var actorId = Guid.NewGuid();

        await _service.ApplyAdminPortalSettingsAsync(new AdminPortalSettingsDto
        {
            Enabled = false,
            PublicUrl = "  https://admin.example.org  ",
            AllowTenantAdminAccess = true
        }, actorId);

        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(s =>
            s.SettingKey == GovernanceSettingKeys.AdminPortal.Enabled && s.Value == "false"), Arg.Any<CancellationToken>());
        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(s =>
            s.SettingKey == GovernanceSettingKeys.AdminPortal.PublicUrl && s.Value == "\"https://admin.example.org\""), Arg.Any<CancellationToken>());
        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(s =>
            s.SettingKey == GovernanceSettingKeys.AdminPortal.AllowTenantAdminAccess && s.Value == "true"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyModuleSettingsPatchAsync_WhenOneLeafIsSupplied_WritesOnlyThatKeyAndForwardsCancellation()
    {
        var writes = CaptureWrites();
        var actorId = Guid.NewGuid();
        using var transactionCancellation = new CancellationTokenSource();

        await _service.ApplyModuleSettingsPatchAsync(
            PlatformDefaults.DefaultTenantId,
            new PatchModuleSettingsDto
            {
                EnableIslamicModule = OptionalUpdate<bool>.Set(false)
            },
            new ModuleSettingsDto { EnableIslamicModule = false, EnableTechModule = true },
            actorId,
            transactionCancellation.Token);

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Modules.IslamicEnabled
        ]);
        await _moduleCapabilityService.Received(1).SyncTenantModuleCapabilityPatchAsync(
            PlatformDefaults.DefaultTenantId,
            enableIslamic: false,
            enableTech: null,
            actorId,
            transactionCancellation.Token);
        await _systemSettingRepository.Received(1).UpsertAsync(
            Arg.Is<SystemSetting>(setting => setting.SettingKey == GovernanceSettingKeys.Modules.IslamicEnabled),
            transactionCancellation.Token);
        await _moduleCapabilityService.DidNotReceive().SyncTenantModuleCapabilitiesAsync(
            Arg.Any<Guid>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyEventPolicyPatchAsync_WhenOneLeafIsSupplied_WritesOnlyThatKey()
    {
        var writes = CaptureWrites();

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyEventPolicyPatchAsync(
            new PatchEventPolicyDto
            {
                AllowUserSubmittedEvents = OptionalUpdate<bool>.Set(false)
            },
            new EventPolicyDto
            {
                AllowUserSubmittedEvents = false,
                AllowOrganizationSubmittedEvents = true,
                AllowGroupSubmittedEvents = true,
                EventCardClickOpensDetailPage = true
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Events.UserSubmissionEnabled
        ]);
        await Assert.That(notifications.Select(notification => notification.Key)).IsEquivalentTo([
            GovernanceSettingKeys.Events.UserSubmissionEnabled
        ]);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyOrganizationPolicyPatchAsync_WhenOneLeafIsSupplied_WritesOnlyThatKey()
    {
        var writes = CaptureWrites();

        await _service.ApplyOrganizationPolicyPatchAsync(
            new PatchOrganizationPolicyDto
            {
                RequireOrganizationVerification = OptionalUpdate<bool>.Set(false)
            },
            new OrganizationPolicyDto
            {
                RequireOrganizationVerification = false,
                AllowTenantToOmitVerification = false,
                AllowOrganizationSelfRegistration = true,
                AllowGroupSelfRegistration = true
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Organizations.VerificationRequired
        ]);
    }

    [Test]
    public async Task ApplyBrandingSettingsPatchAsync_WhenValueAndLockAreSupplied_WritesAndReturnsOneNotification()
    {
        var writes = CaptureWrites();

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyBrandingSettingsPatchAsync(
            new PatchBrandingSettingsDto
            {
                DefaultBrandLogoUrl = OptionalUpdate<string?>.Set("  https://new.example/logo.svg  "),
                LockTenantBrandLogoUrl = OptionalUpdate<bool>.Set(true)
            },
            new BrandingSettingsDto
            {
                DefaultBrandDisplayName = "Current brand",
                DefaultBrandLogoUrl = "  https://new.example/logo.svg  ",
                DefaultBrandFaviconUrl = "https://current.example/favicon.svg",
                DefaultBrandCustomCssUrl = "https://current.example/site.css",
                LockTenantBrandLogoUrl = true
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Branding.LogoUrl
        ]);
        await Assert.That(writes.Single().IsLocked).IsTrue();
        await Assert.That(notifications.Select(notification => notification.Key)).IsEquivalentTo([
            GovernanceSettingKeys.Branding.LogoUrl
        ]);
        await Assert.That(notifications.Single().Scope).IsEqualTo(SettingSource.SystemLocked);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateBrandingSettings_WhenProvisioningFails_DoesNotPublishValueNotification()
    {
        SetupEmptyBatchResolve();
        _systemSettingRepository.UpsertAsync(
                Arg.Any<SystemSetting>(),
                Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var actorId = Guid.NewGuid();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(actorId, Arg.Any<CancellationToken>()).Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.Arg<CancellationToken>()));
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        deploymentModeProvider.IsSingleTenantAsync(Arg.Any<CancellationToken>()).Returns(true);
        var provisioningService = Substitute.For<ITenantBrandingSettingsDocumentProvisioningService>();
        provisioningService.EnsureTenantBrandingDocumentAsync(
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Explore.Domain.Settings.Documents.TenantSettingsDocument>(
                new InvalidOperationException("provisioning failed")));
        var handler = new UpdateBrandingSettingsCommandHandler(
            adminContext,
            _service,
            deploymentModeProvider,
            provisioningService,
            unitOfWork,
            _mediator);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new UpdateBrandingSettingsCommand
            {
                UserId = actorId,
                Patch = new PatchBrandingSettingsDto
                {
                    DefaultBrandDisplayName = OptionalUpdate<string?>.Set("Updated brand")
                }
            },
            CancellationToken.None));

        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyDomainSettingsPatchAsync_WhenOneLeafIsSupplied_WritesOnlyThatKey()
    {
        var writes = CaptureWrites();

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyDomainSettingsPatchAsync(
            new PatchDomainSettingsDto
            {
                AdminHost = OptionalUpdate<string?>.Set("  admin.new.example  ")
            },
            new DomainSettingsDto
            {
                InstanceBaseDomain = "current.example",
                AdminHost = "  admin.new.example  ",
                AllowTenantCustomDomains = true
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Domains.AdminHost
        ]);
        await Assert.That(notifications.Select(notification => notification.Key)).IsEquivalentTo([
            GovernanceSettingKeys.Domains.AdminHost
        ]);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyTenantDelegationSettingsPatchAsync_WhenOneLeafIsSupplied_WritesOnlyThatKey()
    {
        var writes = CaptureWrites();

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyTenantDelegationSettingsPatchAsync(
            false,
            new PatchTenantDelegationSettingsDto
            {
                LockTenantStorage = OptionalUpdate<bool>.Set(false)
            },
            new TenantDelegationSettingsDto
            {
                LockTenantSmtp = true,
                LockTenantStorage = false,
                LockTenantAnalytics = true,
                LockTenantAiAssistant = true
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.TenantDelegation.LockStorage
        ]);
        await Assert.That(notifications.Select(notification => notification.Key)).IsEquivalentTo([
            GovernanceSettingKeys.TenantDelegation.LockStorage
        ]);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyAdminPortalSettingsPatchAsync_WhenOneLeafIsSupplied_WritesOnlyThatKey()
    {
        var writes = CaptureWrites();

        await _service.ApplyAdminPortalSettingsPatchAsync(
            new PatchAdminPortalSettingsDto
            {
                PublicUrl = OptionalUpdate<string?>.Set("  https://admin.new.example  ")
            },
            new AdminPortalSettingsDto
            {
                Enabled = true,
                PublicUrl = "  https://admin.new.example  ",
                AllowTenantAdminAccess = true
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.AdminPortal.PublicUrl
        ]);
    }

    [Test]
    public async Task ApplyAiAssistantGovernanceSettingsPatchAsync_WhenProviderConfigurationIsSupplied_WritesOnlyItsKeys()
    {
        var writes = CaptureWrites();

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyAiAssistantGovernanceSettingsPatchAsync(
            new PatchAiAssistantGovernanceSettingsDto
            {
                ProviderConfiguration = OptionalUpdate<AiAssistantProviderConfigurationWriteDto>.Set(new()
                {
                    Provider = "openai-compatible",
                    EndpointUrl = "https://ai.example.test/v1",
                    ApiKey = "replacement-key",
                    ModelId = "model-a",
                    AllowedModelIds = ["model-a"]
                })
            },
            new AiAssistantGovernanceSettingsDto
            {
                Enabled = true,
                Provider = "openai-compatible",
                EndpointUrl = "https://ai.example.test/v1",
                ApiKey = "replacement-key",
                ModelId = "model-a",
                AllowedModelIds = ["model-a"],
                ToolProposalsEnabled = true,
                LockTenantAiAssistant = false
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.AiAssistant.Provider,
            GovernanceSettingKeys.AiAssistant.EndpointUrl,
            GovernanceSettingKeys.AiAssistant.ApiKey,
            GovernanceSettingKeys.AiAssistant.ModelId,
            GovernanceSettingKeys.AiAssistant.AllowedModelIds
        ]);
        await Assert.That(notifications.Select(notification => notification.Key)).IsEquivalentTo([
            GovernanceSettingKeys.AiAssistant.Provider,
            GovernanceSettingKeys.AiAssistant.EndpointUrl,
            GovernanceSettingKeys.AiAssistant.ApiKey,
            GovernanceSettingKeys.AiAssistant.ModelId,
            GovernanceSettingKeys.AiAssistant.AllowedModelIds
        ]);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyMcpGovernanceSettingsPatchAsync_WhenOneLeafIsSupplied_WritesOnlyThatKey()
    {
        var writes = CaptureWrites();

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyMcpGovernanceSettingsPatchAsync(
            new PatchMcpGovernanceSettingsDto
            {
                Enabled = OptionalUpdate<bool>.Set(false)
            },
            new McpGovernanceSettingsDto
            {
                Enabled = false,
                EnableLegacySse = true,
                LockTenantMcp = true,
                LockTenantMcpLegacySse = false
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Mcp.Enabled
        ]);
        await Assert.That(notifications.Select(notification => notification.Key)).IsEquivalentTo([
            GovernanceSettingKeys.Mcp.Enabled
        ]);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyRenderPolicySettingsPatchAsync_WhenOneLeafIsSupplied_WritesOnlyThatKey()
    {
        var writes = CaptureWrites();

        await _service.ApplyRenderPolicySettingsPatchAsync(
            new PatchRenderPolicySettingsDto
            {
                GlobalPrerenderEnabled = OptionalUpdate<bool>.Set(true)
            },
            new RenderPolicySettingsDto
            {
                RenderPolicyPreset = "AllInteractiveServer",
                EnableAdvancedRenderPolicyOverrides = false,
                GlobalRenderMode = "InteractiveServer",
                GlobalPrerenderEnabled = true,
                PublicSeoRenderMode = "InteractiveServer",
                OperationalRenderMode = "InteractiveServer",
                AdminRenderMode = "InteractiveServer",
                OnboardingRenderMode = "InteractiveAuto"
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Routing.RenderPolicy.Fallback.PrerenderEnabled
        ]);
    }

    [Test]
    public async Task ApplyEventPolicySettingsPatchAsync_WhenOnlyLockIsSupplied_UsesLockUpsert()
    {
        var lockWrites = CaptureLockWrites();
        var writes = CaptureWrites();

        await _service.ApplyEventPolicyPatchAsync(
            new PatchEventPolicyDto
            {
                LockTenantEventCardClickBehavior = OptionalUpdate<bool>.Set(true)
            },
            new EventPolicyDto
            {
                EventCardClickOpensDetailPage = false,
                LockTenantEventCardClickBehavior = true
            },
            Guid.NewGuid());

        await Assert.That(lockWrites.Count).IsEqualTo(1);
        await Assert.That(lockWrites[0].SettingKey).IsEqualTo(GovernanceSettingKeys.Events.CardClickOpensDetailPage);
        await Assert.That(lockWrites[0].Value).IsEqualTo("false");
        await Assert.That(lockWrites[0].IsLocked).IsTrue();
        await Assert.That(writes).IsEmpty();
    }

    [Test]
    public async Task ApplyMcpGovernanceSettingsPatchAsync_WhenOnlyEnabledLockIsSupplied_UpdatesEnabledAndItsDelegationMirror()
    {
        var lockWrites = CaptureLockWrites();
        var writes = CaptureWrites();

        await _service.ApplyMcpGovernanceSettingsPatchAsync(
            new PatchMcpGovernanceSettingsDto
            {
                LockTenantMcp = OptionalUpdate<bool>.Set(true)
            },
            new McpGovernanceSettingsDto
            {
                Enabled = true,
                EnableLegacySse = false,
                LockTenantMcp = true,
                LockTenantMcpLegacySse = false
            },
            Guid.NewGuid());

        await Assert.That(lockWrites.Count).IsEqualTo(1);
        await Assert.That(lockWrites.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Mcp.Enabled
        ]);
        var enabled = lockWrites.Single();
        await Assert.That(enabled.Value).IsEqualTo("true");
        await Assert.That(enabled.IsLocked).IsTrue();
        await Assert.That(lockWrites.Any(setting => setting.SettingKey == GovernanceSettingKeys.Mcp.EnableLegacySse)).IsFalse();
        await Assert.That(writes.Count).IsEqualTo(1);
        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.TenantDelegation.LockMcp
        ]);
        await _systemSettingRepository.DidNotReceive().UpsertAsync(
            Arg.Is<SystemSetting>(setting => setting.SettingKey == GovernanceSettingKeys.Mcp.Enabled),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyMcpGovernanceSettingsPatchAsync_WhenOnlyLegacySseLockIsSupplied_UpdatesLegacySseAndItsDelegationMirror()
    {
        var lockWrites = CaptureLockWrites();
        var writes = CaptureWrites();

        await _service.ApplyMcpGovernanceSettingsPatchAsync(
            new PatchMcpGovernanceSettingsDto
            {
                LockTenantMcpLegacySse = OptionalUpdate<bool>.Set(true)
            },
            new McpGovernanceSettingsDto
            {
                Enabled = true,
                EnableLegacySse = true,
                LockTenantMcp = false,
                LockTenantMcpLegacySse = true
            },
            Guid.NewGuid());

        await Assert.That(lockWrites.Count).IsEqualTo(1);
        await Assert.That(lockWrites.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Mcp.EnableLegacySse
        ]);
        var legacySse = lockWrites.Single();
        await Assert.That(legacySse.Value).IsEqualTo("true");
        await Assert.That(legacySse.IsLocked).IsTrue();
        await Assert.That(lockWrites.Any(setting => setting.SettingKey == GovernanceSettingKeys.Mcp.Enabled)).IsFalse();
        await Assert.That(writes.Count).IsEqualTo(1);
        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.TenantDelegation.LockMcpLegacySse
        ]);
        await _systemSettingRepository.DidNotReceive().UpsertAsync(
            Arg.Is<SystemSetting>(setting => setting.SettingKey == GovernanceSettingKeys.Mcp.EnableLegacySse),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyAiAssistantGovernanceSettingsPatchAsync_WhenOnlyLockIsSupplied_UpdatesAllGovernedRowsAndDelegationMirror()
    {
        var lockWrites = CaptureLockWrites();
        var writes = CaptureWrites();
        string[] governedKeys =
        [
            GovernanceSettingKeys.AiAssistant.Enabled,
            GovernanceSettingKeys.AiAssistant.Provider,
            GovernanceSettingKeys.AiAssistant.EndpointUrl,
            GovernanceSettingKeys.AiAssistant.ApiKey,
            GovernanceSettingKeys.AiAssistant.ModelId,
            GovernanceSettingKeys.AiAssistant.AllowedModelIds,
            GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess,
            GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled
        ];

        await _service.ApplyAiAssistantGovernanceSettingsPatchAsync(
            new PatchAiAssistantGovernanceSettingsDto
            {
                LockTenantAiAssistant = OptionalUpdate<bool>.Set(true)
            },
            new AiAssistantGovernanceSettingsDto
            {
                Enabled = true,
                Provider = "openai",
                EndpointUrl = "https://unused.example",
                ApiKey = "current-key",
                ModelId = "current-model",
                AllowedModelIds = ["current-model", "second-model"],
                AllowAnonymousAccess = true,
                ToolProposalsEnabled = false,
                LockTenantAiAssistant = true
            },
            Guid.NewGuid());

        await Assert.That(lockWrites.Count).IsEqualTo(8);
        await Assert.That(lockWrites.Select(setting => setting.SettingKey)).IsEquivalentTo(governedKeys);
        var governed = lockWrites.ToList();
        await Assert.That(governed.All(setting => setting.IsLocked)).IsTrue();
        await Assert.That(lockWrites.Single(setting => setting.SettingKey == GovernanceSettingKeys.AiAssistant.Enabled).Value)
            .IsEqualTo("true");
        await Assert.That(lockWrites.Single(setting => setting.SettingKey == GovernanceSettingKeys.AiAssistant.Provider).Value)
            .IsEqualTo("\"openai\"");
        await Assert.That(lockWrites.Single(setting => setting.SettingKey == GovernanceSettingKeys.AiAssistant.EndpointUrl).Value)
            .IsEqualTo("\"\"");
        await Assert.That(lockWrites.Single(setting => setting.SettingKey == GovernanceSettingKeys.AiAssistant.ApiKey).Value)
            .IsEqualTo("\"current-key\"");
        await Assert.That(lockWrites.Single(setting => setting.SettingKey == GovernanceSettingKeys.AiAssistant.ModelId).Value)
            .IsEqualTo("\"current-model\"");
        await Assert.That(lockWrites.Single(setting => setting.SettingKey == GovernanceSettingKeys.AiAssistant.AllowedModelIds).Value)
            .IsEqualTo(SettingValueSerializer.Serialize(new List<string> { "current-model", "second-model" }));
        await Assert.That(lockWrites.Single(setting => setting.SettingKey == GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess).Value)
            .IsEqualTo("true");
        await Assert.That(lockWrites.Single(setting => setting.SettingKey == GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled).Value)
            .IsEqualTo("false");
        await Assert.That(writes.Count).IsEqualTo(1);
        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.TenantDelegation.LockAiAssistant
        ]);
        await Assert.That(writes.Single(setting => setting.SettingKey == GovernanceSettingKeys.TenantDelegation.LockAiAssistant).Value)
            .IsEqualTo("true");
        await _systemSettingRepository.DidNotReceive().UpsertAsync(
            Arg.Is<SystemSetting>(setting => governedKeys.Contains(setting.SettingKey, StringComparer.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyRenderPolicySettingsPatchAsync_WhenOnlyLockIsSupplied_WritesOnlyTheLockSetting()
    {
        var writes = CaptureWrites();

        await _service.ApplyRenderPolicySettingsPatchAsync(
            new PatchRenderPolicySettingsDto
            {
                LockTenantPublicSeoRenderPolicy = OptionalUpdate<bool>.Set(true)
            },
            new RenderPolicySettingsDto
            {
                LockTenantPublicSeoRenderPolicy = true
            },
            Guid.NewGuid());

        await Assert.That(writes.Select(setting => setting.SettingKey)).IsEquivalentTo([
            GovernanceSettingKeys.Routing.RenderPolicy.LockTenantPublicSeo
        ]);
    }


    [Test]
    public async Task ReadSettingsAsync_ReadsMcpGovernanceSettings()
    {
        var resolved = new List<ResolvedSetting>
        {
            CreateResolvedSetting(GovernanceSettingKeys.Mcp.Enabled, "true"),
            CreateResolvedSetting(GovernanceSettingKeys.Mcp.EnableLegacySse, "true"),
            CreateResolvedSetting(GovernanceSettingKeys.TenantDelegation.LockMcp, "false"),
            CreateResolvedSetting(GovernanceSettingKeys.TenantDelegation.LockMcpLegacySse, "true")
        };
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>())
            .Returns(resolved);

        var result = await _service.ReadSettingsAsync();

        await Assert.That(result.Mcp.Enabled).IsTrue();
        await Assert.That(result.Mcp.EnableLegacySse).IsTrue();
        await Assert.That(result.Mcp.LockTenantMcp).IsFalse();
        await Assert.That(result.Mcp.LockTenantMcpLegacySse).IsTrue();
    }

    [Test]
    public async Task ApplyMcpGovernanceSettingsAsync_UpsertsRuntimeValuesAndLocks()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        var actorId = Guid.NewGuid();

        await _service.ApplyMcpGovernanceSettingsAsync(new McpGovernanceSettingsDto
        {
            Enabled = true,
            EnableLegacySse = true,
            LockTenantMcp = true,
            LockTenantMcpLegacySse = false
        }, actorId);

        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(s =>
            s.SettingKey == GovernanceSettingKeys.Mcp.Enabled && s.Value == "true" && s.IsLocked), Arg.Any<CancellationToken>());
        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(s =>
            s.SettingKey == GovernanceSettingKeys.Mcp.EnableLegacySse && s.Value == "true" && !s.IsLocked), Arg.Any<CancellationToken>());
        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(s =>
            s.SettingKey == GovernanceSettingKeys.TenantDelegation.LockMcp && s.Value == "true"), Arg.Any<CancellationToken>());
        await _systemSettingRepository.Received().UpsertAsync(Arg.Is<SystemSetting>(s =>
            s.SettingKey == GovernanceSettingKeys.TenantDelegation.LockMcpLegacySse && s.Value == "false"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyModuleSettingsAsync_DelegatesCapabilitySync_ToModuleCapabilityService()
    {
        _systemSettingRepository.GetByKey(Arg.Any<string>()).Returns((SystemSetting?)null);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        using var transactionCancellation = new CancellationTokenSource();

        await _service.ApplyModuleSettingsAsync(tenantId, new ModuleSettingsDto
        {
            EnableIslamicModule = true,
            EnableTechModule = false
        }, actorId, transactionCancellation.Token);

        await _moduleCapabilityService.Received(1).SyncTenantModuleCapabilitiesAsync(
            tenantId, true, false, actorId, transactionCancellation.Token);
        await _systemSettingRepository.Received(1).UpsertAsync(
            Arg.Is<SystemSetting>(setting => setting.SettingKey == GovernanceSettingKeys.Modules.IslamicEnabled),
            transactionCancellation.Token);
        await _systemSettingRepository.Received(1).UpsertAsync(
            Arg.Is<SystemSetting>(setting => setting.SettingKey == GovernanceSettingKeys.Modules.TechEnabled),
            transactionCancellation.Token);
    }

    // ── Helpers ──────────────────────────────────────

    private void SetupEmptyBatchResolve()
    {
        _resolver.ResolveBatchAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<SettingContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<ResolvedSetting>());
    }

    private static List<ResolvedSetting> CreateResolvedSettingsWithOverrideEnabled()
    {
        return
        [
            CreateResolvedSetting(GovernanceSettingKeys.Routing.RenderPolicy.AllowTenantOverride, "true")
        ];
    }

    private static ResolvedSetting CreateResolvedSetting(string key, string value, bool isLocked = false)
    {
        return new ResolvedSetting
        {
            Key = key,
            Value = value,
            IsLocked = isLocked,
            Source = SettingSource.SystemDefault
        };
    }

    private List<SystemSetting> CaptureWrites()
    {
        var writes = new List<SystemSetting>();
        _systemSettingRepository.UpsertAsync(
                Arg.Do<SystemSetting>(setting => writes.Add(setting)),
                Arg.Any<CancellationToken>())
            .Returns((string?)null);
        return writes;
    }

    private List<SystemSetting> CaptureLockWrites(string? persistedValue = null)
    {
        var writes = new List<SystemSetting>();
        _systemSettingRepository.UpsertLockAsync(
                Arg.Do<SystemSetting>(setting => writes.Add(setting)),
                Arg.Any<CancellationToken>())
            .Returns(persistedValue);
        return writes;
    }

    private static InstanceGovernanceSettings CreateValidSettings()
    {
        return new InstanceGovernanceSettings
        {
            DeploymentMode = new DeploymentModeDto { Mode = DeploymentMode.SingleTenant },
            Modules = new ModuleSettingsDto
            {
                EnableIslamicModule = true,
                EnableTechModule = true
            },
            EventPolicy = new EventPolicyDto(),
            OrganizationPolicy = new OrganizationPolicyDto(),
            Branding = new BrandingSettingsDto(),
            Domains = new DomainSettingsDto(),
            AiAssistant = new AiAssistantGovernanceSettingsDto(),
            Mcp = new McpGovernanceSettingsDto(),
            TenantDelegation = new TenantDelegationSettingsDto
            {
                DefaultPublicHomePage = "EventList"
            },
            AdminPortal = new AdminPortalSettingsDto(),
            LocationPrivacy = new LocationPrivacyGovernanceSettingsDto(),
            RenderPolicy = new RenderPolicySettingsDto
            {
                RenderPolicyVersion = 1,
                RenderPolicyPreset = "AllInteractiveServer",
                EnableAdvancedRenderPolicyOverrides = false,
                GlobalRenderMode = "InteractiveServer",
                GlobalPrerenderEnabled = false,
                PublicSeoRenderMode = "InteractiveServer",
                PublicSeoPrerenderEnabled = false,
                OperationalRenderMode = "InteractiveServer",
                OperationalPrerenderEnabled = false,
                AdminRenderMode = "InteractiveServer",
                AdminPrerenderEnabled = false,
                OnboardingRenderMode = "InteractiveServer",
                OnboardingPrerenderEnabled = false,
                DisallowInteractiveServerOnOnboarding = false
            }
        };
    }
}
