// ABOUTME: Handles first-run instance onboarding completion, role assignment, and deployment mode persistence.
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
using Microsoft.Extensions.Logging;

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
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ISetupSecretProvider _setupSecretProvider;
    private readonly IAdminCacheInvalidator _adminCacheInvalidator;
    private readonly IDeploymentModeProvider _deploymentModeProvider;
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier;
    private readonly ILogger<CompleteInstanceOnboardingCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteInstanceOnboardingCommandHandler(
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        IPlatformUserRoleRepository platformUserRoleRepository,
        ITenantMemberRepository tenantMemberRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IActorRepository actorRepository,
        IUserExternalLoginRepository userExternalLoginRepository,
        ITenantRepository tenantRepository,
        ISystemSettingRepository systemSettingRepository,
        ISetupSecretProvider setupSecretProvider,
        IAdminCacheInvalidator adminCacheInvalidator,
        IDeploymentModeProvider deploymentModeProvider,
        IJwtAuthorityRefreshNotifier jwtAuthorityRefreshNotifier,
        ILogger<CompleteInstanceOnboardingCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _platformUserRoleRepository = platformUserRoleRepository;
        _tenantMemberRepository = tenantMemberRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _actorRepository = actorRepository;
        _userExternalLoginRepository = userExternalLoginRepository;
        _tenantRepository = tenantRepository;
        _systemSettingRepository = systemSettingRepository;
        _setupSecretProvider = setupSecretProvider;
        _adminCacheInvalidator = adminCacheInvalidator;
        _deploymentModeProvider = deploymentModeProvider;
        _jwtAuthorityRefreshNotifier = jwtAuthorityRefreshNotifier;
        _logger = logger;
        _unitOfWork = unitOfWork;
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

        var configuredDeploymentMode = await _deploymentModeProvider.GetConfiguredOnboardingModeAsync(cancellationToken);
        request.Settings.DeploymentMode = configuredDeploymentMode;

        var validator = new CompleteInstanceOnboardingRequestValidator();
        var validation = await validator.ValidateAsync(request.Settings, cancellationToken);
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid onboarding request.";
            response.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Pre-validate user before opening the transaction — avoids early-return inside tx scope
        var existingUserCheck = await _userRepository.GetById(request.UserId);
        if (existingUserCheck == null && string.IsNullOrWhiteSpace(request.Email))
        {
            response.Success = false;
            response.Message = "User identity data is required to complete onboarding.";
            response.Errors = new List<string> { "No user found and no email claim available to create one." };
            return response;
        }

        var deploymentMode = configuredDeploymentMode;
        var isSingleTenant = deploymentMode == DeploymentMode.SingleTenant;
        Guid? defaultTenantId = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (isSingleTenant)
            {
                var defaultTenant = await EnsureDefaultTenantAsync();
                defaultTenantId = defaultTenant.Id;
            }

            var user = await _userRepository.GetById(request.UserId);
            if (user == null)
            {
                user = await CreateOnboardingUserAsync(request, defaultTenantId);
            }

            await PersistDeploymentModeSettingAsync(deploymentMode);

            if (!string.IsNullOrWhiteSpace(request.Settings.InstanceName))
            {
                await PersistInstanceNameSettingAsync(request.Settings.InstanceName.Trim());
            }

            await EnsurePlatformAdministratorRoleAsync(request.UserId);
            _logger.LogInformation("Onboarding: Assigned Platform Admin role to user {UserId}", request.UserId);

            if (isSingleTenant && defaultTenantId.HasValue)
            {
                await EnsureDefaultTenantAdministratorAsync(defaultTenantId.Value, request.UserId);
                _logger.LogInformation("Onboarding: Assigned Tenant Admin role for default tenant {TenantId} to user {UserId}", defaultTenantId, request.UserId);
            }

            var selectedMode = deploymentMode.ToString();

            if (bootstrap == null)
            {
                bootstrap = await _instanceBootstrapStateRepository.Create(new InstanceBootstrapState
                {
                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    CompletedByUserId = request.UserId,
                    SelectedDeploymentMode = selectedMode
                });
            }
            else
            {
                bootstrap.IsCompleted = true;
                bootstrap.CompletedAt = DateTime.UtcNow;
                bootstrap.CompletedByUserId = request.UserId;
                bootstrap.SelectedDeploymentMode = selectedMode;
                await _instanceBootstrapStateRepository.Update(bootstrap);
            }
        }, cancellationToken);

        // Post-commit side effects
        _adminCacheInvalidator.InvalidateUser(request.UserId);
        await _deploymentModeProvider.InvalidateCacheAsync();
        await _jwtAuthorityRefreshNotifier.ReloadAsync(cancellationToken);
        _setupSecretProvider.Lock();

        response.Success = true;
        response.Message = "Instance onboarding completed successfully.";
        response.Id = bootstrap.Id;
        return response;
    }

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
            return username.ToLowerInvariant().Replace(" ", "-");

        if (string.IsNullOrWhiteSpace(email))
            return providerUserId.Replace(":", "-").Replace(".", "-").ToLowerInvariant();

        var emailPrefix = email.Split('@')[0];
        return emailPrefix.ToLowerInvariant().Replace(".", "-").Replace(" ", "-");
    }

    private async Task<Tenant> EnsureDefaultTenantAsync()
    {
        var tenant = await _tenantRepository.GetById(PlatformDefaults.DefaultTenantId);
        if (tenant != null) return tenant;

        return await _tenantRepository.Create(new Tenant
        {
            Id = PlatformDefaults.DefaultTenantId,
            FullName = PlatformDefaults.DefaultTenantName,
            Slug = PlatformDefaults.DefaultTenantSlug,
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        });
    }

    private async Task EnsurePlatformAdministratorRoleAsync(Guid userId)
    {
        // Resolve the platform admin role using the canonical master code used by the repository.
        var platformAdminRole = await _roleRepository.GetByMasterCodeAsync("platform.admin");
        
        // Fallback to ID if master code search fails (unlikely given seeds, but for robustness)
        if (platformAdminRole == null)
        {
            platformAdminRole = await _roleRepository.GetByIdAsync((int)RoleEnum.Admin);
        }

        if (platformAdminRole == null)
        {
            throw new InvalidOperationException("Critical system error: Platform Admin role not found in database.");
        }

        if (platformAdminRole.Scope != RoleScopeEnum.Platform)
        {
            throw new InvalidOperationException($"Critical system error: Role '{platformAdminRole.MasterCode}' has incorrect scope {platformAdminRole.Scope}. Expected Platform.");
        }

        var existing = await _platformUserRoleRepository.GetByUserAndRole(userId, platformAdminRole.Id);
        if (existing != null) return;

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

    private async Task PersistDeploymentModeSettingAsync(DeploymentMode mode)
    {
        var key = GovernanceSettingKeys.Deployment.Mode;
        var value = System.Text.Json.JsonSerializer.Serialize(mode.ToString());
        var existing = await _systemSettingRepository.GetByKey(key);

        if (existing == null)
        {
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = key,
                Value = value,
                ValueType = SettingValueType.String,
                IsLocked = true,
                Category = "System",
                DisplayOrder = 1,
                Description = "Deployment mode of the application",
                AllowedValues = "[\"SingleTenant\", \"MultiTenant\"]",
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
            await _systemSettingRepository.Update(existing);
        }
    }

    private async Task PersistInstanceNameSettingAsync(string instanceName)
    {
        var key = GovernanceSettingKeys.Branding.DisplayName;
        var value = System.Text.Json.JsonSerializer.Serialize(instanceName);
        var existing = await _systemSettingRepository.GetByKey(key);

        if (existing == null)
        {
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = key,
                Value = value,
                ValueType = SettingValueType.String,
                IsLocked = false,
                Category = "Branding",
                DisplayOrder = 1,
                Description = "Instance brand display name",
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
            await _systemSettingRepository.Update(existing);
        }
    }

    private async Task EnsureDefaultTenantAdministratorAsync(Guid tenantId, Guid userId)
    {
        var tenantMember = await _tenantMemberRepository.GetByTenantAndUser(tenantId, userId);
        
        // Resolve the tenant admin role using the canonical master code.
        var tenantAdminRole = await _roleRepository.GetByMasterCodeAsync("tenant.admin");
        
        if (tenantAdminRole == null)
        {
            tenantAdminRole = await _roleRepository.GetByIdAsync((int)RoleEnum.TenantAdmin);
        }

        if (tenantAdminRole == null)
        {
            throw new InvalidOperationException("Critical system error: Tenant Admin role not found in database.");
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

        if (tenantMember.RoleId == tenantAdminRole.Id) return;
        
        tenantMember.RoleId = tenantAdminRole.Id;
        await _tenantMemberRepository.Update(tenantMember);
    }
}
