// ABOUTME: PostgreSQL-backed tests for tenant-local user participation state.
// ABOUTME: Verifies tenant isolation and authority gating independently from global User records.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class TenantUserRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task TenantUsers_ShouldAllowSameGlobalUserInMultipleTenantsButOnlyOncePerTenant()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenantA = await SeedTenantAsync(context, "tenant-user-a");
        var tenantB = await SeedTenantAsync(context, "tenant-user-b");
        var user = NewUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.TenantUsers.Add(NewTenantUser(tenantA.Id, user.Id, TenantUserStatusEnum.Active));
        context.TenantUsers.Add(NewTenantUser(tenantB.Id, user.Id, TenantUserStatusEnum.Suspended));
        await context.SaveChangesAsync();

        context.TenantUsers.Add(NewTenantUser(tenantA.Id, user.Id, TenantUserStatusEnum.Active));

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task TenantMemberAuthority_ShouldRequireActiveTenantUserStatePerTenant()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var activeTenant = await SeedTenantAsync(context, "authority-active");
        var suspendedTenant = await SeedTenantAsync(context, "authority-suspended");
        var bannedTenant = await SeedTenantAsync(context, "authority-banned");
        var removedTenant = await SeedTenantAsync(context, "authority-removed");
        var user = NewUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.TenantUsers.AddRange(
            NewTenantUser(activeTenant.Id, user.Id, TenantUserStatusEnum.Active),
            NewTenantUser(suspendedTenant.Id, user.Id, TenantUserStatusEnum.Suspended),
            NewTenantUser(bannedTenant.Id, user.Id, TenantUserStatusEnum.Banned),
            NewTenantUser(removedTenant.Id, user.Id, TenantUserStatusEnum.Removed));
        context.TenantMembers.AddRange(
            NewTenantAdmin(activeTenant.Id, user.Id),
            NewTenantAdmin(suspendedTenant.Id, user.Id),
            NewTenantAdmin(bannedTenant.Id, user.Id),
            NewTenantAdmin(removedTenant.Id, user.Id));
        await context.SaveChangesAsync();
        var repository = new TenantMemberRepository(context);

        await Assert.That(await repository.IsTenantMember(activeTenant.Id, user.Id)).IsTrue();
        await Assert.That(await repository.IsTenantAdmin(activeTenant.Id, user.Id)).IsTrue();
        await Assert.That(await repository.IsTenantMember(suspendedTenant.Id, user.Id)).IsFalse();
        await Assert.That(await repository.IsTenantAdmin(suspendedTenant.Id, user.Id)).IsFalse();
        await Assert.That(await repository.IsTenantMember(bannedTenant.Id, user.Id)).IsFalse();
        await Assert.That(await repository.IsTenantAdmin(bannedTenant.Id, user.Id)).IsFalse();
        await Assert.That(await repository.IsTenantMember(removedTenant.Id, user.Id)).IsFalse();
        await Assert.That(await repository.IsTenantAdmin(removedTenant.Id, user.Id)).IsFalse();
    }

    [Test]
    public async Task TenantMemberAuthority_ShouldIgnoreSoftDeletedTenantUserWithoutChangingGlobalUser()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "authority-deleted");
        var user = NewUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var tenantUser = NewTenantUser(tenant.Id, user.Id, TenantUserStatusEnum.Active);
        tenantUser.IsDeleted = true;
        tenantUser.DeletedAt = DateTime.UtcNow;
        context.TenantUsers.Add(tenantUser);
        context.TenantMembers.Add(NewTenantAdmin(tenant.Id, user.Id));
        await context.SaveChangesAsync();
        var repository = new TenantMemberRepository(context);

        await Assert.That(await repository.IsTenantMember(tenant.Id, user.Id)).IsFalse();
        await Assert.That(await repository.IsTenantAdmin(tenant.Id, user.Id)).IsFalse();
        var persistedUser = await context.Users.IgnoreQueryFilters().SingleAsync(x => x.Id == user.Id);
        await Assert.That(persistedUser.IsDeleted).IsFalse();
        await Assert.That(persistedUser.Pii.Email).IsEqualTo(user.Pii.Email);
    }

    private static async Task<Tenant> SeedTenantAsync(ExploreDbContext context, string slugPrefix)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = $"Tenant User {slugPrefix}",
            Slug = $"tenant-user-{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant;
    }

    private static User NewUser() =>
        new()
        {
            Id = Guid.NewGuid(),
            Pii = new UserPii
            {
                Email = $"tenant-user-{Guid.NewGuid():N}@example.com",
                FirstName = "Amina",
                LastName = "Admin",
            },
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
        };

    private static TenantUser NewTenantUser(Guid tenantId, Guid userId, TenantUserStatusEnum status) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            StatusId = (int)status,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

    private static TenantMember NewTenantAdmin(Guid tenantId, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            RoleId = (int)RoleEnum.TenantAdmin,
            Role = null!,
            GrantedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
}
