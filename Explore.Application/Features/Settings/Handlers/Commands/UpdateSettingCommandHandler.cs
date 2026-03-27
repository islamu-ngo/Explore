// ABOUTME: Command handler for updating a single setting value at a specific scope.
// ABOUTME: User scope writes via IUserPreferenceRepository; other scopes via IHierarchicalSettingsResolver.

namespace Explore.Application.Features.Settings.Handlers.Commands;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Settings.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Logging;

public class UpdateSettingCommandHandler
    : IRequestHandler<UpdateSettingCommand, BaseCommandResponse<Guid>>
{
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly IUserPreferenceRepository _userPreferenceRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminContext _adminContext;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateSettingCommandHandler> _logger;

    public UpdateSettingCommandHandler(
        IHierarchicalSettingsResolver resolver,
        IUserPreferenceRepository userPreferenceRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IAdminContext adminContext,
        IMediator mediator,
        ILogger<UpdateSettingCommandHandler> logger)
    {
        _resolver = resolver;
        _userPreferenceRepository = userPreferenceRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _adminContext = adminContext;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateSettingCommand request, CancellationToken cancellationToken)
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

        // Validate scope range
        if (request.Scope < definition.MinScope || request.Scope > definition.MaxScope)
        {
            response.Success = false;
            response.Message = $"Setting '{request.Key}' cannot be overridden at {request.Scope} scope. " +
                               $"Allowed range: {definition.MinScope}–{definition.MaxScope}.";
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

        // Validate and serialize value
        var (isValid, serializedValue, validationError) =
            SettingCommandHelper.ValidateAndSerialize(request.Value, definition);
        if (!isValid)
        {
            response.Success = false;
            response.Message = validationError;
            return response;
        }

        // Check lock state
        var context = SettingCommandHelper.BuildSettingContext(
            request.Scope, _tenantContext, _currentUserService);
        var resolved = await _resolver.ResolveWithMetadataAsync(request.Key, context, cancellationToken);

        if (resolved is not null)
        {
            var (isBlocked, lockReason) = SettingCommandHelper.CheckLockState(resolved, request.Scope);
            if (isBlocked)
            {
                response.Success = false;
                response.Message = $"Cannot update '{request.Key}': {lockReason}.";
                return response;
            }
        }

        var oldValue = resolved?.Value;
        var (scopeId, actorId) = SettingCommandHelper.GetScopeAndActorIds(
            request.Scope, _tenantContext, _currentUserService);

        // Write
        if (request.Scope == SettingScope.User)
        {
            await WriteUserPreferenceAsync(
                request.Key, serializedValue!, actorId, cancellationToken);
            _resolver.InvalidateUserCache(_tenantContext.TenantId, actorId);
        }
        else
        {
            await _resolver.SetValueAsync(
                request.Key, serializedValue!, request.Scope, scopeId, actorId, cancellationToken);
            _resolver.InvalidateCache(request.Scope, scopeId);
        }

        _logger.LogInformation(
            "Setting updated: {SettingKey} at {Scope} scope. Actor: {ActorId}",
            request.Key, request.Scope, actorId);

        _ = _mediator.Publish(new SettingChangedNotification(
            request.Key, oldValue, serializedValue,
            SettingCommandHelper.MapScopeToSource(request.Scope),
            _tenantContext.TenantId, actorId, DateTime.UtcNow), CancellationToken.None);

        response.Success = true;
        response.Id = scopeId;
        response.Message = $"Setting '{request.Key}' updated successfully.";
        return response;
    }

    private async Task WriteUserPreferenceAsync(
        string key, string serializedValue, Guid userId, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var existing = await _userPreferenceRepository.GetByUserAndKey(tenantId, userId, key);

        if (existing is not null)
        {
            existing.Value = serializedValue;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;
            await _userPreferenceRepository.Update(existing);
        }
        else
        {
            await _userPreferenceRepository.Create(new UserPreference
            {
                TenantId = tenantId,
                Tenant = null!,
                UserId = userId,
                SettingKey = key,
                Value = serializedValue,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            });
        }
    }
}
