// ABOUTME: Database seeding orchestrator. Seeds lookup tables in ALL environments at runtime.
// ABOUTME: In Development, also seeds business entities (users, orgs, events) for testing.

using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Modules;
using Explore.Domain.Settings.Documents;
using Explore.Persistence.QueryFilters;
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
        var shouldClearBypass = !context.IsTenantFilterBypassed;
        context.EnableTenantFilterBypass(TenantFilterBypassReasons.DatabaseSeeding);

        try
        {
            // Lookup tables are required in ALL environments
            await LookupTableSeeder.SeedAsync(context, cancellationToken);

            // Business entities are seeded only in Development for testing
            if (environment.IsDevelopment())
            {
                await SeedDevelopmentDataAsync(context, cancellationToken);
                await SeedDevelopmentSmtpAsync(context, cancellationToken);
            }

            await EnsureTenantBrandingDocumentsAsync(context, cancellationToken);
        }
        finally
        {
            if (shouldClearBypass)
            {
                context.ClearTenantFilterBypass();
            }
        }
    }


    private static async Task EnsureTenantBrandingDocumentsAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        var tenants = await context.Set<Tenant>()
            .AsNoTracking()
            .Select(tenant => new { tenant.Id, tenant.FullName })
            .ToListAsync(ct);

        if (tenants.Count == 0)
        {
            return;
        }

        var tenantIds = tenants.Select(tenant => tenant.Id).ToList();
        var existingTenantIds = await context.Set<TenantSettingsDocument>()
            .Where(document => document.DocumentKey == SettingsDocumentKeys.Tenant.Branding
                && tenantIds.Contains(document.TenantId))
            .Select(document => document.TenantId)
            .ToListAsync(ct);
        var existing = existingTenantIds.ToHashSet();

        var missingDocuments = tenants
            .Where(tenant => !existing.Contains(tenant.Id))
            .Select(tenant => TenantBrandingSettingsDocumentDefaults.Create(tenant.Id, tenant.FullName))
            .ToList();

        if (missingDocuments.Count == 0)
        {
            return;
        }

        context.Set<TenantSettingsDocument>().AddRange(missingDocuments);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Seeds comprehensive test data for Development environment.
    /// Handles circular FK between User/Organization ↔ Actor by inserting in phases.
    /// </summary>
    private static async Task SeedDevelopmentDataAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        var foundationExists = await context.Set<Tenant>().AnyAsync(t => t.Id == SeedIds.DefaultTenantId, ct);
        if (foundationExists)
        {
            await EnsureIslamicEventCatalogAsync(context, ct);
            return;
        }

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

        // Phase 6: Tenant users, tenant role grants, organization members, storage objects
        context.Set<TenantUser>().AddRange(
            SeedData.AdminTenantUser,
            SeedData.RegularTenantUser,
            SeedData.ModeratorTenantUser);

        context.Set<TenantUserRoleGrant>().AddRange(
            SeedData.AdminTenantUserRoleGrant,
            SeedData.RegularTenantUserRoleGrant,
            SeedData.ModeratorTenantUserRoleGrant);

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

        // Phase 7: Tenant capabilities
        context.Set<TenantCapability>().AddRange(
            SeedData.DefaultTenantCoreCapability,
            SeedData.DefaultTenantIslamicCapability);
        await context.SaveChangesAsync(ct);

        // Phase 8: Categories, tags, baseline location
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

        // Phase 9: Islamic event catalog (depends on actors, storage, tenant, categories, tags)
        await EnsureIslamicEventCatalogAsync(context, ct);
    }

    private static async Task EnsureIslamicEventCatalogAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        await EnsureIslamicEventLocationsAsync(context, ct);

        await AddMissingSeedRowsAsync(context, SeedData.IslamicEvents, e => e.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicEventAspects, aspect => aspect.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicEventDays, day => day.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicSessionGroups, group => group.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicEventSessions, session => session.Id, ct);
        await AddMissingSessionAspectsAsync(context, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicSessionGroupSessions, session => session.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicEventAgendaItems, item => item.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicSessionAgendaItems, item => item.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicEventCategories, category => category.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicEventTags, tag => tag.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicSessionCategories, category => category.Id, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicSessionTags, tag => tag.Id, ct);
        await AddMissingSessionLanguagesAsync(context, ct);
        await AddMissingSeedRowsAsync(context, SeedData.IslamicSessionSpeakers, speaker => speaker.Id, ct);
    }

    private static async Task AddMissingSeedRowsAsync<T>(
        ExploreDbContext context,
        IReadOnlyCollection<T> seedRows,
        Func<T, Guid> getId,
        CancellationToken ct)
        where T : class
    {
        if (seedRows.Count == 0)
        {
            return;
        }

        var seedIds = seedRows.Select(getId).ToArray();
        var existingIds = await context.Set<T>()
            .IgnoreQueryFilters()
            .Where(row => seedIds.Contains(EF.Property<Guid>(row, "Id")))
            .Select(row => EF.Property<Guid>(row, "Id"))
            .ToArrayAsync(ct);
        var existing = existingIds.ToHashSet();
        var missing = seedRows
            .Where(row => !existing.Contains(getId(row)))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        context.Set<T>().AddRange(missing);
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();
    }

    private static async Task AddMissingSessionLanguagesAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        var seedRows = SeedData.IslamicSessionLanguages;
        if (seedRows.Count == 0)
        {
            return;
        }

        var sessionIds = seedRows
            .Select(language => language.EventSessionId)
            .Distinct()
            .ToArray();
        var existingKeys = await context.Set<EventSessionLanguage>()
            .IgnoreQueryFilters()
            .Where(language => sessionIds.Contains(language.EventSessionId))
            .Select(language => new
            {
                language.EventSessionId,
                language.LanguageId
            })
            .ToArrayAsync(ct);
        var existing = existingKeys
            .Select(language => (language.EventSessionId, language.LanguageId))
            .ToHashSet();
        var missing = seedRows
            .Where(language => !existing.Contains((language.EventSessionId, language.LanguageId)))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        context.Set<EventSessionLanguage>().AddRange(missing);
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();
    }

    private static async Task AddMissingSessionAspectsAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        var seedRows = SeedData.IslamicSessionAspects;
        if (seedRows.Count == 0)
        {
            return;
        }

        var seedSessionIds = seedRows
            .Select(aspect => aspect.EventSessionId)
            .ToArray();
        var existingSessionIds = await context.Set<EventSessionIslamicAspect>()
            .Where(aspect => seedSessionIds.Contains(aspect.EventSessionId))
            .Select(aspect => aspect.EventSessionId)
            .ToArrayAsync(ct);
        var existing = existingSessionIds.ToHashSet();
        var missing = seedRows
            .Where(aspect => !existing.Contains(aspect.EventSessionId))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        context.Set<EventSessionIslamicAspect>().AddRange(missing);
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();
    }

    private static async Task EnsureIslamicEventLocationsAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        var locationIds = SeedData.IslamicEventLocations.Select(location => location.Id).ToList();
        var existingLocationIds = await context.Set<Location>()
            .Where(location => locationIds.Contains(location.Id))
            .Select(location => location.Id)
            .ToListAsync(ct);

        var existingLocations = existingLocationIds.ToHashSet();
        var missingLocations = SeedData.IslamicEventLocations
            .Where(location => !existingLocations.Contains(location.Id))
            .ToList();

        if (missingLocations.Count > 0)
        {
            context.Set<Location>().AddRange(missingLocations);
            await context.SaveChangesAsync(ct);
        }

        var roomIds = SeedData.IslamicEventRooms.Select(room => room.Id).ToList();
        var existingRoomIds = await context.Set<LocationRoom>()
            .Where(room => roomIds.Contains(room.Id))
            .Select(room => room.Id)
            .ToListAsync(ct);

        var existingRooms = existingRoomIds.ToHashSet();
        var missingRooms = SeedData.IslamicEventRooms
            .Where(room => !existingRooms.Contains(room.Id))
            .ToList();

        if (missingRooms.Count > 0)
        {
            context.Set<LocationRoom>().AddRange(missingRooms);
            await context.SaveChangesAsync(ct);
        }
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
            .FirstOrDefaultAsync(s => s.SettingKey == GovernanceSettingKeys.Email.SmtpHost, ct);

        if (hostSetting is null)
            return;

        var currentHost = hostSetting.Value?.Trim('"');
        if (!string.IsNullOrWhiteSpace(currentHost))
            return;

        var now = DateTime.UtcNow;

        await UpdateSettingValueAsync(context, GovernanceSettingKeys.Email.SmtpHost, "\"mailpit.openislamu.org\"", now, ct);
        await UpdateSettingValueAsync(context, GovernanceSettingKeys.Email.SmtpPort, "8025", now, ct);
        await UpdateSettingValueAsync(context, GovernanceSettingKeys.Email.SmtpSecurity, "\"None\"", now, ct);
        await UpdateSettingValueAsync(context, GovernanceSettingKeys.Email.FromAddress, "\"noreply@explore.dev\"", now, ct);
        await UpdateSettingValueAsync(context, GovernanceSettingKeys.Email.FromName, "\"Explore Dev\"", now, ct);

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
