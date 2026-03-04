// ABOUTME: Database seeding orchestrator. Seeds lookup tables in ALL environments at runtime.
// ABOUTME: In Development, also seeds business entities (users, orgs, events) for testing.

using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Explore.Persistence.Seed;

/// <summary>
/// Database seeder orchestrator. Called after migrations in application startup.
///
/// ALL environments: Seeds lookup/enum tables via LookupTableSeeder.
/// Development only: Seeds business entities (tenant, users, organizations, members, events)
/// so developers don't have to manually create test data.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ExploreDbContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        // Lookup tables are required in ALL environments
        await LookupTableSeeder.SeedAsync(context, cancellationToken);

        // Business entities are seeded only in Development for testing
        if (environment.IsDevelopment())
        {
            await SeedDevelopmentDataAsync(context, cancellationToken);
            await SeedDevelopmentSmtpAsync(context, cancellationToken);
        }
    }

    /// <summary>
    /// Seeds comprehensive test data for Development environment.
    /// Handles circular FK between User/Organization ↔ Actor by inserting in phases.
    /// </summary>
    private static async Task SeedDevelopmentDataAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        // Idempotent: skip if dev data already exists
        if (await context.Set<Tenant>().AnyAsync(t => t.Id == SeedIds.DefaultTenantId, ct))
            return;

        // Phase 1: Tenant (foundation for all tenant-scoped entities)
        context.Set<Tenant>().Add(SeedData.DefaultTenant);
        await context.SaveChangesAsync(ct);

        // Phase 2: Users without ActorId (circular dependency — Actor references User)
        var adminUser = SeedData.AdminUser;
        var regularUser = SeedData.RegularUser;
        var moderatorUser = SeedData.ModeratorUser;
        context.Set<User>().AddRange(adminUser, regularUser, moderatorUser);
        await context.SaveChangesAsync(ct);

        // Phase 3: Organizations without ActorId (circular dependency — Actor references Organization)
        var islamuOrg = SeedData.IslamuOrg;
        var techOrg = SeedData.TechOrg;
        context.Set<Organization>().AddRange(islamuOrg, techOrg);
        await context.SaveChangesAsync(ct);

        // Phase 4: Actors (now Users + Organizations exist for FK references)
        context.Set<Actor>().AddRange(
            SeedData.AdminUserActor,
            SeedData.RegularUserActor,
            SeedData.ModeratorUserActor,
            SeedData.IslamuOrgActor,
            SeedData.TechOrgActor);
        await context.SaveChangesAsync(ct);

        // Phase 5: Resolve circular dependency — set ActorId on Users and Organizations
        adminUser.ActorId = SeedIds.AdminUserActorId;
        regularUser.ActorId = SeedIds.RegularUserActorId;
        moderatorUser.ActorId = SeedIds.ModeratorUserActorId;
        islamuOrg.ActorId = SeedIds.IslamuOrgActorId;
        techOrg.ActorId = SeedIds.TechOrgActorId;
        await context.SaveChangesAsync(ct);

        // Phase 6: Tenant members, organization members, storage objects
        context.Set<TenantMember>().AddRange(
            SeedData.AdminTenantMember,
            SeedData.RegularTenantMember,
            SeedData.ModeratorTenantMember);

        context.Set<OrganizationMember>().AddRange(
            SeedData.AdminIslamuCreator,
            SeedData.RegularIslamuMember,
            SeedData.ModeratorIslamuMod,
            SeedData.AdminTechCoOwner,
            SeedData.RegularTechCreator);

        context.Set<StorageObject>().AddRange(
            SeedData.DefaultEventImage,
            SeedData.DefaultProfileImage,
            SeedData.DefaultOrganizationLogo);
        await context.SaveChangesAsync(ct);

        // Phase 7: Tenant settings and capabilities
        context.Set<TenantSettings>().Add(SeedData.DefaultTenantSettings);
        context.Set<TenantCapability>().AddRange(
            SeedData.DefaultTenantCoreCapability,
            SeedData.DefaultTenantIslamicCapability);
        await context.SaveChangesAsync(ct);

        // Phase 8: Categories, tags, location
        context.Set<Location>().Add(SeedData.OnlineLocation);

        context.Set<Category>().AddRange(
            SeedData.IslamicStudiesCategory,
            SeedData.QuranCategory,
            SeedData.HadithCategory,
            SeedData.FiqhCategory,
            SeedData.AqeedahCategory,
            SeedData.SeerahCategory,
            SeedData.ArabicLanguageCategory,
            SeedData.CommunityEventsCategory);

        context.Set<Tag>().AddRange(
            SeedData.BeginnerTag,
            SeedData.IntermediateTag,
            SeedData.AdvancedTag,
            SeedData.FreeTag,
            SeedData.PaidTag,
            SeedData.OnlineTag,
            SeedData.InPersonTag);
        await context.SaveChangesAsync(ct);

        // Phase 9: Sample event (depends on actors, storage, tenant)
        context.Set<Event>().Add(SeedData.SampleEvent);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Pre-configures Mailpit SMTP in Development for email testing.
    /// Mailpit captures all outbound emails for inspection without delivery.
    /// Same SmtpEmailService code path is used in production with real SMTP credentials.
    /// Idempotent: only applies when SMTP host is empty (not yet manually configured).
    /// </summary>
    private static async Task SeedDevelopmentSmtpAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        var hostSetting = await context.Set<SystemSetting>()
            .FirstOrDefaultAsync(s => s.SettingKey == GovernanceSettingKeys.EmailSmtpHost, ct);

        if (hostSetting is null)
            return;

        var currentHost = hostSetting.Value?.Trim('"');
        if (!string.IsNullOrWhiteSpace(currentHost))
            return;

        var now = DateTime.UtcNow;

        await UpdateSettingValueAsync(context, GovernanceSettingKeys.EmailSmtpHost, "\"mailpit.openislamu.org\"", now, ct);
        await UpdateSettingValueAsync(context, GovernanceSettingKeys.EmailSmtpPort, "1025", now, ct);
        await UpdateSettingValueAsync(context, GovernanceSettingKeys.EmailSmtpSecurity, "\"None\"", now, ct);
        await UpdateSettingValueAsync(context, GovernanceSettingKeys.EmailFromAddress, "\"noreply@explore.dev\"", now, ct);
        await UpdateSettingValueAsync(context, GovernanceSettingKeys.EmailFromName, "\"Explore Dev\"", now, ct);

        await context.SaveChangesAsync(ct);
    }

    private static async Task UpdateSettingValueAsync(
        ExploreDbContext context,
        string key,
        string value,
        DateTime timestamp,
        CancellationToken ct)
    {
        var setting = await context.Set<SystemSetting>()
            .FirstOrDefaultAsync(s => s.SettingKey == key, ct);

        if (setting is not null)
        {
            setting.Value = value;
            setting.UpdatedAt = timestamp;
        }
    }
}
