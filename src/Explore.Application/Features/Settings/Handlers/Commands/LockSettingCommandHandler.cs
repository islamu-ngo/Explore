// ABOUTME: Command handler for locking a setting at Instance or Tenant scope.
// ABOUTME: Validates IsLockable, scope support, and admin authorization before delegating to resolver.

namespace Explore.Application.Features.Settings.Handlers.Commands;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Settings.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Logging;

public class LockSettingCommandHandler
    : IRequestHandler<LockSettingCommand, BaseCommandResponse<Guid>>
{
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminContext _adminContext;
    private readonly ICerbosConfigResolver? _cerbosConfigResolver;
    private readonly IMediator _mediator;
    private readonly ILogger<LockSettingCommandHandler> _logger;

    public LockSettingCommandHandler(
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IAdminContext adminContext,
        IMediator mediator,
        ILogger<LockSettingCommandHandler> logger,
        ICerbosConfigResolver? cerbosConfigResolver = null)
    {
        _resolver = resolver;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _adminContext = adminContext;
        _cerbosConfigResolver = cerbosConfigResolver;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        LockSettingCommand request, CancellationToken cancellationToken)
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

        // Validate lockable
        if (!definition.IsLockable)
        {
            response.Success = false;
            response.Message = $"Setting '{request.Key}' is not lockable.";
            return response;
        }

        // Validate scope (only Instance and Tenant supported)
        if (request.Scope is not SettingScope.Instance and not SettingScope.Tenant)
        {
            response.Success = false;
            response.Message = $"Locking is only supported at Instance and Tenant scopes, not {request.Scope}.";
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

        var (scopeId, actorId) = SettingCommandHelper.GetScopeAndActorIds(
            request.Scope, _tenantContext, _currentUserService);

        await _resolver.LockAsync(
            request.Key, request.Scope, scopeId, actorId, cancellationToken);

        _resolver.InvalidateCache(request.Scope, scopeId);
        CerbosSettingsCacheInvalidation.InvalidateIfCerbosSettingChanged(
            _cerbosConfigResolver, request.Key, request.Scope, scopeId);

        var lockSource = request.Scope == SettingScope.Instance
            ? SettingSource.SystemLocked
            : SettingSource.TenantLocked;

        _logger.LogInformation(
            "Setting locked: {SettingKey} at {Scope} scope. Actor: {ActorId}",
            request.Key, request.Scope, actorId);

        _ = _mediator.Publish(new SettingChangedNotification(
            request.Key, null, null, lockSource,
            _tenantContext.TenantId, actorId, DateTime.UtcNow), CancellationToken.None);

        response.Success = true;
        response.Id = scopeId;
        response.Message = $"Setting '{request.Key}' locked at {request.Scope} scope.";
        return response;
    }
}
