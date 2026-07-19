// ABOUTME: Command handler for removing a setting override, reverting to parent scope cascade.
// ABOUTME: User scope uses IUserPreferenceRepository; Tenant/Org/Group uses resolver.RemoveOverrideAsync.

namespace Explore.Application.Features.Settings.Handlers.Commands;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Application.Features.Settings.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Logging;

public class ResetSettingCommandHandler
    : IRequestHandler<ResetSettingCommand, BaseCommandResponse<Guid>>
{
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly IUserPreferenceRepository _userPreferenceRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminContext _adminContext;
    private readonly ICerbosConfigResolver? _cerbosConfigResolver;
    private readonly IMediator _mediator;
    private readonly ILogger<ResetSettingCommandHandler> _logger;
    private readonly ILocationPrivacyGovernanceMutationService? _locationPrivacyMutations;

    public ResetSettingCommandHandler(
        IHierarchicalSettingsResolver resolver,
        IUserPreferenceRepository userPreferenceRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IAdminContext adminContext,
        IMediator mediator,
        ILogger<ResetSettingCommandHandler> logger,
        ICerbosConfigResolver? cerbosConfigResolver = null,
        ILocationPrivacyGovernanceMutationService? locationPrivacyMutations = null)
    {
        _resolver = resolver;
        _userPreferenceRepository = userPreferenceRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _adminContext = adminContext;
        _cerbosConfigResolver = cerbosConfigResolver;
        _mediator = mediator;
        _logger = logger;
        _locationPrivacyMutations = locationPrivacyMutations;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        ResetSettingCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Validate key exists
        var definition = SettingRegistry.Get(request.Key);
        if (definition is null)
        {
            response.Success = false;
            response.Message = $"Setting key '{request.Key}' not found in registry.";
            return response;
        }

        // Instance scope cannot be reset (it IS the root)
        if (request.Scope == SettingScope.Instance)
        {
            response.Success = false;
            response.Message = "Cannot reset instance-level settings. Use update to change the value.";
            return response;
        }

        // Authorization
        var (authorized, authError) = await SettingCommandHelper.CheckAuthorizationAsync(
            request.Scope, _adminContext, _tenantContext, _currentUserService, cancellationToken);
        if (!authorized)
        {
            response.Success = false;
            response.Message = authError;
            return response;
        }

        // Get current value for notification
        var context = SettingCommandHelper.BuildSettingContext(
            request.Scope, _tenantContext, _currentUserService);
        var resolved = await _resolver.ResolveWithMetadataAsync(
            request.Key, context, cancellationToken);
        var oldValue = resolved?.Value;

        var (scopeId, actorId) = SettingCommandHelper.GetScopeAndActorIds(
            request.Scope, _tenantContext, _currentUserService);

        // Remove override
        if (request.Scope == SettingScope.User)
        {
            var removed = await _userPreferenceRepository.RemoveOverride(
                _tenantContext.TenantId, actorId, request.Key);
            if (!removed)
            {
                response.Success = false;
                response.Message = $"No user override found for '{request.Key}'.";
                return response;
            }

            _resolver.InvalidateUserCache(_tenantContext.TenantId, actorId);
        }
        else
        {
            try
            {
                await _resolver.RemoveOverrideAsync(
                    request.Key, request.Scope, scopeId, actorId, cancellationToken);
            }
            catch (SettingSystemLockedException exception)
            {
                response.Success = false;
                response.FailureCode = SettingSystemLockedException.Code;
                response.Message = exception.Message;
                return response;
            }

            _resolver.InvalidateCache(request.Scope, scopeId);
            CerbosSettingsCacheInvalidation.InvalidateIfCerbosSettingChanged(
                _cerbosConfigResolver, request.Key, request.Scope, scopeId);
            if (_locationPrivacyMutations?.Handles(request.Key) == true)
            {
                await _locationPrivacyMutations.InvalidateScopeAsync(
                    request.Scope,
                    request.Scope == SettingScope.Tenant ? _tenantContext.TenantId : null,
                    CancellationToken.None);
            }
        }

        _logger.LogInformation(
            "Setting reset: {SettingKey} at {Scope} scope. Actor: {ActorId}",
            request.Key, request.Scope, actorId);

        await _mediator.Publish(new SettingChangedNotification(
            request.Key, oldValue, null,
            SettingCommandHelper.MapScopeToSource(request.Scope),
            _tenantContext.TenantId, actorId, DateTime.UtcNow), CancellationToken.None);

        response.Success = true;
        response.Id = scopeId;
        response.Message = $"Setting '{request.Key}' reset to inherited value.";
        return response;
    }
}
