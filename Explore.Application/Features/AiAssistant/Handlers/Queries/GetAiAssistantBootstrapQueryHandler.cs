// ABOUTME: Resolves tenant-scoped AI assistant bootstrap metadata without exposing secrets.
// ABOUTME: Builds safe model, actor-context, feature, limit, and disabled-state details from hierarchical settings.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Handlers.Queries;

public sealed class GetAiAssistantBootstrapQueryHandler : IRequestHandler<GetAiAssistantBootstrapQuery, AiAssistantBootstrapDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly IActorRepository _actorRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;

    public GetAiAssistantBootstrapQueryHandler(
        ITenantContext tenantContext,
        IHierarchicalSettingsResolver settingsResolver,
        ICurrentUserService currentUserService,
        IActorRepository actorRepository,
        ITenantUserRepository tenantUserRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository)
    {
        _tenantContext = tenantContext;
        _settingsResolver = settingsResolver;
        _currentUserService = currentUserService;
        _actorRepository = actorRepository;
        _tenantUserRepository = tenantUserRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _groupMemberRepository = groupMemberRepository;
    }

    public async Task<AiAssistantBootstrapDto> Handle(
        GetAiAssistantBootstrapQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var settings = await _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
            new SettingContext(TenantId: tenantId), cancellationToken);

        var provider = NormalizeProvider(settings.Provider);
        var models = BuildModels(provider, settings);
        var disabledReason = ResolveDisabledReason(provider, settings, models);
        var defaultModelId = ResolveDefaultModelId(settings, models);
        var actorContexts = await BuildActorContextsAsync(tenantId, cancellationToken);

        return new AiAssistantBootstrapDto
        {
            TenantId = tenantId,
            Enabled = settings.Enabled,
            Available = disabledReason is null,
            DisabledReason = disabledReason,
            Provider = provider,
            DefaultModelId = defaultModelId,
            ActorContexts = actorContexts,
            Models = models,
            Features = new AiAssistantFeatureFlagsDto
            {
                ToolProposalsEnabled = disabledReason is null && settings.ToolProposalsEnabled,
                StreamingEnabled = disabledReason is null && settings.StreamingEnabled
            },
            Limits = new AiAssistantLimitsDto
            {
                MaxInputTokens = settings.MaxInputTokens,
                MaxOutputTokens = settings.MaxOutputTokens,
                Temperature = settings.Temperature,
                TimeoutSeconds = AiAssistantAvailability.ResolveTimeoutSeconds(settings),
                DailyMessageLimit = settings.DailyMessageLimit
            },
            RetentionDays = settings.RetentionDays
        };
    }

    private async Task<IReadOnlyList<AiAssistantActorContextDto>> BuildActorContextsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is not Guid userId)
        {
            return [];
        }

        var contexts = new List<AiAssistantActorContextDto>();
        var seenActorIds = new HashSet<Guid>();

        var tenantUser = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, cancellationToken);
        var userActor = tenantUser?.ActorId is Guid tenantUserActorId
            ? await _actorRepository.GetActorWithDetails(tenantUserActorId)
            : await _actorRepository.GetActorByUserIdAndTenantId(userId, tenantId);

        AddActorContext(contexts, seenActorIds, userActor, nameof(ActorTypeEnum.User));

        var allowedOrganizationIds = (await _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
            userId,
            PermissionCodes.EventCreate)).ToHashSet();
        var organizationMemberships = await _organizationMemberRepository.GetMembershipsByUser(userId);
        foreach (var membership in organizationMemberships
                     .Where(membership => membership.TenantId == tenantId
                         && membership.Organization.ActorId is Guid
                         && allowedOrganizationIds.Contains(membership.OrganizationId))
                     .OrderBy(membership => membership.Organization.FullName, StringComparer.OrdinalIgnoreCase))
        {
            AddActorContext(
                contexts,
                seenActorIds,
                membership.Organization.ActorId!.Value,
                nameof(ActorTypeEnum.Organization),
                membership.Organization.FullName);
        }

        var allowedGroupIds = (await _groupMemberRepository.GetGroupIdsWhereUserHasPermission(
            userId,
            PermissionCodes.EventCreate)).ToHashSet();
        var groupMemberships = await _groupMemberRepository.GetMembershipsByUser(userId);
        foreach (var membership in groupMemberships
                     .Where(membership => membership.TenantId == tenantId
                         && membership.Group.ActorId is Guid
                         && allowedGroupIds.Contains(membership.GroupId))
                     .OrderBy(membership => membership.Group.FullName, StringComparer.OrdinalIgnoreCase))
        {
            AddActorContext(
                contexts,
                seenActorIds,
                membership.Group.ActorId!.Value,
                nameof(ActorTypeEnum.Group),
                membership.Group.FullName);
        }

        return contexts;
    }

    private static void AddActorContext(
        ICollection<AiAssistantActorContextDto> contexts,
        ISet<Guid> seenActorIds,
        Actor? actor,
        string fallbackActorType)
    {
        if (actor is null)
        {
            return;
        }

        AddActorContext(contexts, seenActorIds, actor.Id, fallbackActorType, actor.DisplayName);
    }

    private static void AddActorContext(
        ICollection<AiAssistantActorContextDto> contexts,
        ISet<Guid> seenActorIds,
        Guid actorId,
        string actorType,
        string? actorDisplayName)
    {
        if (actorId == Guid.Empty || !seenActorIds.Add(actorId))
        {
            return;
        }

        contexts.Add(new AiAssistantActorContextDto
        {
            ActorId = actorId,
            ActorType = actorType,
            ActorDisplayName = string.IsNullOrWhiteSpace(actorDisplayName) ? actorType : actorDisplayName.Trim()
        });
    }

    private static string NormalizeProvider(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? AiProviderDefaults.ProviderNone
            : provider.Trim().ToLowerInvariant();
    }

    private static IReadOnlyList<AiAssistantModelDto> BuildModels(string provider, AiAssistantSettingGroup settings)
    {
        if (provider == AiProviderDefaults.ProviderFake)
        {
            return
            [
                new AiAssistantModelDto
                {
                    Id = AiProviderDefaults.FakeModelId,
                    DisplayName = AiProviderDefaults.FakeModelDisplayName,
                    MaxInputTokens = AiProviderDefaults.DefaultMaxInputTokens,
                    MaxOutputTokens = AiProviderDefaults.DefaultMaxOutputTokens,
                    SupportsToolProposals = true,
                    SupportsStreaming = false
                }
            ];
        }

        if ((provider == AiProviderDefaults.ProviderOpenAiCompatible || provider == AiProviderDefaults.ProviderAnthropicCompatible) && !string.IsNullOrWhiteSpace(settings.ModelId))
        {
            return AiAssistantAvailability.ResolveAllowedModelIds(settings)
                .Select(modelId => new AiAssistantModelDto
                {
                    Id = modelId,
                    DisplayName = modelId,
                    MaxInputTokens = settings.MaxInputTokens,
                    MaxOutputTokens = settings.MaxOutputTokens,
                    SupportsToolProposals = settings.ToolProposalsEnabled,
                    SupportsStreaming = settings.StreamingEnabled
                })
                .ToList();
        }

        return [];
    }

    private static string? ResolveDisabledReason(
        string provider,
        AiAssistantSettingGroup settings,
        IReadOnlyCollection<AiAssistantModelDto> models)
    {
        if (!settings.Enabled)
            return "disabled";

        if (provider == AiProviderDefaults.ProviderNone)
            return "provider_not_configured";

        if (provider != AiProviderDefaults.ProviderFake && provider != AiProviderDefaults.ProviderOpenAiCompatible && provider != AiProviderDefaults.ProviderAnthropicCompatible)
            return "provider_unsupported";

        if (provider == AiProviderDefaults.ProviderOpenAiCompatible || provider == AiProviderDefaults.ProviderAnthropicCompatible)
        {
            if (string.IsNullOrWhiteSpace(settings.EndpointUrl))
                return "endpoint_not_configured";

            if (!settings.HasModel)
                return "model_not_configured";
        }

        return models.Count == 0 ? "model_not_configured" : null;
    }

    private static string? ResolveDefaultModelId(
        AiAssistantSettingGroup settings,
        IReadOnlyList<AiAssistantModelDto> models)
    {
        if (models.Count == 0)
        {
            return null;
        }

        var configuredModelId = AiAssistantAvailability.ResolveModelId(settings);
        return models.FirstOrDefault(model => string.Equals(
                model.Id,
                configuredModelId,
                StringComparison.OrdinalIgnoreCase))?.Id
            ?? models[0].Id;
    }
}
