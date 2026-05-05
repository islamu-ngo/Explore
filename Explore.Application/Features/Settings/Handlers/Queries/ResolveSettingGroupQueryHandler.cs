// ABOUTME: Generic query handler that resolves all settings for a category through hierarchical cascade.
// ABOUTME: Computes per-setting CanEdit/Reason metadata based on lock state, scope range, and authorization.

namespace Explore.Application.Features.Settings.Handlers.Queries;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Settings;
using Explore.Application.Features.Settings.Requests.Queries;
using Explore.Application.Lookups;
using Explore.Application.Settings;
using Explore.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Logging;

public class ResolveSettingGroupQueryHandler
    : IRequestHandler<ResolveSettingGroupQuery, SettingGroupResponseDto>
{
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminContext _adminContext;
    private readonly ILogger<ResolveSettingGroupQueryHandler> _logger;

    public ResolveSettingGroupQueryHandler(
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IAdminContext adminContext,
        ILogger<ResolveSettingGroupQueryHandler> logger)
    {
        _resolver = resolver;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _adminContext = adminContext;
        _logger = logger;
    }

    public async Task<SettingGroupResponseDto> Handle(
        ResolveSettingGroupQuery request, CancellationToken cancellationToken)
    {
        var definitions = SettingRegistry.GetByCategory(request.Category);
        if (definitions is null || definitions.Count == 0)
        {
            _logger.LogWarning("Setting category '{Category}' not found in registry", request.Category);
            return new SettingGroupResponseDto
            {
                Category = request.Category,
                Settings = []
            };
        }

        var context = SettingCommandHelper.BuildSettingContext(
            request.Scope, _tenantContext, _currentUserService);

        var keys = definitions.Select(d => d.Key);
        var resolved = await _resolver.ResolveBatchAsync(keys, context, cancellationToken);

        var isAuthorized = await CheckScopeAuthorizationAsync(request.Scope, cancellationToken);

        var effectiveSettings = new List<EffectiveSettingDto>(definitions.Count);
        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            var setting = resolved[i];

            var (canEdit, reason) = ComputeEditability(
                setting, definition, request.Scope, isAuthorized);

            effectiveSettings.Add(new EffectiveSettingDto
            {
                Key = definition.Key,
                Value = setting.Value ?? definition.DefaultValue,
                SettingValueTypeId = (int)setting.ValueType,
                SettingValueTypeCode = NormalizedLookupMetadata.SettingValueType((int)setting.ValueType).Code,
                SettingValueTypeName = NormalizedLookupMetadata.SettingValueType((int)setting.ValueType).Name,
                Source = setting.Source,
                IsLocked = setting.IsLocked,
                CanEdit = canEdit,
                Reason = reason,
                Description = setting.Description ?? definition.Description,
                AllowedValues = definition.AllowedValues is { Length: > 0 }
                    ? string.Join(",", definition.AllowedValues)
                    : null
            });
        }

        return new SettingGroupResponseDto
        {
            Category = request.Category,
            Settings = effectiveSettings
        };
    }

    private static (bool CanEdit, string? Reason) ComputeEditability(
        ResolvedSetting resolved, SettingDefinition definition,
        SettingScope requestedScope, bool isAuthorized)
    {
        // Check if locked from a scope above the requested one
        if (resolved.IsLocked)
        {
            var (isBlocked, lockReason) = SettingCommandHelper.CheckLockState(resolved, requestedScope);
            if (isBlocked)
                return (false, lockReason);
        }

        // Check scope range
        if (requestedScope < definition.MinScope || requestedScope > definition.MaxScope)
            return (false, $"Not configurable at {requestedScope} scope");

        // Check authorization
        if (!isAuthorized)
            return (false, "Insufficient permissions");

        return (true, null);
    }

    private async Task<bool> CheckScopeAuthorizationAsync(
        SettingScope scope, CancellationToken ct)
    {
        return scope switch
        {
            SettingScope.User => _currentUserService.IsAuthenticated,
            SettingScope.Tenant => await _adminContext.IsTenantAdminAsync(_tenantContext.TenantId, ct)
                                   || await _adminContext.IsInstanceAdminAsync(ct),
            SettingScope.Instance => await _adminContext.IsInstanceAdminAsync(ct),
            _ => false
        };
    }
}
