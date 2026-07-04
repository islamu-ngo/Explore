// ABOUTME: Deterministic support-access seed for Playwright E2E coverage.
// ABOUTME: Creates a Keycloak-linked instance admin, default tenant, and enabled support-access governance settings.

using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class SupportAccessScenarioSeed
{
    private const string AdminEmail = "admin@test.islamu.org";

    public sealed record Result(
        Guid TenantId,
        string TenantName,
        string TenantSlug,
        Guid AdminUserId);

    public static async Task<Result> SeedAsync(
        ExploreDbContext context,
        IReadOnlyCollection<string> adminProviderSubjects,
        Guid? adminUserId = null)
    {
        var normalizedAdminProviderSubjects = adminProviderSubjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Select(subject => subject.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedAdminProviderSubjects.Length == 0)
        {
            throw new ArgumentException("At least one admin provider subject is required.", nameof(adminProviderSubjects));
        }

        var now = DateTime.UtcNow;
        var tenant = EnsureDefaultTenant(context, now);
        var platformAdminRole = EnsurePlatformAdminRole(context);
        var adminUser = CreateAdminUser(now, normalizedAdminProviderSubjects[0], adminUserId);

        context.Users.Add(adminUser);
        context.UserExternalLogins.AddRange(normalizedAdminProviderSubjects.Select(subject =>
            CreateAdminExternalLogin(tenant, adminUser, subject, now)));
        await context.SaveChangesAsync();

        var adminActor = CreateAdminActor(tenant.Id, adminUser.Id, now);
        adminUser.ActorId = adminActor.Id;
        adminUser.DefaultActorId = adminActor.Id;

        context.Actors.Add(adminActor);
        context.PlatformUserRoles.Add(new PlatformUserRole
        {
            Id = Guid.CreateVersion7(),
            UserId = adminUser.Id,
            User = adminUser,
            RoleId = (int)RoleEnum.Admin,
            Role = platformAdminRole,
            GrantedAt = now
        });

        SeedSingleTenantBootstrapState(context, adminUser.Id, now);
        EnableSupportAccess(context, now);

        await context.SaveChangesAsync();

        return new Result(
            tenant.Id,
            tenant.FullName,
            tenant.Slug,
            adminUser.Id);
    }

    public static async Task GrantInstanceAdminAsync(ExploreDbContext context, Guid userId)
    {
        var now = DateTime.UtcNow;
        var platformAdminRole = EnsurePlatformAdminRole(context);
        var user = context.Users.Local.FirstOrDefault(candidate => candidate.Id == userId)
            ?? context.Users.FirstOrDefault(candidate => candidate.Id == userId);
        if (user is null)
        {
            throw new InvalidOperationException($"Cannot grant instance admin authority because user {userId} was not found.");
        }

        var hasPlatformRole = context.PlatformUserRoles.Local.Any(role =>
                role.UserId == userId && role.RoleId == (int)RoleEnum.Admin) ||
            context.PlatformUserRoles.Any(role => role.UserId == userId && role.RoleId == (int)RoleEnum.Admin);
        if (!hasPlatformRole)
        {
            context.PlatformUserRoles.Add(new PlatformUserRole
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                User = user,
                RoleId = (int)RoleEnum.Admin,
                Role = platformAdminRole,
                GrantedAt = now
            });
        }

        var bootstrap = context.InstanceBootstrapStates.Local.FirstOrDefault()
            ?? context.InstanceBootstrapStates.FirstOrDefault();
        if (bootstrap is not null)
        {
            bootstrap.CompletedByUserId = userId;
        }

        await context.SaveChangesAsync();
    }

    private static Role EnsurePlatformAdminRole(ExploreDbContext context)
    {
        var roleScope = context.RoleScopes.Local.FirstOrDefault(scope => scope.Id == (int)RoleScopeEnum.Platform)
            ?? context.RoleScopes.FirstOrDefault(scope => scope.Id == (int)RoleScopeEnum.Platform);
        if (roleScope is null)
        {
            roleScope = new RoleScope
            {
                Id = (int)RoleScopeEnum.Platform,
                MasterCode = "platform",
                FullName = "Platform"
            };
            context.RoleScopes.Add(roleScope);
        }
        else
        {
            roleScope.MasterCode = "platform";
            roleScope.FullName = "Platform";
        }

        var role = context.Roles.Local.FirstOrDefault(candidate => candidate.Id == (int)RoleEnum.Admin)
            ?? context.Roles.FirstOrDefault(candidate => candidate.Id == (int)RoleEnum.Admin);
        if (role is null)
        {
            role = new Role
            {
                Id = (int)RoleEnum.Admin,
                MasterCode = "platform.admin",
                FullName = "Platform Admin",
                RoleScopeId = (int)RoleScopeEnum.Platform,
                RoleScope = roleScope,
                IsSystem = true
            };
            context.Roles.Add(role);
        }
        else
        {
            role.MasterCode = "platform.admin";
            role.FullName = "Platform Admin";
            role.RoleScopeId = (int)RoleScopeEnum.Platform;
            role.RoleScope = roleScope;
            role.IsSystem = true;
        }

        return role;
    }

    private static Tenant EnsureDefaultTenant(ExploreDbContext context, DateTime now)
    {
        var tenant = context.Tenants.Local.FirstOrDefault(candidate => candidate.Id == PlatformDefaults.DefaultTenantId)
            ?? context.Tenants.FirstOrDefault(candidate => candidate.Id == PlatformDefaults.DefaultTenantId);
        if (tenant is not null)
        {
            tenant.FullName = PlatformDefaults.DefaultTenantName;
            tenant.Slug = PlatformDefaults.DefaultTenantSlug;
            tenant.TenantStatusId = (int)TenantStatusEnum.Active;
            tenant.UpdatedAt = now;
            return tenant;
        }

        tenant = new Tenant
        {
            Id = PlatformDefaults.DefaultTenantId,
            FullName = PlatformDefaults.DefaultTenantName,
            Slug = PlatformDefaults.DefaultTenantSlug,
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
            CreatedAt = now
        };

        context.Tenants.Add(tenant);
        return tenant;
    }

    private static User CreateAdminUser(DateTime now, string adminProviderSubject, Guid? adminUserId)
    {
        var userId = adminUserId ?? Guid.CreateVersion7();
        return new User
        {
            Id = userId,
            Pii = new UserPii
            {
                UserId = userId,
                Email = AdminEmail,
                FirstName = "Test",
                LastName = "Admin"
            },
            AuthProvider = AuthSchemeNames.Keycloak.ToLowerInvariant(),
            AuthProviderId = adminProviderSubject,
            EmailVerified = true,
            ConcurrencyStamp = Guid.CreateVersion7(),
            CreatedAt = now
        };
    }

    private static UserExternalLogin CreateAdminExternalLogin(
        Tenant tenant,
        User adminUser,
        string adminProviderSubject,
        DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            UserId = adminUser.Id,
            User = adminUser,
            TenantId = tenant.Id,
            Tenant = tenant,
            Provider = AuthSchemeNames.Keycloak.ToLowerInvariant(),
            ProviderKey = adminProviderSubject,
            ProviderDisplayName = AuthSchemeNames.Keycloak,
            CreatedAt = now
        };

    private static Actor CreateAdminActor(Guid tenantId, Guid userId, DateTime now)
    {
        var actorId = Guid.CreateVersion7();
        return new Actor
        {
            Id = actorId,
            TenantId = tenantId,
            Tenant = null!,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = userId,
            Pii = new ActorPii
            {
                ActorId = actorId,
                DisplayName = "Test Admin"
            },
            ConcurrencyStamp = Guid.CreateVersion7(),
            CreatedAt = now
        };
    }

    private static void SeedSingleTenantBootstrapState(ExploreDbContext context, Guid adminUserId, DateTime now)
    {
        context.InstanceBootstrapStates.Add(new InstanceBootstrapState
        {
            Id = Guid.CreateVersion7(),
            IsCompleted = true,
            CreatedAt = now,
            CompletedAt = now,
            CompletedByUserId = adminUserId,
            SelectedDeploymentMode = DeploymentMode.SingleTenant.ToString()
        });

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Deployment.Mode,
            $"\"{DeploymentMode.SingleTenant}\"",
            SettingValueType.String,
            "System",
            now);

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Routing.ResolverPathEnabled,
            "false",
            SettingValueType.Boolean,
            "Routing",
            now);

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Routing.PathPrefix,
            "\"\"",
            SettingValueType.String,
            "Routing",
            now);
    }

    private static void EnableSupportAccess(ExploreDbContext context, DateTime now)
    {
        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.SupportAccess.Enabled,
            "true",
            SettingValueType.Boolean,
            "SupportAccess",
            now);
        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.SupportAccess.RequireTicketReference,
            "true",
            SettingValueType.Boolean,
            "SupportAccess",
            now);
        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.SupportAccess.AllowWriteMode,
            "false",
            SettingValueType.Boolean,
            "SupportAccess",
            now);
        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.SupportAccess.MaxReadOnlyMinutes,
            "30",
            SettingValueType.Integer,
            "SupportAccess",
            now);
        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.SupportAccess.MaxWriteMinutes,
            "10",
            SettingValueType.Integer,
            "SupportAccess",
            now);
        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.SupportAccess.OneActiveSessionPerActor,
            "true",
            SettingValueType.Boolean,
            "SupportAccess",
            now);
    }

    private static void UpsertSystemSetting(
        ExploreDbContext context,
        string settingKey,
        string value,
        SettingValueType valueType,
        string category,
        DateTime now)
    {
        var setting = context.SystemSettings.Local.FirstOrDefault(x => x.SettingKey == settingKey)
            ?? context.SystemSettings.FirstOrDefault(x => x.SettingKey == settingKey);

        if (setting is null)
        {
            context.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.CreateVersion7(),
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                IsLocked = true,
                Category = category,
                Description = "Support-access E2E setting.",
                CreatedAt = now
            });

            return;
        }

        setting.Value = value;
        setting.ValueType = valueType;
        setting.IsLocked = true;
        setting.Category ??= category;
        setting.UpdatedAt = now;
    }
}
