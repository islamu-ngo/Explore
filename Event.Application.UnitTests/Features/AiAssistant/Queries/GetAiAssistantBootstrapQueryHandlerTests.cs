// ABOUTME: Unit tests for AI assistant bootstrap query settings resolution.
// ABOUTME: Verifies safe availability, disabled reasons, limits, and model metadata without secrets.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AiAssistant.Handlers.Queries;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Queries;

public sealed class GetAiAssistantBootstrapQueryHandlerTests
{
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly ITenantUserRepository _tenantUserRepository = Substitute.For<ITenantUserRepository>();
    private readonly IOrganizationMemberRepository _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();

    [Test]
    public async Task Handle_WhenDisabled_ReturnsDisabledBootstrapWithoutModels()
    {
        var tenantId = Guid.NewGuid();
        var handler = CreateHandler(tenantId, CreateSettings());

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.Enabled).IsFalse();
        await Assert.That(result.Available).IsFalse();
        await Assert.That(result.DisabledReason).IsEqualTo("disabled");
        await Assert.That(result.Models.Count).IsEqualTo(0);
        await Assert.That(result.Features.ToolProposalsEnabled).IsFalse();
    }

    [Test]
    public async Task Handle_WhenFakeProviderEnabled_ReturnsDeterministicFakeModel()
    {
        var handler = CreateHandler(Guid.NewGuid(), CreateSettings(
            enabled: true,
            provider: AiProviderDefaults.ProviderFake,
            toolProposalsEnabled: true,
            streamingEnabled: true));

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.Available).IsTrue();
        await Assert.That(result.DisabledReason).IsNull();
        await Assert.That(result.DefaultModelId).IsEqualTo(AiProviderDefaults.FakeModelId);
        await Assert.That(result.Models.Count).IsEqualTo(1);
        await Assert.That(result.Models[0].SupportsToolProposals).IsTrue();
        await Assert.That(result.Models[0].SupportsStreaming).IsFalse();
        await Assert.That(result.Features.ToolProposalsEnabled).IsTrue();
        await Assert.That(result.Features.StreamingEnabled).IsTrue();
    }

    [Test]
    public async Task Handle_WhenOpenAiCompatibleMissingEndpoint_ReturnsEndpointDisabledReason()
    {
        var handler = CreateHandler(Guid.NewGuid(), CreateSettings(
            enabled: true,
            provider: AiProviderDefaults.ProviderOpenAiCompatible,
            apiKey: "secret",
            modelId: "gpt-test"));

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.Available).IsFalse();
        await Assert.That(result.DisabledReason).IsEqualTo("endpoint_not_configured");
        await Assert.That(result.Models.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_WhenOpenAiCompatibleConfigured_ReturnsConfiguredModelAndLimits()
    {
        var handler = CreateHandler(Guid.NewGuid(), CreateSettings(
            enabled: true,
            provider: AiProviderDefaults.ProviderOpenAiCompatible,
            endpointUrl: "https://ai.example.test/v1",
            apiKey: "secret",
            modelId: "gpt-test",
            maxInputTokens: 16000,
            maxOutputTokens: 2048,
            temperature: 0.4m,
            timeoutSeconds: 45,
            retentionDays: 14,
            dailyMessageLimit: 25,
            toolProposalsEnabled: true));

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.Available).IsTrue();
        await Assert.That(result.Provider).IsEqualTo(AiProviderDefaults.ProviderOpenAiCompatible);
        await Assert.That(result.DefaultModelId).IsEqualTo("gpt-test");
        await Assert.That(result.Models[0].DisplayName).IsEqualTo("gpt-test");
        await Assert.That(result.Limits.MaxInputTokens).IsEqualTo(16000);
        await Assert.That(result.Limits.MaxOutputTokens).IsEqualTo(2048);
        await Assert.That(result.Limits.Temperature).IsEqualTo(0.4m);
        await Assert.That(result.Limits.TimeoutSeconds).IsEqualTo(AiProviderDefaults.DefaultTimeoutSeconds);
        await Assert.That(result.Limits.DailyMessageLimit).IsEqualTo(25);
        await Assert.That(result.RetentionDays).IsEqualTo(14);
        await Assert.That(result.Features.ToolProposalsEnabled).IsTrue();
        await Assert.That(result.Features.StreamingEnabled).IsFalse();
    }

    [Test]
    public async Task Handle_WhenOpenAiCompatibleUsesLocalEndpoint_ReturnsLocalTimeoutFloor()
    {
        var handler = CreateHandler(Guid.NewGuid(), CreateSettings(
            enabled: true,
            provider: AiProviderDefaults.ProviderOpenAiCompatible,
            endpointUrl: "http://127.0.0.1:1337/v1",
            modelId: "Gemma-4-E2B-Uncensored-HauhauCS-Aggressive-Q8_K_P",
            timeoutSeconds: 30));

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.Available).IsTrue();
        await Assert.That(result.Limits.TimeoutSeconds).IsEqualTo(AiProviderDefaults.LocalProviderTimeoutSeconds);
    }

    [Test]
    public async Task Handle_WhenUserHasMembershipWithoutEventCreatePermission_ExcludesActorContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userActorId = Guid.NewGuid();
        var allowedOrganizationId = Guid.NewGuid();
        var allowedOrganizationActorId = Guid.NewGuid();
        var blockedOrganizationId = Guid.NewGuid();
        var blockedOrganizationActorId = Guid.NewGuid();
        var allowedGroupId = Guid.NewGuid();
        var allowedGroupActorId = Guid.NewGuid();
        var blockedGroupId = Guid.NewGuid();
        var blockedGroupActorId = Guid.NewGuid();
        var handler = CreateHandler(tenantId, CreateSettings(enabled: true, provider: AiProviderDefaults.ProviderFake));
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(userId);
        _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId).Returns(CreateUserActor(userActorId, tenantId, userId));
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(userId, PermissionCodes.EventCreate)
            .Returns([allowedOrganizationId]);
        _organizationMemberRepository.GetMembershipsByUser(userId).Returns(
        [
            CreateOrganizationMembership(tenantId, allowedOrganizationId, allowedOrganizationActorId, "Allowed Org"),
            CreateOrganizationMembership(tenantId, blockedOrganizationId, blockedOrganizationActorId, "Blocked Org")
        ]);
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(userId, PermissionCodes.EventCreate)
            .Returns([allowedGroupId]);
        _groupMemberRepository.GetMembershipsByUser(userId).Returns(
        [
            CreateGroupMembership(tenantId, allowedGroupId, allowedGroupActorId, "Allowed Group"),
            CreateGroupMembership(tenantId, blockedGroupId, blockedGroupActorId, "Blocked Group")
        ]);

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.ActorContexts.Select(actor => actor.ActorId)).IsEquivalentTo(
        [
            userActorId,
            allowedGroupActorId,
            allowedOrganizationActorId
        ]);
        await Assert.That(result.ActorContexts.Select(actor => actor.ActorId)).DoesNotContain(blockedOrganizationActorId);
        await Assert.That(result.ActorContexts.Select(actor => actor.ActorId)).DoesNotContain(blockedGroupActorId);
    }

    private GetAiAssistantBootstrapQueryHandler CreateHandler(Guid tenantId, AiAssistantSettingGroup settings)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(settings);
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);
        _organizationMemberRepository.GetMembershipsByUser(Arg.Any<Guid>()).Returns(Task.FromResult(new List<OrganizationMember>()));
        _groupMemberRepository.GetMembershipsByUser(Arg.Any<Guid>()).Returns(Task.FromResult(new List<GroupMember>()));

        return new GetAiAssistantBootstrapQueryHandler(
            _tenantContext,
            _settingsResolver,
            _currentUserService,
            _actorRepository,
            _tenantUserRepository,
            _organizationMemberRepository,
            _groupMemberRepository);
    }

    private static AiAssistantSettingGroup CreateSettings(
        bool enabled = false,
        string provider = AiProviderDefaults.ProviderNone,
        string endpointUrl = "",
        string apiKey = "",
        string modelId = "",
        int maxInputTokens = 8000,
        int maxOutputTokens = 1024,
        decimal temperature = 0.2m,
        int timeoutSeconds = 30,
        int retentionDays = 30,
        int dailyMessageLimit = 50,
        bool toolProposalsEnabled = false,
        bool streamingEnabled = false)
    {
        var settings = new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = Setting(GovernanceSettingKeys.AiAssistant.Enabled, enabled),
            [GovernanceSettingKeys.AiAssistant.Provider] = Setting(GovernanceSettingKeys.AiAssistant.Provider, provider),
            [GovernanceSettingKeys.AiAssistant.EndpointUrl] = Setting(GovernanceSettingKeys.AiAssistant.EndpointUrl, endpointUrl),
            [GovernanceSettingKeys.AiAssistant.ApiKey] = Setting(GovernanceSettingKeys.AiAssistant.ApiKey, apiKey),
            [GovernanceSettingKeys.AiAssistant.ModelId] = Setting(GovernanceSettingKeys.AiAssistant.ModelId, modelId),
            [GovernanceSettingKeys.AiAssistant.MaxInputTokens] = Setting(GovernanceSettingKeys.AiAssistant.MaxInputTokens, maxInputTokens),
            [GovernanceSettingKeys.AiAssistant.MaxOutputTokens] = Setting(GovernanceSettingKeys.AiAssistant.MaxOutputTokens, maxOutputTokens),
            [GovernanceSettingKeys.AiAssistant.Temperature] = Setting(GovernanceSettingKeys.AiAssistant.Temperature, temperature),
            [GovernanceSettingKeys.AiAssistant.TimeoutSeconds] = Setting(GovernanceSettingKeys.AiAssistant.TimeoutSeconds, timeoutSeconds),
            [GovernanceSettingKeys.AiAssistant.RetentionDays] = Setting(GovernanceSettingKeys.AiAssistant.RetentionDays, retentionDays),
            [GovernanceSettingKeys.AiAssistant.DailyMessageLimit] = Setting(GovernanceSettingKeys.AiAssistant.DailyMessageLimit, dailyMessageLimit),
            [GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled] = Setting(GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled, toolProposalsEnabled),
            [GovernanceSettingKeys.AiAssistant.StreamingEnabled] = Setting(GovernanceSettingKeys.AiAssistant.StreamingEnabled, streamingEnabled)
        };

        var group = new AiAssistantSettingGroup();
        group.Populate(settings);
        return group;
    }

    private static ResolvedSetting Setting(string key, object value) => new()
    {
        Key = key,
        Value = System.Text.Json.JsonSerializer.Serialize(value),
        Source = SettingSource.SystemDefault,
        IsLocked = false
    };

    private static Actor CreateUserActor(Guid actorId, Guid tenantId, Guid userId)
    {
        return new Actor
        {
            Id = actorId,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = userId,
            TenantId = tenantId,
            Tenant = null!,
            Pii = new ActorPii { ActorId = actorId, DisplayName = "Amina Yusuf" }
        };
    }

    private static OrganizationMember CreateOrganizationMembership(
        Guid tenantId,
        Guid organizationId,
        Guid actorId,
        string name)
    {
        return new OrganizationMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            OrganizationId = organizationId,
            Organization = new Organization
            {
                Id = organizationId,
                Tenant = null!,
                ActorId = actorId,
                ApprovalStatus = null!,
                Pii = new OrganizationPii { FullName = name }
            },
            UserId = Guid.NewGuid(),
            User = null!,
            Role = null!
        };
    }

    private static GroupMember CreateGroupMembership(Guid tenantId, Guid groupId, Guid actorId, string name)
    {
        return new GroupMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            GroupId = groupId,
            Group = new Group
            {
                Id = groupId,
                Tenant = null!,
                ActorId = actorId,
                ApprovalStatus = null!,
                FullName = name
            },
            UserId = Guid.NewGuid(),
            User = null!,
            Role = null!
        };
    }
}
