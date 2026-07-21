// ABOUTME: Unit tests for authenticated UI-shell capability aggregation across representative principals.
// ABOUTME: Proves administrative authority never implicitly grants organizer workspace access.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Actors;
using Explore.Application.Features.UiShell.Handlers.Queries;
using Explore.Application.Features.UiShell.Requests.Queries;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.UiShell.Queries;

public sealed class GetUiShellContextRequestHandlerTests
{
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly IAiAssistantActorContextService _actorContextService = Substitute.For<IAiAssistantActorContextService>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IDeploymentModeProvider _deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();

    public GetUiShellContextRequestHandlerTests()
    {
        _userContext.GetRequiredUserId().Returns(_userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _adminContext.IsInstanceAdminAsync(_userId, Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminOrganizationIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminGroupIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([]);
        _actorContextService.ListAuthorizedActorContextsAsync(_tenantId, _userId, Arg.Any<CancellationToken>())
            .Returns([]);
        _settingsResolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns([Setting(GovernanceSettingKeys.Events.UserSubmissionEnabled, false)]);
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new AiAssistantSettingGroup());
        _deploymentModeProvider.GetCurrentModeAsync(Arg.Any<CancellationToken>())
            .Returns(DeploymentMode.MultiTenant);
    }

    [Test]
    public async Task Handle_InstanceAdminOnly_DoesNotGrantStudioOrTenantScope()
    {
        _adminContext.IsInstanceAdminAsync(_userId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.Workspaces.Studio).IsFalse();
        await Assert.That(result.SettingsScopes.Select(scope => scope.Scope)).IsEquivalentTo(["Personal", "Instance"]);
    }

    [Test]
    public async Task Handle_TenantAdminWithoutPublisher_DoesNotGrantStudio()
    {
        _adminContext.GetAdminTenantIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([_tenantId]);

        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.Workspaces.Studio).IsFalse();
        await Assert.That(result.SettingsScopes.Select(scope => scope.Scope)).IsEquivalentTo(["Personal", "Tenant"]);
    }

    [Test]
    public async Task Handle_OrganizationOrganizer_ExposesStudioAndAuthorizedOrganizationScope()
    {
        Guid organizationId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        _adminContext.GetAdminOrganizationIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([organizationId]);
        ConfigureActors(Actor(actorId, organizationId, nameof(ActorTypeEnum.Organization), "Community"));

        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.Workspaces.Studio).IsTrue();
        await Assert.That(result.ManagedActors.Single().ActorId).IsEqualTo(actorId);
        await Assert.That(result.SettingsScopes.Single(scope => scope.Scope == "Organization").DisplayName)
            .IsEqualTo("Community");
    }

    [Test]
    public async Task Handle_AuthenticatedSeeker_ExposesOnlyPersonalSettings()
    {
        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.Workspaces.Studio).IsFalse();
        await Assert.That(result.SettingsScopes.Select(scope => scope.Scope)).IsEquivalentTo(["Personal"]);
        await Assert.That(result.NavigationDefaults.Events).IsEqualTo("Docked");
    }

    [Test]
    public async Task Handle_MissingSettings_FailsClosedForStudioAndUsesNavigationDefaults()
    {
        ConfigureSettings();

        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.Workspaces.Studio).IsFalse();
        await Assert.That(result.NavigationDefaults.Events).IsEqualTo("Docked");
        await Assert.That(result.NavigationDefaults.AllowUserOverride).IsTrue();
        await Assert.That(result.NavigationDefaults.OrganizerDefaultWorkspace).IsEqualTo("Events");
    }

    [Test]
    public async Task Handle_PersonalPublisher_ExposesStudioWithoutManagedActor()
    {
        ConfigureSettings(Setting(GovernanceSettingKeys.Events.UserSubmissionEnabled, true));

        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.Workspaces.Studio).IsTrue();
        await Assert.That(result.ManagedActors).IsEmpty();
    }

    [Test]
    public async Task Handle_MultiRolePrincipal_UnionsOnlyExplicitScopes()
    {
        Guid organizationId = Guid.CreateVersion7();
        Guid groupId = Guid.CreateVersion7();
        _adminContext.IsInstanceAdminAsync(_userId, Arg.Any<CancellationToken>()).Returns(true);
        _adminContext.GetAdminTenantIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([_tenantId]);
        _adminContext.GetAdminOrganizationIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([organizationId]);
        _adminContext.GetAdminGroupIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([groupId]);
        ConfigureActors(
            Actor(Guid.CreateVersion7(), organizationId, nameof(ActorTypeEnum.Organization), "Organization"),
            Actor(Guid.CreateVersion7(), groupId, nameof(ActorTypeEnum.Group), "Group"));

        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.SettingsScopes.Select(scope => scope.Scope))
            .IsEquivalentTo(["Personal", "Organization", "Group", "Tenant", "Instance"]);
    }

    [Test]
    public async Task Handle_GroupOrganizer_IncludesGroupManagedActorAndScope()
    {
        Guid groupId = Guid.CreateVersion7();
        _adminContext.GetAdminGroupIdsAsync(_userId, Arg.Any<CancellationToken>()).Returns([groupId]);
        ConfigureActors(Actor(Guid.CreateVersion7(), groupId, nameof(ActorTypeEnum.Group), "Volunteers"));

        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.ManagedActors.Single().ActorType).IsEqualTo(nameof(ActorTypeEnum.Group));
        await Assert.That(result.SettingsScopes.Single(scope => scope.Scope == "Group").ScopeId).IsEqualTo(groupId);
    }

    [Test]
    public async Task Handle_OrganizationCentricOrganizer_PinsConfiguredOrganizationActor()
    {
        Guid organizationId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        ConfigureActors(Actor(actorId, organizationId, nameof(ActorTypeEnum.Organization), "Pinned community"));
        ConfigureSettings(
            Setting(GovernanceSettingKeys.Events.UserSubmissionEnabled, false),
            Setting(GovernanceSettingKeys.PublicExperience.Mode, "OrganizationCentric"),
            Setting(GovernanceSettingKeys.PublicExperience.PrimaryOrganizationId, organizationId.ToString()),
            Setting(GovernanceSettingKeys.UiShell.DefaultNavModeStudio, "Collapsed"));

        var result = await CreateHandler().Handle(new GetUiShellContextRequest(), CancellationToken.None);

        await Assert.That(result.PinnedActorId).IsEqualTo(actorId);
        await Assert.That(result.NavigationDefaults.Studio).IsEqualTo("Collapsed");
    }

    private GetUiShellContextRequestHandler CreateHandler() => new(
        _userContext,
        _tenantContext,
        _adminContext,
        _actorContextService,
        _settingsResolver,
        _deploymentModeProvider);

    private void ConfigureActors(params AiAssistantActorContextDto[] actors) =>
        _actorContextService.ListAuthorizedActorContextsAsync(_tenantId, _userId, Arg.Any<CancellationToken>())
            .Returns(actors);

    private void ConfigureSettings(params ResolvedSetting[] settings) =>
        _settingsResolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(settings);

    private static AiAssistantActorContextDto Actor(
        Guid actorId,
        Guid scopeId,
        string actorType,
        string displayName) => new()
    {
        ActorId = actorId,
        ScopeId = scopeId,
        ActorType = actorType,
        ActorDisplayName = displayName
    };

    private static ResolvedSetting Setting(string key, object value) => new()
    {
        Key = key,
        Value = SettingValueSerializer.Serialize(value),
        Source = SettingSource.SystemDefault
    };
}
