// ABOUTME: Database seeding orchestrator. Seeds lookup tables in ALL environments at runtime.
// ABOUTME: In Development, also seeds business entities (users, orgs, events) for testing.

using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Modules;
using Explore.Domain.Settings.Documents;
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

        await EnsureTenantBrandingDocumentsAsync(context, cancellationToken);
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
        // Foundation entities are seeded once; the event catalog is reseeded below so dev databases
        // receive catalog updates without requiring a full reset.
        var foundationExists = await context.Set<Tenant>().AnyAsync(t => t.Id == SeedIds.DefaultTenantId, ct);
        if (foundationExists)
        {
            await SeedIslamicEventCatalogAsync(context, ct);
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
        await SeedIslamicEventCatalogAsync(context, ct);
    }

    private static async Task SeedIslamicEventCatalogAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        await RemovePreviousDevelopmentEventCatalogAsync(context, ct);
        await EnsureIslamicEventLocationsAsync(context, ct);

        context.Set<Event>().AddRange(SeedData.IslamicEvents);
        await context.SaveChangesAsync(ct);

        context.Set<EventIslamicAspect>().AddRange(SeedData.IslamicEventAspects);
        context.Set<EventDay>().AddRange(SeedData.IslamicEventDays);
        context.Set<EventSessionGroup>().AddRange(SeedData.IslamicSessionGroups);
        await context.SaveChangesAsync(ct);

        context.Set<EventSession>().AddRange(SeedData.IslamicEventSessions);
        await context.SaveChangesAsync(ct);

        context.Set<EventSessionIslamicAspect>().AddRange(SeedData.IslamicSessionAspects);
        context.Set<EventSessionGroupSession>().AddRange(SeedData.IslamicSessionGroupSessions);
        context.Set<EventAgendaItem>().AddRange(SeedData.IslamicEventAgendaItems);
        context.Set<EventSessionAgendaItem>().AddRange(SeedData.IslamicSessionAgendaItems);
        context.Set<EventCategories>().AddRange(SeedData.IslamicEventCategories);
        context.Set<EventTags>().AddRange(SeedData.IslamicEventTags);
        context.Set<EventSessionCategory>().AddRange(SeedData.IslamicSessionCategories);
        context.Set<EventSessionTag>().AddRange(SeedData.IslamicSessionTags);
        context.Set<EventSessionLanguage>().AddRange(SeedData.IslamicSessionLanguages);
        context.Set<EventSessionSpeaker>().AddRange(SeedData.IslamicSessionSpeakers);
        await context.SaveChangesAsync(ct);
    }

    private static async Task RemovePreviousDevelopmentEventCatalogAsync(
        ExploreDbContext context,
        CancellationToken ct)
    {
        context.ChangeTracker.Clear();

        var catalogEventIds = SeedIds.IslamicEventCatalogIds
            .Concat([SeedIds.SampleEventId])
            .ToArray();
        var catalogSessionIds = SeedData.IslamicEventSessions
            .Select(session => session.Id)
            .ToArray();
        var catalogSessionGroupIds = SeedData.IslamicSessionGroups
            .Select(group => group.Id)
            .ToArray();
        var catalogRegistrationIntentIds = await context.Set<EventRegistrationIntent>()
            .IgnoreQueryFilters()
            .Where(intent => catalogEventIds.Contains(intent.EventId))
            .Select(intent => intent.Id)
            .ToArrayAsync(ct);
        var catalogContactShareExportIds = await context.Set<EventContactShareExport>()
            .IgnoreQueryFilters()
            .Where(export => export.EventId.HasValue && catalogEventIds.Contains(export.EventId.Value))
            .Select(export => export.Id)
            .ToArrayAsync(ct);

        await context.Set<EventContactShareExportItem>()
            .Where(item => catalogContactShareExportIds.Contains(item.ExportId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventContactShareExport>()
            .IgnoreQueryFilters()
            .Where(export => export.EventId.HasValue && catalogEventIds.Contains(export.EventId.Value))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventContactShareConsent>()
            .IgnoreQueryFilters()
            .Where(consent => consent.SourceEventId.HasValue && catalogEventIds.Contains(consent.SourceEventId.Value))
            .ExecuteDeleteAsync(ct);
        var catalogEmailDispatchOutboxIds = await context.Set<EmailDispatchOutbox>()
            .IgnoreQueryFilters()
            .Where(outbox => (outbox.EventId.HasValue && catalogEventIds.Contains(outbox.EventId.Value))
                || (outbox.RegistrationIntentId.HasValue
                    && catalogRegistrationIntentIds.Contains(outbox.RegistrationIntentId.Value)))
            .Select(outbox => outbox.Id)
            .ToArrayAsync(ct);

        await context.Set<EmailDispatchReceipt>()
            .Where(receipt => catalogEmailDispatchOutboxIds.Contains(receipt.EmailDispatchOutboxId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EmailDispatchAttempt>()
            .Where(attempt => catalogEmailDispatchOutboxIds.Contains(attempt.EmailDispatchOutboxId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EmailDispatchOutbox>()
            .IgnoreQueryFilters()
            .Where(outbox => catalogEmailDispatchOutboxIds.Contains(outbox.Id))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventRegistration>()
            .IgnoreQueryFilters()
            .Where(registration => catalogSessionIds.Contains(registration.EventSessionId)
                || (registration.EventRegistrationIntentId.HasValue
                    && catalogRegistrationIntentIds.Contains(registration.EventRegistrationIntentId.Value)))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventRegistrationIntent>()
            .IgnoreQueryFilters()
            .Where(intent => catalogEventIds.Contains(intent.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventRoleAssignment>()
            .IgnoreQueryFilters()
            .Where(assignment => catalogEventIds.Contains(assignment.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionCustomPropertyProjection>()
            .Where(projection => catalogSessionIds.Contains(projection.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionCustomPropertyValue>()
            .IgnoreQueryFilters()
            .Where(value => catalogSessionIds.Contains(value.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionCustomPropertyDefinition>()
            .IgnoreQueryFilters()
            .Where(definition => catalogSessionIds.Contains(definition.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionSpeaker>()
            .IgnoreQueryFilters()
            .Where(speaker => catalogSessionIds.Contains(speaker.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionLanguage>()
            .IgnoreQueryFilters()
            .Where(language => catalogSessionIds.Contains(language.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionTag>()
            .IgnoreQueryFilters()
            .Where(tag => catalogSessionIds.Contains(tag.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionCategory>()
            .IgnoreQueryFilters()
            .Where(category => catalogSessionIds.Contains(category.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionAgendaItem>()
            .IgnoreQueryFilters()
            .Where(item => catalogSessionIds.Contains(item.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionGroupSession>()
            .IgnoreQueryFilters()
            .Where(session => catalogSessionIds.Contains(session.EventSessionId)
                || catalogSessionGroupIds.Contains(session.EventSessionGroupId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionIslamicAspect>()
            .IgnoreQueryFilters()
            .Where(aspect => catalogSessionIds.Contains(aspect.EventSessionId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSession>()
            .IgnoreQueryFilters()
            .Where(session => catalogEventIds.Contains(session.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventAgendaItem>()
            .IgnoreQueryFilters()
            .Where(item => catalogEventIds.Contains(item.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventSessionGroup>()
            .IgnoreQueryFilters()
            .Where(group => catalogEventIds.Contains(group.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventIslamicAspect>()
            .IgnoreQueryFilters()
            .Where(aspect => catalogEventIds.Contains(aspect.Id))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventCategories>()
            .IgnoreQueryFilters()
            .Where(category => catalogEventIds.Contains(category.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventTags>()
            .IgnoreQueryFilters()
            .Where(tag => catalogEventIds.Contains(tag.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventDay>()
            .IgnoreQueryFilters()
            .Where(day => catalogEventIds.Contains(day.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventCustomPropertyProjection>()
            .Where(projection => catalogEventIds.Contains(projection.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventCustomPropertyValue>()
            .IgnoreQueryFilters()
            .Where(value => catalogEventIds.Contains(value.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<EventCustomPropertyDefinition>()
            .IgnoreQueryFilters()
            .Where(definition => catalogEventIds.Contains(definition.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Set<OrganizationReview>()
            .IgnoreQueryFilters()
            .Where(review => catalogEventIds.Contains(review.EventId))
            .ExecuteDeleteAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM events
            WHERE id = {SeedIds.SampleEventId}
                OR slug = {"welcome-to-islamu-events"}
                OR id = ANY({SeedIds.IslamicEventCatalogIds})
            """, ct);

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
