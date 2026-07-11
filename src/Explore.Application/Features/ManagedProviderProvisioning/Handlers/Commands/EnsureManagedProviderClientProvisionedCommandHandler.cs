// ABOUTME: Application-layer orchestration for provider-provisioned tenant, user actor, and tenant-admin role grant creation.
// ABOUTME: Uses tenant-scoped role grants and optional organizer actors without granting platform/instance administrator roles.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ManagedProviderProvisioning;
using Explore.Application.DTOs.ManagedProviderProvisioning.Validators;
using Explore.Application.Features.ManagedProviderProvisioning.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.ManagedProviderProvisioning.Handlers.Commands;

public class EnsureManagedProviderClientProvisionedCommandHandler(
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    IActorRepository actorRepository,
    IUserExternalLoginRepository userExternalLoginRepository,
    ITenantUserRepository tenantUserRepository,
    ITenantUserProfileRepository tenantUserProfileRepository,
    ITenantUserRoleGrantRepository tenantUserRoleGrantRepository,
    IRoleRepository roleRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupRepository groupRepository,
    IGroupMemberRepository groupMemberRepository,
    IExternalBindingRepository externalBindingRepository,
    IUnitOfWork unitOfWork,
    ILogger<EnsureManagedProviderClientProvisionedCommandHandler> logger)
    : IRequestHandler<EnsureManagedProviderClientProvisionedCommand, BaseCommandResponse<ManagedProviderClientProvisioningResultDto>>
{
    public async Task<BaseCommandResponse<ManagedProviderClientProvisioningResultDto>> Handle(
        EnsureManagedProviderClientProvisionedCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<ManagedProviderClientProvisioningResultDto>();
        var dto = request.ProvisioningDto;

        var validator = new ManagedProviderClientProvisioningDtoValidator();
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = "Managed provider client provisioning failed due to validation errors.";
            response.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var normalizedProviderKey = dto.ProviderKey.Trim();
        var normalizedExternalSystem = dto.ExternalSystem.Trim();
        var normalizedExternalCustomerId = dto.ExternalCustomerId.Trim();
        var normalizedTenantSlug = dto.TenantSlug.Trim().ToLowerInvariant();
        var normalizedIdentityProvider = dto.ExternalAdmin.IdentityProvider.Trim();
        var normalizedSubject = dto.ExternalAdmin.Subject.Trim();

        var existingCustomerBinding = await externalBindingRepository.GetByExternalKeyAsync(
            normalizedProviderKey,
            normalizedExternalSystem,
            ExternalBindingTypes.External.ProviderCustomer,
            normalizedExternalCustomerId,
            scopeTenantId: null,
            cancellationToken);

        if (existingCustomerBinding != null)
        {
            return await RehydrateExistingProvisioningResultAsync(
                existingCustomerBinding,
                normalizedProviderKey,
                normalizedIdentityProvider,
                normalizedSubject,
                cancellationToken);
        }

        var existingTenant = await tenantRepository.GetTenantBySlug(normalizedTenantSlug);
        if (existingTenant != null)
        {
            return Failure("A tenant with this slug already exists.", "Tenant slug must be unique across managed provider provisioning requests.");
        }

        var existingLogin = await userExternalLoginRepository.GetByProviderAndKey(normalizedIdentityProvider, normalizedSubject);
        var existingUser = existingLogin == null ? null : await userRepository.GetById(existingLogin.UserId);
        if (existingLogin != null && existingUser == null)
        {
            return Failure("External admin identity is linked to a missing user.", "The external login points to a user that could not be found.");
        }

        var tenantId = Guid.NewGuid();
        var userId = existingUser?.Id ?? Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var tenantUserProfileId = Guid.NewGuid();
        var userActorId = Guid.NewGuid();
        var userExternalLoginId = existingLogin?.Id ?? Guid.NewGuid();
        var tenantUserRoleGrantId = Guid.NewGuid();
        var organizerId = dto.Organizer == null ? (Guid?)null : Guid.NewGuid();
        var organizerActorId = dto.Organizer == null ? (Guid?)null : Guid.NewGuid();
        var organizerMembershipId = dto.Organizer == null ? (Guid?)null : Guid.NewGuid();

        var result = await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var tenant = await CreateTenantAsync(dto, normalizedTenantSlug, tenantId);
            var user = await EnsureUserAsync(dto.ExternalAdmin, normalizedIdentityProvider, normalizedSubject, existingUser, userId);
            var userActor = await EnsureUserActorAsync(dto.ExternalAdmin, tenant.Id, user, userActorId);
            var tenantUser = await EnsureTenantUserAsync(tenant.Id, user.Id, userActor.Id, tenantUserId, user.Id);
            var tenantUserProfile = await EnsureTenantUserProfileAsync(dto.ExternalAdmin, tenant.Id, tenantUser.Id, tenantUserProfileId, user.Id);

            if (existingLogin == null)
            {
                await userExternalLoginRepository.Create(new UserExternalLogin
                {
                    Id = userExternalLoginId,
                    UserId = user.Id,
                    User = null!,
                    TenantId = tenant.Id,
                    Tenant = null!,
                    Provider = normalizedIdentityProvider,
                    ProviderKey = normalizedSubject,
                    ProviderDisplayName = dto.ExternalAdmin.IdentityProvider.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.Id
                });
            }

            var tenantUserRoleGrant = await EnsureTenantAdminRoleGrantAsync(tenant.Id, tenantUser.Id, user.Id, tenantUserRoleGrantId);
            var organizerResult = dto.Organizer == null
                ? (OrganizerId: (Guid?)null, ActorId: (Guid?)null, MembershipId: (Guid?)null)
                : await CreateOrganizerAsync(dto.Organizer, tenant.Id, user.Id, organizerId!.Value, organizerActorId!.Value, organizerMembershipId!.Value, ct);

            await EnsureExternalBindingAsync(
                normalizedProviderKey,
                normalizedExternalSystem,
                ExternalBindingTypes.External.ProviderCustomer,
                normalizedExternalCustomerId,
                ExternalBindingTypes.Internal.Tenant,
                tenant.Id,
                scopeTenantId: null,
                createdBy: user.Id,
                ct);

            await EnsureExternalBindingAsync(
                normalizedProviderKey,
                normalizedIdentityProvider,
                ExternalBindingTypes.External.ExternalAdminUser,
                normalizedSubject,
                ExternalBindingTypes.Internal.User,
                user.Id,
                tenant.Id,
                user.Id,
                ct);

            await EnsureExternalBindingAsync(
                normalizedProviderKey,
                normalizedIdentityProvider,
                ExternalBindingTypes.External.ExternalAdminTenantUser,
                normalizedSubject,
                ExternalBindingTypes.Internal.TenantUser,
                tenantUser.Id,
                tenant.Id,
                user.Id,
                ct);

            await EnsureExternalBindingAsync(
                normalizedProviderKey,
                normalizedIdentityProvider,
                ExternalBindingTypes.External.ExternalAdminTenantUserProfile,
                normalizedSubject,
                ExternalBindingTypes.Internal.TenantUserProfile,
                tenantUserProfile.Id,
                tenant.Id,
                user.Id,
                ct);

            await EnsureExternalBindingAsync(
                normalizedProviderKey,
                normalizedIdentityProvider,
                ExternalBindingTypes.External.ExternalAdminUserActor,
                normalizedSubject,
                ExternalBindingTypes.Internal.Actor,
                userActor.Id,
                tenant.Id,
                user.Id,
                ct);

            await EnsureExternalBindingAsync(
                normalizedProviderKey,
                normalizedIdentityProvider,
                ExternalBindingTypes.External.ExternalAdminUserLogin,
                normalizedSubject,
                ExternalBindingTypes.Internal.UserExternalLogin,
                existingLogin?.Id ?? userExternalLoginId,
                tenant.Id,
                user.Id,
                ct);

            if (dto.Organizer != null && organizerResult.OrganizerId.HasValue)
            {
                var organizerExternalType = dto.Organizer.Kind == ManagedProviderOrganizerKindDto.Group
                    ? ExternalBindingTypes.External.CustomerGroup
                    : ExternalBindingTypes.External.CustomerOrganization;
                var organizerActorExternalType = dto.Organizer.Kind == ManagedProviderOrganizerKindDto.Group
                    ? ExternalBindingTypes.External.CustomerGroupActor
                    : ExternalBindingTypes.External.CustomerOrganizationActor;
                var organizerInternalType = dto.Organizer.Kind == ManagedProviderOrganizerKindDto.Group
                    ? ExternalBindingTypes.Internal.Group
                    : ExternalBindingTypes.Internal.Organization;

                await EnsureExternalBindingAsync(
                    normalizedProviderKey,
                    normalizedExternalSystem,
                    organizerExternalType,
                    normalizedExternalCustomerId,
                    organizerInternalType,
                    organizerResult.OrganizerId.Value,
                    tenant.Id,
                    user.Id,
                    ct);

                if (organizerResult.ActorId.HasValue)
                {
                    await EnsureExternalBindingAsync(
                        normalizedProviderKey,
                        normalizedExternalSystem,
                        organizerActorExternalType,
                        normalizedExternalCustomerId,
                        ExternalBindingTypes.Internal.Actor,
                        organizerResult.ActorId.Value,
                        tenant.Id,
                        user.Id,
                        ct);
                }
            }

            return new ManagedProviderClientProvisioningResultDto
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                TenantUserId = tenantUser.Id,
                TenantUserProfileId = tenantUserProfile.Id,
                UserActorId = userActor.Id,
                UserExternalLoginId = existingLogin?.Id ?? userExternalLoginId,
                TenantUserRoleGrantId = tenantUserRoleGrant.Id,
                OrganizerId = organizerResult.OrganizerId,
                OrganizerActorId = organizerResult.ActorId,
                OrganizerKind = dto.Organizer?.Kind,
                OrganizerMembershipId = organizerResult.MembershipId
            };
        }, cancellationToken);

        response.Success = true;
        response.Id = result;
        response.Message = "Managed provider client provisioned successfully.";
        return response;
    }

    private async Task<Tenant> CreateTenantAsync(ManagedProviderClientProvisioningDto dto, string normalizedTenantSlug, Guid tenantId)
    {
        return await tenantRepository.Create(new Tenant
        {
            Id = tenantId,
            FullName = dto.TenantFullName.Trim(),
            Slug = normalizedTenantSlug,
            Description = $"Provisioned from {dto.ExternalSystem.Trim()} customer {dto.ExternalCustomerId.Trim()} by provider {dto.ProviderKey.Trim()}.",
            TenantStatusId = dto.ActivateTenant ? (int)TenantStatusEnum.Active : (int)TenantStatusEnum.Provisioning,
            TenantStatus = null!,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<User> EnsureUserAsync(
        ManagedProviderExternalAdminDto admin,
        string normalizedIdentityProvider,
        string normalizedSubject,
        User? existingUser,
        Guid userId)
    {
        if (existingUser != null)
        {
            return existingUser;
        }

        return await userRepository.Create(new User
        {
            Id = userId,
            Pii = new UserPii
            {
                Email = admin.Email.Trim(),
                FirstName = admin.FirstName.Trim(),
                LastName = admin.LastName.Trim()
            },
            AuthProvider = normalizedIdentityProvider,
            AuthProviderId = normalizedSubject,
            EmailVerified = admin.EmailVerified,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<Actor> EnsureUserActorAsync(ManagedProviderExternalAdminDto admin, Guid tenantId, User user, Guid userActorId)
    {
        var existingUserActor = await actorRepository.GetActorByUserIdAndTenantId(user.Id, tenantId);
        if (existingUserActor != null)
        {
            return existingUserActor;
        }

        var userActor = await actorRepository.Create(new Actor
        {
            Id = userActorId,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = tenantId,
            Tenant = null!,
            UserId = user.Id,
            OrganizationId = null,
            GroupId = null,
            Pii = new ActorPii
            {
                DisplayName = ResolveDisplayName(admin),
                Handle = GenerateHandle(ResolveDisplayName(admin), "user")
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Id
        });

        if (user.ActorId == null)
        {
            user.ActorId = userActor.Id;
        }

        if (user.DefaultActorId == null)
        {
            user.DefaultActorId = userActor.Id;
        }

        await userRepository.Update(user);
        return userActor;
    }

    private async Task<TenantUser> EnsureTenantUserAsync(Guid tenantId, Guid userId, Guid actorId, Guid tenantUserId, Guid createdBy)
    {
        var existing = await tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId);
        if (existing != null)
        {
            return existing;
        }

        return await tenantUserRepository.Create(new TenantUser
        {
            Id = tenantUserId,
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            ActorId = actorId,
            Actor = null!,
            StatusId = (int)TenantUserStatusEnum.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        });
    }

    private async Task<TenantUserProfile> EnsureTenantUserProfileAsync(
        ManagedProviderExternalAdminDto admin,
        Guid tenantId,
        Guid tenantUserId,
        Guid tenantUserProfileId,
        Guid createdBy)
    {
        var existing = await tenantUserProfileRepository.GetByTenantUserAsync(tenantUserId);
        if (existing != null)
        {
            return existing;
        }

        return await tenantUserProfileRepository.Create(new TenantUserProfile
        {
            Id = tenantUserProfileId,
            TenantId = tenantId,
            Tenant = null!,
            TenantUserId = tenantUserId,
            TenantUser = null!,
            DisplayNameOverride = ResolveDisplayName(admin),
            ContactEmailOverride = admin.Email.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        });
    }

    private async Task<TenantUserRoleGrant> EnsureTenantAdminRoleGrantAsync(Guid tenantId, Guid tenantUserId, Guid userId, Guid tenantUserRoleGrantId)
    {
        var existing = await tenantUserRoleGrantRepository.GetByTenantAndUser(tenantId, userId);
        if (existing != null)
        {
            return existing;
        }

        var tenantAdminRole = await roleRepository.GetByMasterCodeAsync("tenant.admin")
            ?? await roleRepository.GetByIdAsync((int)RoleEnum.TenantAdmin);

        if (tenantAdminRole == null)
        {
            logger.LogWarning("tenant.admin role not found during managed provider provisioning for TenantId={TenantId}.", tenantId);
            tenantAdminRole = new Role
            {
                Id = (int)RoleEnum.TenantAdmin,
                MasterCode = "tenant.admin",
                FullName = "Tenant Administrator",
                Scope = RoleScopeEnum.Tenant
            };
        }

        return await tenantUserRoleGrantRepository.Create(new TenantUserRoleGrant
        {
            Id = tenantUserRoleGrantId,
            TenantId = tenantId,
            Tenant = null!,
            TenantUserId = tenantUserId,
            TenantUser = null!,
            RoleId = tenantAdminRole.Id,
            Role = null!,
            RoleScopeId = (int)RoleScopeEnum.Tenant,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
    }

    private async Task<(Guid? OrganizerId, Guid? ActorId, Guid? MembershipId)> CreateOrganizerAsync(
        ManagedProviderOrganizerDto organizer,
        Guid tenantId,
        Guid adminUserId,
        Guid organizerId,
        Guid organizerActorId,
        Guid organizerMembershipId,
        CancellationToken cancellationToken)
    {
        return organizer.Kind == ManagedProviderOrganizerKindDto.Group
            ? await CreateGroupOrganizerAsync(organizer, tenantId, adminUserId, organizerId, organizerActorId, organizerMembershipId)
            : await CreateOrganizationOrganizerAsync(organizer, tenantId, adminUserId, organizerId, organizerActorId, organizerMembershipId, cancellationToken);
    }

    private async Task<(Guid? OrganizerId, Guid? ActorId, Guid? MembershipId)> CreateOrganizationOrganizerAsync(
        ManagedProviderOrganizerDto organizer,
        Guid tenantId,
        Guid adminUserId,
        Guid organizationId,
        Guid actorId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.Create(new Organization
        {
            Id = organizationId,
            Pii = new OrganizationPii
            {
                FullName = organizer.FullName.Trim(),
                Email = organizer.Email?.Trim(),
                Country = organizer.Country?.Trim(),
                City = organizer.City?.Trim(),
                Address = organizer.Address?.Trim(),
                Postcode = organizer.Postcode?.Trim()
            },
            WebsiteUrl = organizer.WebsiteUrl?.Trim(),
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
            ApprovalStatus = null!,
            ApprovedAt = DateTime.UtcNow,
            ApprovedBy = adminUserId,
            TenantId = tenantId,
            Tenant = null!,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminUserId
        });

        var actor = await actorRepository.Create(new Actor
        {
            Id = actorId,
            ActorTypeId = (int)ActorTypeEnum.Organization,
            ActorType = null!,
            TenantId = tenantId,
            Tenant = null!,
            OrganizationId = organization.Id,
            Pii = new ActorPii
            {
                DisplayName = organization.FullName,
                Handle = GenerateHandle(organization.FullName, "org")
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminUserId
        });

        organization.ActorId = actor.Id;
        await organizationRepository.Update(organization);

        var existingMembership = await organizationMemberRepository.GetByOrganizationAndUser(organization.Id, adminUserId);
        var membership = existingMembership ?? await organizationMemberRepository.Create(new OrganizationMember
        {
            Id = membershipId,
            OrganizationId = organization.Id,
            Organization = null!,
            UserId = adminUserId,
            User = null!,
            RoleId = (int)RoleEnum.OrgAdmin,
            Role = null!,
            TenantId = tenantId,
            Tenant = null!,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminUserId
        });

        return (organization.Id, actor.Id, membership.Id);
    }

    private async Task<(Guid? OrganizerId, Guid? ActorId, Guid? MembershipId)> CreateGroupOrganizerAsync(
        ManagedProviderOrganizerDto organizer,
        Guid tenantId,
        Guid adminUserId,
        Guid groupId,
        Guid actorId,
        Guid membershipId)
    {
        var group = await groupRepository.Create(new Group
        {
            Id = groupId,
            FullName = organizer.FullName.Trim(),
            Description = organizer.Description?.Trim(),
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminUserId
        });

        var actor = await actorRepository.Create(new Actor
        {
            Id = actorId,
            ActorTypeId = (int)ActorTypeEnum.Group,
            ActorType = null!,
            TenantId = tenantId,
            Tenant = null!,
            GroupId = group.Id,
            Pii = new ActorPii
            {
                DisplayName = group.FullName,
                Handle = GenerateHandle(group.FullName, "grp")
            },
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminUserId
        });

        group.ActorId = actor.Id;
        await groupRepository.Update(group);

        var existingMembership = await groupMemberRepository.GetByGroupAndUser(group.Id, adminUserId);
        var membership = existingMembership ?? await groupMemberRepository.Create(new GroupMember
        {
            Id = membershipId,
            GroupId = group.Id,
            Group = null!,
            UserId = adminUserId,
            User = null!,
            RoleId = (int)RoleEnum.GroupAdmin,
            Role = null!,
            TenantId = tenantId,
            Tenant = null!,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminUserId
        });

        return (group.Id, actor.Id, membership.Id);
    }

    private async Task<BaseCommandResponse<ManagedProviderClientProvisioningResultDto>> RehydrateExistingProvisioningResultAsync(
        ExternalBinding existingCustomerBinding,
        string normalizedProviderKey,
        string normalizedIdentityProvider,
        string normalizedSubject,
        CancellationToken cancellationToken)
    {
        if (existingCustomerBinding.InternalType != ExternalBindingTypes.Internal.Tenant)
        {
            return Failure(
                "External provider customer binding is invalid.",
                "The provider customer binding does not point to an Event tenant.");
        }

        var tenant = await tenantRepository.GetById(existingCustomerBinding.InternalId);
        if (tenant == null)
        {
            return Failure(
                "External provider customer binding points to a missing tenant.",
                "The existing provider customer binding could not be rehydrated.");
        }

        var userBinding = await externalBindingRepository.GetByExternalKeyAsync(
            normalizedProviderKey,
            normalizedIdentityProvider,
            ExternalBindingTypes.External.ExternalAdminUser,
            normalizedSubject,
            tenant.Id,
            cancellationToken);
        if (userBinding == null || userBinding.InternalType != ExternalBindingTypes.Internal.User)
        {
            return Failure(
                "External admin user binding is missing.",
                "The provider customer was already provisioned, but the external admin user binding is incomplete.");
        }

        var user = await userRepository.GetById(userBinding.InternalId);
        if (user == null)
        {
            return Failure(
                "External admin user binding points to a missing user.",
                "The existing external admin user binding could not be rehydrated.");
        }

        var actorBinding = await externalBindingRepository.GetByExternalKeyAsync(
            normalizedProviderKey,
            normalizedIdentityProvider,
            ExternalBindingTypes.External.ExternalAdminUserActor,
            normalizedSubject,
            tenant.Id,
            cancellationToken);
        var tenantUserBinding = await externalBindingRepository.GetByExternalKeyAsync(
            normalizedProviderKey,
            normalizedIdentityProvider,
            ExternalBindingTypes.External.ExternalAdminTenantUser,
            normalizedSubject,
            tenant.Id,
            cancellationToken);
        var tenantUserProfileBinding = await externalBindingRepository.GetByExternalKeyAsync(
            normalizedProviderKey,
            normalizedIdentityProvider,
            ExternalBindingTypes.External.ExternalAdminTenantUserProfile,
            normalizedSubject,
            tenant.Id,
            cancellationToken);
        var loginBinding = await externalBindingRepository.GetByExternalKeyAsync(
            normalizedProviderKey,
            normalizedIdentityProvider,
            ExternalBindingTypes.External.ExternalAdminUserLogin,
            normalizedSubject,
            tenant.Id,
            cancellationToken);
        var tenantUserRoleGrant = await tenantUserRoleGrantRepository.GetByTenantAndUser(tenant.Id, user.Id);

        if (actorBinding == null || tenantUserBinding == null || tenantUserProfileBinding == null || loginBinding == null || tenantUserRoleGrant == null)
        {
            return Failure(
                "Managed provider provisioning binding is incomplete.",
                "The existing provider customer binding is missing the tenant user state, user actor, external login, or tenant-admin role grant.");
        }

        var response = new BaseCommandResponse<ManagedProviderClientProvisioningResultDto>
        {
            Success = true,
            Message = "Managed provider client already provisioned.",
            Id = new ManagedProviderClientProvisioningResultDto
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                TenantUserId = tenantUserBinding.InternalId,
                TenantUserProfileId = tenantUserProfileBinding.InternalId,
                UserActorId = actorBinding.InternalId,
                UserExternalLoginId = loginBinding.InternalId,
                TenantUserRoleGrantId = tenantUserRoleGrant.Id
            }
        };

        return response;
    }

    private async Task<ExternalBinding> EnsureExternalBindingAsync(
        string providerKey,
        string externalSystem,
        string externalType,
        string externalId,
        string internalType,
        Guid internalId,
        Guid? scopeTenantId,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var existing = await externalBindingRepository.GetByExternalKeyAsync(
            providerKey,
            externalSystem,
            externalType,
            externalId,
            scopeTenantId,
            cancellationToken);
        if (existing != null)
        {
            if (existing.InternalType != internalType || existing.InternalId != internalId)
            {
                throw new InvalidOperationException("External binding already points to a different internal record.");
            }

            existing.LastSeenAt = DateTime.UtcNow;
            await externalBindingRepository.Update(existing);
            return existing;
        }

        return await externalBindingRepository.Create(new ExternalBinding
        {
            Id = Guid.NewGuid(),
            ProviderKey = providerKey,
            ExternalSystem = externalSystem,
            ExternalType = externalType,
            ExternalId = externalId,
            InternalType = internalType,
            InternalId = internalId,
            ScopeTenantId = scopeTenantId,
            ExternalBindingStatusId = (int)ExternalBindingStatusEnum.Active,
            LastSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        });
    }

    private static BaseCommandResponse<ManagedProviderClientProvisioningResultDto> Failure(string message, string error) => new()
    {
        Success = false,
        Message = message,
        Errors = [error]
    };

    private static string ResolveDisplayName(ManagedProviderExternalAdminDto admin)
    {
        if (!string.IsNullOrWhiteSpace(admin.DisplayName)) return admin.DisplayName.Trim();

        var fullName = $"{admin.FirstName.Trim()} {admin.LastName.Trim()}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? admin.Email.Trim() : fullName;
    }

    private static string GenerateHandle(string name, string prefix)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? prefix : name.ToLowerInvariant();
        normalized = normalized.Replace(" ", "-").Replace("'", string.Empty).Replace("\"", string.Empty).Replace(".", string.Empty).Replace(",", string.Empty);
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, "[^a-z0-9-]", string.Empty);

        if (string.IsNullOrWhiteSpace(normalized)) normalized = prefix;
        if (normalized.Length > 20) normalized = normalized[..20];

        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{normalized}-{suffix}";
    }
}
