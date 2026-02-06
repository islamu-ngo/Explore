// ABOUTME: Database seeding infrastructure for conditional and runtime-dependent data.
// Provides development-only seeding separate from lookup table HasData().

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Explore.Persistence.Seed;

/// <summary>
/// Database seeder for conditional/runtime-dependent data.
///
/// Architecture:
/// - Lookup/enum tables: Seeded via HasData() in configurations (always applied via migrations)
/// - Business entities: Seeded via this class (conditionally applied at runtime)
///
/// Business entity seeding is ONLY applied in Development environment.
/// Production databases start empty (except lookup tables) and are populated via API/UI.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds development data if in Development environment.
    /// Called after migrations in the application startup.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="environment">The hosting environment to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task SeedAsync(
        ExploreDbContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        // Only seed business entities in Development
        if (!environment.IsDevelopment())
        {
            return;
        }

        await SeedDevelopmentDataAsync(context, cancellationToken);
    }

    /// <summary>
    /// Seeds development/demo data for local development and testing.
    /// This data is NOT applied in Production - production starts with empty business entities.
    /// </summary>
    private static async Task SeedDevelopmentDataAsync(
        ExploreDbContext context,
        CancellationToken cancellationToken)
    {
        // Check if already seeded (idempotent)
        if (await context.Tenants.AnyAsync(t => t.Id == SeedIds.DefaultTenantId, cancellationToken))
        {
            return;
        }

        // Seed in dependency order
        await SeedTenantsAsync(context, cancellationToken);
        await SeedUsersAsync(context, cancellationToken);
        await SeedOrganizationsAsync(context, cancellationToken);
        await SeedActorsAsync(context, cancellationToken);
        await SeedOrganizationMembersAsync(context, cancellationToken);
        await SeedStorageObjectsAsync(context, cancellationToken);
        await SeedTenantSettingsAsync(context, cancellationToken);
        await SeedTenantCapabilitiesAsync(context, cancellationToken);
        await SeedLocationsAsync(context, cancellationToken);
        await SeedCategoriesAsync(context, cancellationToken);
        await SeedTagsAsync(context, cancellationToken);
        await SeedUserRolesAsync(context, cancellationToken);
        await SeedSampleEventsAsync(context, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedTenantsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Tenants.AnyAsync(ct))
        {
            context.Tenants.Add(SeedData.DefaultTenant);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedUsersAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Users.AnyAsync(ct))
        {
            context.Users.Add(SeedData.SystemUser);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedOrganizationsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Organizations.AnyAsync(ct))
        {
            context.Organizations.Add(SeedData.IslamuOrganization);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedActorsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Actors.AnyAsync(ct))
        {
            context.Actors.AddRange(
                SeedData.SystemUserActor,
                SeedData.IslamuOrganizationActor);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedOrganizationMembersAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.OrganizationMembers.AnyAsync(ct))
        {
            context.OrganizationMembers.Add(SeedData.SystemUserIslamuMember);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedStorageObjectsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.StorageObjects.AnyAsync(ct))
        {
            context.StorageObjects.AddRange(
                SeedData.DefaultEventImage,
                SeedData.DefaultProfileImage,
                SeedData.DefaultOrganizationLogo);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedTenantSettingsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Set<TenantSettings>().AnyAsync(ct))
        {
            context.Set<TenantSettings>().Add(SeedData.DefaultTenantSettings);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedTenantCapabilitiesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Set<TenantCapability>().AnyAsync(ct))
        {
            context.Set<TenantCapability>().AddRange(
                SeedData.DefaultTenantCoreCapability,
                SeedData.DefaultTenantIslamicCapability);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedLocationsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Locations.AnyAsync(ct))
        {
            context.Locations.Add(SeedData.OnlineLocation);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedCategoriesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Categories.AnyAsync(ct))
        {
            // Add parent categories first
            context.Categories.AddRange(
                SeedData.IslamicStudiesCategory,
                SeedData.ArabicLanguageCategory,
                SeedData.CommunityEventsCategory);
            await context.SaveChangesAsync(ct);

            // Add child categories
            context.Categories.AddRange(
                SeedData.QuranCategory,
                SeedData.HadithCategory,
                SeedData.FiqhCategory,
                SeedData.AqeedahCategory,
                SeedData.SeerahCategory);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedTagsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Tags.AnyAsync(ct))
        {
            context.Tags.AddRange(
                SeedData.BeginnerTag,
                SeedData.IntermediateTag,
                SeedData.AdvancedTag,
                SeedData.FreeTag,
                SeedData.PaidTag,
                SeedData.OnlineTag,
                SeedData.InPersonTag);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedUserRolesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.UserRoles.AnyAsync(ct))
        {
            context.UserRoles.AddRange(
                SeedData.SuperAdminRole,
                SeedData.AdminRole,
                SeedData.ModeratorRole,
                SeedData.UserRoleData);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedSampleEventsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (!await context.Events.AnyAsync(ct))
        {
            context.Events.Add(SeedData.SampleEvent);
            await context.SaveChangesAsync(ct);
        }
    }
}
