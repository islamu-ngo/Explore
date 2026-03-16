// ABOUTME: Handles first-run instance onboarding completion, role assignment, and governance persistence.
// ABOUTME: Auto-creates the first user when not yet synced, then assigns instance admin and default tenant admin roles.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using TenantSettingsEntity = Explore.Domain.TenantSettings;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class CompleteInstanceOnboardingCommandHandler : IRequestHandler<CompleteInstanceOnboardingCommand, BaseCommandResponse<Guid>>
{
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly IPlatformUserRoleRepository _platformUserRoleRepository;
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSettingsRepository _tenantSettingsRepository;
    private readonly IInstanceGovernanceSettingService _governanceSettingService;
    private readonly ISetupSecretProvider _setupSecretProvider;
    private readonly IAdminCacheInvalidator _adminCacheInvalidator;
    private readonly IDeploymentModeCacheInvalidator? _deploymentModeCacheInvalidator;

    public CompleteInstanceOnboardingCommandHandler(
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        IPlatformUserRoleRepository platformUserRoleRepository,
        ITenantMemberRepository tenantMemberRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IActorRepository actorRepository,
        IUserExternalLoginRepository userExternalLoginRepository,
        ITenantRepository tenantRepository,
        ITenantSettingsRepository tenantSettingsRepository,
        IInstanceGovernanceSettingService governanceSettingService,
        ISetupSecretProvider setupSecretProvider,
        IAdminCacheInvalidator adminCacheInvalidator,
        IDeploymentModeCacheInvalidator? deploymentModeCacheInvalidator = null)
    {
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _platformUserRoleRepository = platformUserRoleRepository;
        _tenantMemberRepository = tenantMemberRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _actorRepository = actorRepository;
        _userExternalLoginRepository = userExternalLoginRepository;
        _tenantRepository = tenantRepository;
        _tenantSettingsRepository = tenantSettingsRepository;
        _governanceSettingService = governanceSettingService;
        _setupSecretProvider = setupSecretProvider;
        _adminCacheInvalidator = adminCacheInvalidator;
        _deploymentModeCacheInvalidator = deploymentModeCacheInvalidator;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CompleteInstanceOnboardingCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent();
        if (bootstrap?.IsCompleted == true)
        {
            response.Success = false;
            response.Message = "Instance onboarding has already been completed.";
            return response;
        }

        var validator = new InstanceGovernanceSettingsDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Settings, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid instance governance settings.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        var normalizedDeploymentMode = NormalizeDeploymentMode(request.Settings.DeploymentMode);
        if (normalizedDeploymentMode == null)
        {
            response.Success = false;
            response.Message = "Invalid deployment mode.";
            response.Errors = new List<string> { "DeploymentMode must be SingleTenant or MultiTenant." };
            return response;
        }

        request.Settings.DeploymentMode = normalizedDeploymentMode;

        var isSingleTenant = normalizedDeploymentMode.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase);
        Guid? defaultTenantId = null;

        // Create the default tenant BEFORE user sync so the Actor can reference a valid TenantId.
        if (isSingleTenant)
        {
            var defaultTenant = await EnsureDefaultTenantAsync();
            await EnsureDefaultTenantSettingsAsync(defaultTenant.Id);
            defaultTenantId = defaultTenant.Id;
        }

        // Auto-create the user if not yet synced. During onboarding the normal /api/User/sync
        // endpoint cannot work because tenant resolution middleware blocks it (no tenant exists yet).
        var user = await _userRepository.GetById(request.UserId);
        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                response.Success = false;
                response.Message = "User identity data is required to complete onboarding.";
                response.Errors = new List<string> { "No user found and no email claim available to create one." };
                return response;
            }

            user = await CreateOnboardingUserAsync(request, defaultTenantId);
        }

        await _governanceSettingService.ApplySettingsAsync(defaultTenantId, request.Settings, request.UserId);

        await EnsurePlatformAdministratorRoleAsync(request.UserId);

        if (isSingleTenant && defaultTenantId.HasValue)
        {
            await EnsureDefaultTenantAdministratorAsync(defaultTenantId.Value, request.UserId);
        }

        // Invalidate cached admin status so the new roles are recognized immediately
        // without waiting for the 5-minute sliding cache expiration.
        _adminCacheInvalidator.InvalidateUser(request.UserId);

        if (bootstrap == null)
        {
            bootstrap = await _instanceBootstrapStateRepository.Create(new InstanceBootstrapState
            {
                IsCompleted = true,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CompletedByUserId = request.UserId,
                SelectedDeploymentMode = request.Settings.DeploymentMode
            });
        }
        else
        {
            bootstrap.IsCompleted = true;
            bootstrap.CompletedAt = DateTime.UtcNow;
            bootstrap.CompletedByUserId = request.UserId;
            bootstrap.SelectedDeploymentMode = request.Settings.DeploymentMode;
            await _instanceBootstrapStateRepository.Update(bootstrap);
        }

        // Invalidate the middleware's cached deployment mode so tenant resolution
        // picks up the newly saved SingleTenant/MultiTenant mode immediately.
        _deploymentModeCacheInvalidator?.Invalidate();

        // Lock the setup secret provider to prevent further setup mode access.
        // Once locked, all setup-gated endpoints return 410 Gone.
        _setupSecretProvider.Lock();

        response.Success = true;
        response.Message = "Instance onboarding completed successfully.";
        response.Id = bootstrap.Id;
        return response;
    }

    /// <summary>
    /// Creates the User, Actor, and UserExternalLogin records for the first onboarding user.
    /// This bypasses the normal SyncUserCommand flow which requires tenant resolution middleware.
    /// </summary>
    private async Task<User> CreateOnboardingUserAsync(CompleteInstanceOnboardingCommand request, Guid? tenantId)
    {
        var email = request.Email!.Trim().ToLowerInvariant();
        var firstName = string.IsNullOrWhiteSpace(request.FirstName) ? "User" : request.FirstName.Trim();
        var lastName = string.IsNullOrWhiteSpace(request.LastName) ? string.Empty : request.LastName.Trim();
        var provider = string.IsNullOrWhiteSpace(request.AuthProvider)
            ? AuthSchemeNames.Keycloak.ToLowerInvariant()
            : request.AuthProvider.Trim().ToLowerInvariant();
        var providerUserId = string.IsNullOrWhiteSpace(request.AuthProviderId)
            ? request.UserId.ToString()
            : request.AuthProviderId.Trim();

        var user = new User
        {
            Id = request.UserId,
            Pii = new UserPii
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName
            },
            ActorId = null,
            AuthProvider = provider,
            AuthProviderId = providerUserId,
            EmailVerified = request.EmailVerified ?? (provider is "keycloak" or "google"),
            DefaultActorId = null
        };

        user = await _userRepository.Create(user);

        // Actor requires a TenantId. Use the default tenant if available (SingleTenant mode),
        // otherwise use PlatformDefaults.DefaultTenantId as a placeholder for MultiTenant mode.
        var actorTenantId = tenantId ?? PlatformDefaults.DefaultTenantId;
        if (!tenantId.HasValue)
        {
            await EnsureDefaultTenantAsync();
        }

        var displayName = $"{firstName} {lastName}".Trim();
        var handle = GenerateHandle(request.Username, email, providerUserId);

        var actor = new Actor
        {
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = actorTenantId,
            Tenant = null!,
            Pii = new ActorPii
            {
                DisplayName = displayName,
                Handle = handle,
                Did = provider == "atproto" ? providerUserId : null
            },
            Description = null,
            UserId = user.Id,
            OrganizationId = null,
            DidCustodyTypeId = provider == "atproto"
                ? (int)DidCustodyTypeEnum.SelfCustody
                : (int)DidCustodyTypeEnum.Custodial
        };

        actor = await _actorRepository.Create(actor);
        user.ActorId = actor.Id;
        user.DefaultActorId = actor.Id;
        await _userRepository.Update(user);

        // Create the external login link
        var externalLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TenantId = actorTenantId,
            Tenant = null!,
            Provider = provider,
            ProviderKey = providerUserId,
            ProviderDisplayName = provider switch
            {
                "keycloak" => "Keycloak",
                "google" => "Google",
                "atproto" => "AT Protocol",
                _ => provider
            }
        };

        await _userExternalLoginRepository.Create(externalLogin);

        return user;
    }

    private static string GenerateHandle(string? username, string email, string providerUserId)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username.ToLowerInvariant().Replace(" ", "-");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return providerUserId.Replace(":", "-").Replace(".", "-").ToLowerInvariant();
        }

        var emailPrefix = email.Split('@')[0];
        return emailPrefix.ToLowerInvariant().Replace(".", "-").Replace(" ", "-");
    }

    private async Task<Tenant> EnsureDefaultTenantAsync()
    {
        var tenant = await _tenantRepository.GetById(PlatformDefaults.DefaultTenantId);
        if (tenant != null)
        {
            return tenant;
        }

        return await _tenantRepository.Create(new Tenant
        {
            Id = PlatformDefaults.DefaultTenantId,
            FullName = PlatformDefaults.DefaultTenantName,
            Slug = PlatformDefaults.DefaultTenantSlug,
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = new TenantStatus
            {
                Id = (int)TenantStatusEnum.Active,
                MasterCode = "ACTIVE",
                FullName = "Active",
                IsActiveState = true
            }
        });
    }

    private async Task EnsureDefaultTenantSettingsAsync(Guid tenantId)
    {
        var existing = await _tenantSettingsRepository.GetByTenant(tenantId);
        if (existing != null)
        {
            return;
        }

        await _tenantSettingsRepository.Create(new TenantSettingsEntity
        {
            TenantId = tenantId,
            Tenant = null!
        });
    }

    private async Task EnsurePlatformAdministratorRoleAsync(Guid userId)
    {
        var platformAdminRole = await _roleRepository.GetByMasterCodeAsync("platform.admin")
            ?? await _roleRepository.GetByIdAsync((int)RoleEnum.Admin);

        if (platformAdminRole == null || platformAdminRole.Scope != RoleScopeEnum.Platform)
        {
            return;
        }

        var existing = await _platformUserRoleRepository.GetByUserAndRole(userId, platformAdminRole.Id);
        if (existing != null)
        {
            return;
        }

        await _platformUserRoleRepository.Create(new PlatformUserRole
        {
            UserId = userId,
            User = null!,
            RoleId = platformAdminRole.Id,
            Role = null!,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
    }

    private async Task EnsureDefaultTenantAdministratorAsync(Guid tenantId, Guid userId)
    {
        var tenantMember = await _tenantMemberRepository.GetByTenantAndUser(tenantId, userId);
        var tenantAdminRole = await _roleRepository.GetByMasterCodeAsync("tenant.admin")
            ?? await _roleRepository.GetByIdAsync((int)RoleEnum.TenantAdmin);

        if (tenantAdminRole == null)
        {
            return;
        }

        if (tenantMember == null)
        {
            await _tenantMemberRepository.Create(new TenantMember
            {
                TenantId = tenantId,
                Tenant = null!,
                UserId = userId,
                User = null!,
                RoleId = tenantAdminRole.Id,
                Role = null!,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = userId
            });

            return;
        }

        if (tenantMember.RoleId == tenantAdminRole.Id)
        {
            return;
        }

        tenantMember.RoleId = tenantAdminRole.Id;
        await _tenantMemberRepository.Update(tenantMember);
    }

    private static string? NormalizeDeploymentMode(string? deploymentMode)
    {
        if (string.IsNullOrWhiteSpace(deploymentMode))
        {
            return null;
        }

        if (deploymentMode.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase))
        {
            return "SingleTenant";
        }

        if (deploymentMode.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase))
        {
            return "MultiTenant";
        }

        return null;
    }
}
