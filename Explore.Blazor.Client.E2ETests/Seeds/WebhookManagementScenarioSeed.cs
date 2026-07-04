// ABOUTME: Deterministic single-tenant webhook management seed for browser E2E coverage.
// ABOUTME: Creates an instance admin, canonical webhook rows, and retryable delivery state.

using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class WebhookManagementScenarioSeed
{
    private const string AdminEmail = "admin@test.islamu.org";

    public sealed record Result(
        Guid TenantId,
        Guid AdminUserId,
        Guid LocalConsumerId,
        Guid SvixConsumerId,
        Guid DryRunEndpointId,
        Guid ExistingEndpointId,
        Guid FailedAttemptId,
        string DryRunEndpointUrl,
        string ExistingEndpointUrl);

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

        var eventPublished = CreateEventType("event.published", "event", "Event becomes publicly visible.", now);
        var webhookTest = CreateEventType("webhook.test", "webhook", "Endpoint test delivery.", now);
        context.WebhookEventTypes.AddRange(eventPublished, webhookTest);

        var localConsumer = CreateConsumer(tenant.Id, "Operations local bridge", WebhookProviderMode.Local, now);
        var svixConsumer = CreateConsumer(tenant.Id, "Enterprise Svix bridge", WebhookProviderMode.Svix, now);
        var dryRunConsumer = CreateConsumer(tenant.Id, "DryRun verification bridge", WebhookProviderMode.DryRun, now);
        context.WebhookConsumers.AddRange(localConsumer, svixConsumer, dryRunConsumer);

        var existingEndpoint = CreateEndpoint(
            tenant.Id,
            localConsumer.Id,
            "https://hooks.example.test/islamu-existing",
            "Existing local endpoint",
            "secrets/webhooks/e2e-local-v1",
            now);
        context.WebhookEndpoints.Add(existingEndpoint);
        context.WebhookEndpointSubscriptions.Add(new WebhookEndpointSubscription
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            EndpointId = existingEndpoint.Id,
            EventTypeId = eventPublished.Id,
            IsEnabled = true,
            CreatedAt = now
        });

        var dryRunEndpoint = CreateEndpoint(
            tenant.Id,
            dryRunConsumer.Id,
            "https://hooks.example.test/dryrun-no-outbound",
            "DryRun endpoint",
            "secrets/webhooks/e2e-dryrun-v1",
            now);
        context.WebhookEndpoints.Add(dryRunEndpoint);
        context.WebhookEndpointSubscriptions.Add(new WebhookEndpointSubscription
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            EndpointId = dryRunEndpoint.Id,
            EventTypeId = eventPublished.Id,
            IsEnabled = true,
            CreatedAt = now
        });

        var failedMessage = new WebhookMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            EventType = "event.published",
            EventId = Guid.CreateVersion7().ToString("D"),
            AggregateKind = "Event",
            AggregateId = Guid.CreateVersion7(),
            ConsumerId = localConsumer.Id,
            PayloadJson = """{"id":"seeded","type":"event.published","version":1,"data":{"eventId":"seeded"}}""",
            PayloadHash = "sha256:e2e-seeded-message",
            PayloadRetentionUntil = now.AddDays(14),
            ProviderMode = WebhookProviderMode.Local,
            Status = WebhookMessageStatus.Failed,
            CreatedAt = now.AddMinutes(-10),
            PublishedAt = now.AddMinutes(-9)
        };
        context.WebhookMessages.Add(failedMessage);

        var failedAttempt = new WebhookDeliveryAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            MessageId = failedMessage.Id,
            EndpointId = existingEndpoint.Id,
            AttemptNumber = 1,
            Status = WebhookDeliveryAttemptStatus.Failed,
            ScheduledAt = now.AddMinutes(-9),
            SentAt = now.AddMinutes(-8),
            CompletedAt = now.AddMinutes(-8).AddSeconds(1),
            HttpStatusCode = 500,
            FailureCategory = "http_non_success",
            DurationMs = 1200,
            CreatedAt = now.AddMinutes(-9)
        };
        context.WebhookDeliveryAttempts.Add(failedAttempt);

        await context.SaveChangesAsync();

        return new Result(
            tenant.Id,
            adminUser.Id,
            localConsumer.Id,
            svixConsumer.Id,
            dryRunEndpoint.Id,
            existingEndpoint.Id,
            failedAttempt.Id,
            dryRunEndpoint.Url,
            existingEndpoint.Url);
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
        var tenant = new Tenant
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

    private static WebhookEventType CreateEventType(
        string name,
        string groupName,
        string description,
        DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            GroupName = groupName,
            Description = description,
            SchemaJson = "{}",
            SchemaVersion = 1,
            IsPublic = true,
            IsEnabled = true,
            PayloadRetentionDays = 14,
            CreatedAt = now
        };

    private static WebhookConsumer CreateConsumer(
        Guid tenantId,
        string name,
        WebhookProviderMode providerMode,
        DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.SystemIntegration,
            Name = name,
            Status = WebhookConsumerStatus.Active,
            ProviderMode = providerMode,
            ExternalProviderAppId = providerMode == WebhookProviderMode.Svix ? $"app_{Guid.CreateVersion7():N}" : null,
            CreatedAt = now
        };

    private static WebhookEndpoint CreateEndpoint(
        Guid tenantId,
        Guid consumerId,
        string url,
        string description,
        string secretRef,
        DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerId = consumerId,
            Url = url,
            Description = description,
            Status = WebhookEndpointStatus.Active,
            SecretRef = secretRef,
            SecretVersion = 1,
            MaxAttempts = 8,
            TimeoutSeconds = 15,
            RateLimitPerMinute = 60,
            CreatedAt = now
        };

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
                Category = category,
                CreatedAt = now
            });

            return;
        }

        setting.Value = value;
        setting.ValueType = valueType;
        setting.Category ??= category;
        setting.UpdatedAt = now;
    }
}
