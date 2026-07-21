// ABOUTME: Composes existing authority, actor, policy, deployment, and settings contracts for the UI shell.
// ABOUTME: Keeps workspace eligibility server-authoritative without caching or duplicating membership rules.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Ai;
using Explore.Application.DTOs.UiShell;
using Explore.Application.Features.AiAssistant;
using Explore.Application.Features.AiAssistant.Actors;
using Explore.Application.Features.UiShell.Requests.Queries;
using Explore.Application.Models;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.UiShell.Handlers.Queries;

public sealed class GetUiShellContextRequestHandler(
    IUserContext userContext,
    ITenantContext tenantContext,
    IAdminContext adminContext,
    IAiAssistantActorContextService actorContextService,
    IHierarchicalSettingsResolver settingsResolver,
    IDeploymentModeProvider deploymentModeProvider)
    : IRequestHandler<GetUiShellContextRequest, UiShellContextDto>
{
    private static readonly string[] SettingKeys =
    [
        GovernanceSettingKeys.Events.UserSubmissionEnabled,
        GovernanceSettingKeys.UiShell.DefaultNavModeEvents,
        GovernanceSettingKeys.UiShell.DefaultNavModeStudio,
        GovernanceSettingKeys.UiShell.DefaultNavModeAi,
        GovernanceSettingKeys.UiShell.DefaultNavModeSettings,
        GovernanceSettingKeys.UiShell.AllowUserNavOverride,
        GovernanceSettingKeys.UiShell.OrganizerDefaultWorkspace,
        GovernanceSettingKeys.PublicExperience.Mode,
        GovernanceSettingKeys.PublicExperience.PrimaryOrganizationId
    ];

    public async Task<UiShellContextDto> Handle(
        GetUiShellContextRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.GetRequiredUserId();
        Guid tenantId = tenantContext.TenantId;
        var settingContext = new SettingContext(TenantId: tenantId);

        Task<bool> instanceAdminTask = adminContext.IsInstanceAdminAsync(userId, cancellationToken);
        Task<IReadOnlyList<Guid>> tenantAdminTask = adminContext.GetAdminTenantIdsAsync(userId, cancellationToken);
        Task<IReadOnlyList<Guid>> organizationAdminTask = adminContext.GetAdminOrganizationIdsAsync(userId, cancellationToken);
        Task<IReadOnlyList<Guid>> groupAdminTask = adminContext.GetAdminGroupIdsAsync(userId, cancellationToken);
        Task<IReadOnlyList<AiAssistantActorContextDto>> actorsTask =
            actorContextService.ListAuthorizedActorContextsAsync(tenantId, userId, cancellationToken);
        Task<IReadOnlyList<ResolvedSetting>> settingsTask =
            settingsResolver.ResolveBatchAsync(SettingKeys, settingContext, cancellationToken);
        Task<AiAssistantSettingGroup> aiSettingsTask =
            settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(settingContext, cancellationToken);
        Task<DeploymentMode> deploymentModeTask = deploymentModeProvider.GetCurrentModeAsync(cancellationToken);

        bool isInstanceAdmin = await instanceAdminTask;
        IReadOnlyList<Guid> tenantAdminIds = await tenantAdminTask;
        IReadOnlySet<Guid> organizationAdminIds = (await organizationAdminTask).ToHashSet();
        IReadOnlySet<Guid> groupAdminIds = (await groupAdminTask).ToHashSet();
        IReadOnlyList<AiAssistantActorContextDto> actorContexts = await actorsTask;
        Dictionary<string, ResolvedSetting> settings = (await settingsTask)
            .ToDictionary(setting => setting.Key, StringComparer.Ordinal);
        AiAssistantSettingGroup aiSettings = await aiSettingsTask;
        DeploymentMode deploymentMode = await deploymentModeTask;

        List<ManagedActorDto> managedActors = actorContexts
            .Where(actor => actor.ScopeId.HasValue
                && (actor.ActorType == nameof(ActorTypeEnum.Organization)
                    || actor.ActorType == nameof(ActorTypeEnum.Group)))
            .Select(actor => new ManagedActorDto
            {
                ActorId = actor.ActorId,
                ScopeId = actor.ScopeId!.Value,
                ActorType = actor.ActorType,
                DisplayName = actor.ActorDisplayName
            })
            .ToList();

        bool personalCreationAllowed = ReadBool(
            settings,
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            defaultValue: false);
        List<SettingsScopeDto> settingsScopes = BuildSettingsScopes(
            userId,
            tenantId,
            isInstanceAdmin,
            tenantAdminIds,
            organizationAdminIds,
            groupAdminIds,
            managedActors);
        Guid? pinnedActorId = ResolvePinnedActorId(settings, managedActors);

        return new UiShellContextDto
        {
            TenantId = tenantId,
            DeploymentMode = deploymentMode.ToString(),
            Workspaces = new WorkspaceAvailabilityDto
            {
                Studio = managedActors.Count > 0 || personalCreationAllowed,
                Ai = AiAssistantAvailability.ResolveDisabledReason(aiSettings) is null
            },
            ManagedActors = managedActors,
            SettingsScopes = settingsScopes,
            PinnedActorId = pinnedActorId,
            NavigationDefaults = new UiShellNavigationDefaultsDto
            {
                Events = ReadString(settings, GovernanceSettingKeys.UiShell.DefaultNavModeEvents, "Docked"),
                Studio = ReadString(settings, GovernanceSettingKeys.UiShell.DefaultNavModeStudio, "Docked"),
                Ai = ReadString(settings, GovernanceSettingKeys.UiShell.DefaultNavModeAi, "Docked"),
                Settings = ReadString(settings, GovernanceSettingKeys.UiShell.DefaultNavModeSettings, "Docked"),
                AllowUserOverride = ReadBool(settings, GovernanceSettingKeys.UiShell.AllowUserNavOverride, true),
                OrganizerDefaultWorkspace = ReadString(
                    settings,
                    GovernanceSettingKeys.UiShell.OrganizerDefaultWorkspace,
                    "Events")
            }
        };
    }

    private static List<SettingsScopeDto> BuildSettingsScopes(
        Guid userId,
        Guid tenantId,
        bool isInstanceAdmin,
        IReadOnlyList<Guid> tenantAdminIds,
        IReadOnlySet<Guid> organizationAdminIds,
        IReadOnlySet<Guid> groupAdminIds,
        IReadOnlyList<ManagedActorDto> managedActors)
    {
        var scopes = new List<SettingsScopeDto>
        {
            new() { Scope = "Personal", ScopeId = userId, DisplayName = "Personal" }
        };

        scopes.AddRange(managedActors
            .Where(actor => actor.ActorType == nameof(ActorTypeEnum.Organization)
                && organizationAdminIds.Contains(actor.ScopeId))
            .Select(actor => new SettingsScopeDto
            {
                Scope = "Organization",
                ScopeId = actor.ScopeId,
                DisplayName = actor.DisplayName
            }));
        scopes.AddRange(managedActors
            .Where(actor => actor.ActorType == nameof(ActorTypeEnum.Group)
                && groupAdminIds.Contains(actor.ScopeId))
            .Select(actor => new SettingsScopeDto
            {
                Scope = "Group",
                ScopeId = actor.ScopeId,
                DisplayName = actor.DisplayName
            }));

        if (tenantAdminIds.Contains(tenantId))
        {
            scopes.Add(new SettingsScopeDto { Scope = "Tenant", ScopeId = tenantId, DisplayName = "Tenant" });
        }

        if (isInstanceAdmin)
        {
            scopes.Add(new SettingsScopeDto { Scope = "Instance", DisplayName = "Instance" });
        }

        return scopes;
    }

    private static Guid? ResolvePinnedActorId(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        IReadOnlyList<ManagedActorDto> managedActors)
    {
        string mode = ReadString(
            settings,
            GovernanceSettingKeys.PublicExperience.Mode,
            nameof(PublicExperienceMode.DiscoveryCentric));
        string primaryOrganizationId = ReadString(
            settings,
            GovernanceSettingKeys.PublicExperience.PrimaryOrganizationId,
            string.Empty);

        return mode.Equals(nameof(PublicExperienceMode.OrganizationCentric), StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(primaryOrganizationId, out Guid organizationId)
            ? managedActors.FirstOrDefault(actor =>
                actor.ActorType == nameof(ActorTypeEnum.Organization)
                && actor.ScopeId == organizationId)?.ActorId
            : null;
    }

    private static bool ReadBool(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key,
        bool defaultValue) =>
        settings.TryGetValue(key, out ResolvedSetting? setting)
            ? SettingValueSerializer.Deserialize(setting.Value, defaultValue)
            : defaultValue;

    private static string ReadString(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key,
        string defaultValue) =>
        settings.TryGetValue(key, out ResolvedSetting? setting)
            ? SettingValueSerializer.DeserializeString(setting.Value, defaultValue)
            : defaultValue;
}
