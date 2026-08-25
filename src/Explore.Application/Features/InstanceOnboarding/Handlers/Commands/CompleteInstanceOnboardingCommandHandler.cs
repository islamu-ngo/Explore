// ABOUTME: Handles first-run instance onboarding completion, role assignment, and deployment mode persistence.
// ABOUTME: Auto-creates the first user when not yet synced, then assigns instance admin and default tenant admin roles.

using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Common;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Models;
using Explore.Application.Models.PublicExperience;
using Explore.Application.Onboarding;
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
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ISetupSecretProvider _setupSecretProvider;
    private readonly IInstanceBootstrapAuditLogger _bootstrapAuditLogger;
    private readonly IAdminCacheInvalidator _adminCacheInvalidator;
    private readonly IDeploymentModeProvider _deploymentModeProvider;
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier;
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService;
    private readonly ILogger<CompleteInstanceOnboardingCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteInstanceOnboardingCommandHandler(
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        IPlatformUserRoleRepository platformUserRoleRepository,
        ITenantUserRoleGrantRepository tenantUserRoleGrantRepository,
        ITenantUserRepository tenantUserRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IActorRepository actorRepository,
        IUserExternalLoginRepository userExternalLoginRepository,
        ITenantRepository tenantRepository,
        ISystemSettingRepository systemSettingRepository,
        ISetupSecretProvider setupSecretProvider,
        IInstanceBootstrapAuditLogger bootstrapAuditLogger,
        IAdminCacheInvalidator adminCacheInvalidator,
        IDeploymentModeProvider deploymentModeProvider,
        IJwtAuthorityRefreshNotifier jwtAuthorityRefreshNotifier,
        ITenantBrandingSettingsDocumentProvisioningService tenantBrandingProvisioningService,
        ILogger<CompleteInstanceOnboardingCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _platformUserRoleRepository = platformUserRoleRepository;
        _tenantUserRoleGrantRepository = tenantUserRoleGrantRepository;
        _tenantUserRepository = tenantUserRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _actorRepository = actorRepository;
        _userExternalLoginRepository = userExternalLoginRepository;
        _tenantRepository = tenantRepository;
        _systemSettingRepository = systemSettingRepository;
        _setupSecretProvider = setupSecretProvider;
        _bootstrapAuditLogger = bootstrapAuditLogger;
        _adminCacheInvalidator = adminCacheInvalidator;
        _deploymentModeProvider = deploymentModeProvider;
        _jwtAuthorityRefreshNotifier = jwtAuthorityRefreshNotifier;
        _tenantBrandingProvisioningService = tenantBrandingProvisioningService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CompleteInstanceOnboardingCommand request, CancellationToken cancellationToken)
    {
        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent(cancellationToken);
        if (bootstrap?.IsCompleted == true)
        {
            const string message = "Instance onboarding has already been completed.";
            return BaseCommandResponse.Validation<Guid>([message], message);
        }

        var configuredDeploymentMode = await _deploymentModeProvider.GetConfiguredOnboardingModeAsync(cancellationToken);
        request.Settings.DeploymentMode = configuredDeploymentMode;

        if (string.IsNullOrWhiteSpace(request.Settings.SiteProfile.SiteName)
            && !string.IsNullOrWhiteSpace(request.Settings.InstanceName))
        {
            request.Settings.SiteProfile.SiteName = request.Settings.InstanceName;
        }

        var validator = new CompleteInstanceOnboardingRequestValidator();
        var validation = await validator.ValidateAsync(request.Settings, cancellationToken);
        if (!validation.IsValid)
        {
            return BaseCommandResponse.Validation<Guid>(
                validation.Errors.Select(e => e.ErrorMessage),
                "Invalid onboarding request.");
        }

        // Pre-validate user before opening the transaction — avoids early-return inside tx scope
        var existingUserCheck = await _userRepository.GetById(request.UserId);
        if (existingUserCheck == null && string.IsNullOrWhiteSpace(request.Email))
        {
            return BaseCommandResponse.Validation<Guid>(
                ["No user found and no email claim available to create one."],
                "User identity data is required to complete onboarding.");
        }

        var deploymentMode = configuredDeploymentMode;
        var isSingleTenant = deploymentMode == DeploymentMode.SingleTenant;
        var siteProfile = InstanceOnboardingProfileSettingHelpers.Normalize(
            request.Settings.SiteProfile,
            request.Settings.InstanceName);
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
            await PersistSiteProfileSettingsAsync(siteProfile, isSingleTenant, ct);
            await PersistAdministrationAccessSettingsAsync(request.Settings, isSingleTenant);

            await EnsurePlatformAdministratorRoleAsync(request.UserId);
            _logger.LogInformation("Onboarding: Assigned Platform Admin role");

            if (isSingleTenant && defaultTenantId.HasValue)
            {
                await _tenantBrandingProvisioningService.EnsureTenantBrandingDocumentAsync(
                    defaultTenantId.Value,
                    siteProfile.SiteName,
                    ct);
                await EnsureDefaultTenantAdministratorAsync(defaultTenantId.Value, user);
                _logger.LogInformation("Onboarding: Assigned Tenant Admin role for default tenant");
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
        _setupSecretProvider.Lock();
        _adminCacheInvalidator.InvalidateUser(request.UserId);
        await _deploymentModeProvider.InvalidateCacheAsync();
        await _jwtAuthorityRefreshNotifier.ReloadAsync(cancellationToken);
        _bootstrapAuditLogger.Log(new InstanceBootstrapAuditEvent(
            InstanceBootstrapAuditEventType.SetupModeDisabled,
            Operation: "instance_onboarding_complete",
            Outcome: "disabled",
            ActorUserId: request.UserId,
            DeploymentMode: deploymentMode.ToString()));

        return BaseCommandResponse.Success(
            bootstrap?.Id ?? Guid.Empty,
            "Instance onboarding completed successfully.");
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
            AuthProvider = provider,
            AuthProviderId = providerUserId,
            EmailVerified = request.EmailVerified ?? (provider is "keycloak" or "google")
        };

        user = await _userRepository.Create(user);

        var actorTenantId = tenantId ?? PlatformDefaults.DefaultTenantId;
        if (!tenantId.HasValue)
        {
            await EnsureDefaultTenantAsync();
        }

        var displayName = $"{firstName} {lastName}".Trim();
        var actor = new Actor
        {
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            Pii = new ActorPii
            {
                DisplayName = displayName
            },
            Description = null,
            UserId = user.Id
        };

        await _actorRepository.Create(actor);

        var externalLogin = new UserExternalLogin
        {
            Id = Guid.CreateVersion7(),
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
        var value = JsonSerializer.Serialize(mode.ToString());
        await _systemSettingRepository.UpsertAsync(new SystemSetting
        {
            SettingKey = key,
            Value = value,
            ValueType = SettingValueType.String,
            IsLocked = true,
            Category = "System",
            DisplayOrder = 1,
            Description = "Deployment mode of the application",
            AllowedValues = "[\"SingleTenant\", \"MultiTenant\"]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private async Task PersistSiteProfileSettingsAsync(
        SelfHostOnboardingProfileDto siteProfile,
        bool isSingleTenant,
        CancellationToken cancellationToken)
    {
        await InstanceOnboardingProfileSettingHelpers.PersistAsync(
            _systemSettingRepository,
            siteProfile,
            cancellationToken);

        if (!isSingleTenant)
        {
            return;
        }

        await PersistSingleTenantPublicExperienceDefaultsAsync(siteProfile.SiteName);
    }

    private async Task PersistAdministrationAccessSettingsAsync(CompleteInstanceOnboardingRequest settings, bool isSingleTenant)
    {
        if (isSingleTenant
            || !settings.AdministrationAccessMode.Equals(
                CompleteInstanceOnboardingRequest.DedicatedAdminHostAdministrationAccess,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var adminHost = NormalizeOptionalHost(settings.AdminHost);
        if (string.IsNullOrWhiteSpace(adminHost))
        {
            return;
        }

        await PersistSystemSettingAsync(
            GovernanceSettingKeys.Domains.AdminHost,
            JsonSerializer.Serialize(adminHost),
            SettingValueType.String,
            "Domains",
            2,
            "Dedicated admin/control-plane host for multi-tenant operator access");
    }

    private async Task PersistSingleTenantPublicExperienceDefaultsAsync(string siteName)
    {
        await PersistSystemSettingAsync(
            GovernanceSettingKeys.PublicExperience.Mode,
            JsonSerializer.Serialize(PublicExperienceMode.DiscoveryCentric.ToString()),
            SettingValueType.String,
            "PublicExperience",
            1,
            "Anonymous public experience posture");

        await PersistSystemSettingAsync(
            GovernanceSettingKeys.PublicExperience.EventCatalogLabel,
            JsonSerializer.Serialize("Events"),
            SettingValueType.String,
            "PublicExperience",
            2,
            "Display label for the public event catalog entry point");

        await PersistSystemSettingAsync(
            GovernanceSettingKeys.Routing.DefaultPublicHomePage,
            JsonSerializer.Serialize("EventList"),
            SettingValueType.String,
            "Routing",
            1,
            "Default anonymous public home page");

        var homeBlocks = new PublicExperienceHomeBlocksConfig(
            Blocks:
            [
                new PublicExperienceHomeBlockConfig(
                    Id: "hero",
                    Kind: PublicExperienceHomeBlockKind.Hero,
                    Title: siteName,
                    Subtitle: "Discover upcoming events.",
                    LinkText: "Browse events",
                    LinkUrl: "/events",
                    SortOrder: 0)
            ]);

        await PersistSystemSettingAsync(
            GovernanceSettingKeys.PublicExperience.HomeBlocks,
            JsonSerializer.Serialize(homeBlocks),
            SettingValueType.Json,
            "PublicExperience",
            3,
            "Versioned public home block configuration document");

        var ctas = new PublicExperienceCtasConfig(
            Ctas:
            [
                new PublicExperienceCtaConfig(
                    Id: "browse-events",
                    Label: "Browse events",
                    Url: "/events",
                    Placement: PublicExperienceCtaPlacement.Hero,
                    Style: PublicExperienceCtaStyle.Primary,
                    SortOrder: 0)
            ]);

        await PersistSystemSettingAsync(
            GovernanceSettingKeys.PublicExperience.Ctas,
            JsonSerializer.Serialize(ctas),
            SettingValueType.Json,
            "PublicExperience",
            4,
            "Versioned public call-to-action configuration document");
    }

    private async Task PersistSystemSettingAsync(
        string key,
        string value,
        SettingValueType valueType,
        string category,
        int displayOrder,
        string description,
        bool isLocked = false,
        string? allowedValues = null)
    {
        await _systemSettingRepository.UpsertAsync(new SystemSetting
        {
            SettingKey = key,
            Value = value,
            ValueType = valueType,
            IsLocked = isLocked,
            Category = category,
            DisplayOrder = displayOrder,
            Description = description,
            AllowedValues = allowedValues,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private static string? NormalizeOptionalHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return uri.Host.Trim().ToLowerInvariant();
        }

        return trimmed
            .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim('/')
            .ToLowerInvariant();
    }

    private async Task EnsureDefaultTenantAdministratorAsync(Guid tenantId, User user)
    {
        var userId = user.Id;
        var tenantUser = await EnsureActiveTenantUserAsync(tenantId, user);

        var tenantUserRoleGrant = await _tenantUserRoleGrantRepository.GetByTenantAndUser(tenantId, userId);

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

        if (tenantUserRoleGrant == null)
        {
            await _tenantUserRoleGrantRepository.Create(new TenantUserRoleGrant
            {
                TenantId = tenantId,
                Tenant = null!,
                TenantUserId = tenantUser.Id,
                TenantUser = null!,
                RoleId = tenantAdminRole.Id,
                Role = null!,
                RoleScopeId = (int)RoleScopeEnum.Tenant,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = userId
            });
            return;
        }

        if (tenantUserRoleGrant.RoleId == tenantAdminRole.Id) return;

        tenantUserRoleGrant.RoleId = tenantAdminRole.Id;
        await _tenantUserRoleGrantRepository.Update(tenantUserRoleGrant);
    }

    private async Task<TenantUser> EnsureActiveTenantUserAsync(Guid tenantId, User user)
    {
        var tenantUser = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId, user.Id);
        if (tenantUser == null)
        {
            var actor = user.Actor ?? await _actorRepository.GetActorByUserId(user.Id);
            return await _tenantUserRepository.Create(new TenantUser
            {
                TenantId = tenantId,
                Tenant = null!,
                UserId = user.Id,
                User = null!,
                ActorId = actor?.Id,
                Actor = null,
                StatusId = (int)TenantUserStatusEnum.Active,
                JoinedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id
            });
        }

        if (tenantUser.StatusId == (int)TenantUserStatusEnum.Active && !tenantUser.IsDeleted)
        {
            return tenantUser;
        }

        tenantUser.StatusId = (int)TenantUserStatusEnum.Active;
        tenantUser.IsDeleted = false;
        tenantUser.DeletedAt = null;
        tenantUser.DeletedBy = null;
        tenantUser.UpdatedAt = DateTime.UtcNow;
        tenantUser.UpdatedBy = user.Id;
        await _tenantUserRepository.Update(tenantUser);
        return tenantUser;
    }
}
