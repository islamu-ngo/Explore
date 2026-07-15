// ABOUTME: PostgreSQL-backed tests for DB-enforced group hierarchy invariants.
// ABOUTME: Verifies same-tenant parent FKs, parent exclusivity, self-parent checks, and bounded ancestry helpers.

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
public class GroupHierarchyConstraintTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task ActorTypeLookup_ShouldContainEveryDefinedActorType()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var seededIds = await context.Set<ActorType>()
            .Select(actorType => actorType.Id)
            .ToArrayAsync();

        var missingIds = Enum.GetValues<ActorTypeEnum>()
            .Select(actorType => (int)actorType)
            .Except(seededIds)
            .ToArray();

        await Assert.That(missingIds).IsEmpty();
    }

    [Test]
    public async Task RoleLookup_ShouldContainEveryDefinedRole()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var seededIds = await context.Roles
            .Select(role => role.Id)
            .ToArrayAsync();

        var missingIds = Enum.GetValues<RoleEnum>()
            .Select(role => (int)role)
            .Except(seededIds)
            .ToArray();

        await Assert.That(missingIds).IsEmpty();
    }

    [Test]
    public async Task Group_ShouldRejectDualParentReferences()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "dual-parent");
        var organization = await SeedOrganizationAsync(context, tenant.Id, "Dual Parent Org");
        var parentGroup = await SeedGroupAsync(context, tenant.Id, "Dual Parent Group");

        var group = NewGroup(tenant.Id, "Invalid Dual Parent");
        group.ParentOrganizationId = organization.Id;
        group.ParentGroupId = parentGroup.Id;
        context.Groups.Add(group);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task Group_ShouldRejectSelfParentReference()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "self-parent");
        var group = NewGroup(tenant.Id, "Self Parent Group");
        group.Id = Guid.NewGuid();
        group.ParentGroupId = group.Id;
        context.Groups.Add(group);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task Group_ShouldRejectParentOrganizationFromDifferentTenant()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenantA = await SeedTenantAsync(context, "tenant-a");
        var tenantB = await SeedTenantAsync(context, "tenant-b");
        var foreignOrganization = await SeedOrganizationAsync(context, tenantB.Id, "Foreign Org");

        var group = NewGroup(tenantA.Id, "Cross Tenant Org Parent");
        group.ParentOrganizationId = foreignOrganization.Id;
        context.Groups.Add(group);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task Group_ShouldAllowRootGroupWithoutParents()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "root");

        var group = await SeedGroupAsync(context, tenant.Id, "Root Group");

        await Assert.That(group.ParentOrganizationId).IsNull();
        await Assert.That(group.ParentGroupId).IsNull();
    }

    [Test]
    public async Task Group_ShouldAllowParentOrganizationFromSameTenant()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "same-org");
        var organization = await SeedOrganizationAsync(context, tenant.Id, "Same Tenant Org");

        var group = NewGroup(tenant.Id, "Org Child Group");
        group.ParentOrganizationId = organization.Id;
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        await Assert.That(group.ParentOrganizationId).IsEqualTo(organization.Id);
    }

    [Test]
    public async Task Group_ShouldAllowParentGroupFromSameTenant()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "same-group");
        var parentGroup = await SeedGroupAsync(context, tenant.Id, "Same Tenant Parent");

        var child = await SeedGroupAsync(context, tenant.Id, "Same Tenant Child", parentGroup.Id);

        await Assert.That(child.ParentGroupId).IsEqualTo(parentGroup.Id);
    }

    [Test]
    public async Task Group_ShouldRejectParentGroupFromDifferentTenant()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenantA = await SeedTenantAsync(context, "tenant-group-a");
        var tenantB = await SeedTenantAsync(context, "tenant-group-b");
        var foreignGroup = await SeedGroupAsync(context, tenantB.Id, "Foreign Group");

        var group = NewGroup(tenantA.Id, "Cross Tenant Group Parent");
        group.ParentGroupId = foreignGroup.Id;
        context.Groups.Add(group);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task WouldCreateHierarchyCycle_ShouldDetectParentAncestryCycle()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "cycle");
        var root = await SeedGroupAsync(context, tenant.Id, "Root");
        var child = await SeedGroupAsync(context, tenant.Id, "Child", root.Id);
        var repository = new GroupRepository(context);

        var wouldCreateCycle = await repository.WouldCreateHierarchyCycle(
            root.Id,
            child.Id,
            tenant.Id,
            CancellationToken.None);

        await Assert.That(wouldCreateCycle).IsTrue();
    }

    [Test]
    public async Task WouldExceedHierarchyDepth_ShouldDetectParentChainAtMaximumDepth()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "depth");
        Guid? parentGroupId = null;

        for (var depth = 1; depth <= GroupHierarchyRules.MaxDepth; depth++)
        {
            var group = await SeedGroupAsync(context, tenant.Id, $"Depth {depth}", parentGroupId);
            parentGroupId = group.Id;
        }

        var repository = new GroupRepository(context);

        var wouldExceedDepth = await repository.WouldExceedHierarchyDepth(
            parentGroupId,
            tenant.Id,
            GroupHierarchyRules.MaxDepth,
            CancellationToken.None);

        await Assert.That(wouldExceedDepth).IsTrue();
    }

    [Test]
    public async Task WouldExceedHierarchyDepth_ShouldAllowParentChainBelowMaximumDepth()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "depth-allowed");
        Guid? parentGroupId = null;

        for (var depth = 1; depth < GroupHierarchyRules.MaxDepth; depth++)
        {
            var group = await SeedGroupAsync(context, tenant.Id, $"Allowed Depth {depth}", parentGroupId);
            parentGroupId = group.Id;
        }

        var repository = new GroupRepository(context);

        var wouldExceedDepth = await repository.WouldExceedHierarchyDepth(
            parentGroupId,
            tenant.Id,
            GroupHierarchyRules.MaxDepth,
            CancellationToken.None);

        await Assert.That(wouldExceedDepth).IsFalse();
    }

    [Test]
    public async Task WouldExceedHierarchyDepthForMove_ShouldDetectSubtreeDepthOverflow()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "move-depth");
        Guid? deepParentGroupId = null;

        for (var depth = 1; depth < GroupHierarchyRules.MaxDepth; depth++)
        {
            var group = await SeedGroupAsync(context, tenant.Id, $"Deep Parent {depth}", deepParentGroupId);
            deepParentGroupId = group.Id;
        }

        var movingRoot = await SeedGroupAsync(context, tenant.Id, "Moving Root");
        await SeedGroupAsync(context, tenant.Id, "Moving Child", movingRoot.Id);
        var repository = new GroupRepository(context);

        var wouldExceedDepth = await repository.WouldExceedHierarchyDepthForMove(
            movingRoot.Id,
            deepParentGroupId,
            tenant.Id,
            GroupHierarchyRules.MaxDepth,
            CancellationToken.None);

        await Assert.That(wouldExceedDepth).IsTrue();
    }

    [Test]
    public async Task ExecuteWithHierarchyMutationLock_ShouldRunOperationInsideTransaction()
    {
        await fixture.ResetAsync();
        Guid tenantId;
        await using (var seedContext = fixture.CreateDbContext())
        {
            tenantId = (await SeedTenantAsync(seedContext, "lock")).Id;
        }

        await using var context = CreateRetryingDbContext();
        var repository = new GroupRepository(context);

        var createdId = await repository.ExecuteWithHierarchyMutationLock(
            tenantId,
            async token =>
            {
                var group = NewGroup(tenantId, "Locked Mutation");
                context.Groups.Add(group);
                await context.SaveChangesAsync(token);
                return group.Id;
            },
            CancellationToken.None);

        var exists = await context.Groups.AnyAsync(group => group.Id == createdId);
        await Assert.That(exists).IsTrue();
    }

    private ExploreDbContext CreateRetryingDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention()
            .Options;

        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Retry-enabled group hierarchy mutation integration test.");
        return context;
    }

    private static async Task<Tenant> SeedTenantAsync(ExploreDbContext context, string slugPrefix)
    {
        var tenant = new Tenant
        {
            FullName = $"Group Hierarchy {slugPrefix}",
            Slug = $"grp-hierarchy-{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = 2,
            TenantStatus = null!,
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant;
    }

    private static async Task<Organization> SeedOrganizationAsync(
        ExploreDbContext context,
        Guid tenantId,
        string fullName)
    {
        var organization = new Organization
        {
            Pii = new OrganizationPii { FullName = fullName },
            ApprovalStatusId = 1,
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid(),
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        return organization;
    }

    private static async Task<Group> SeedGroupAsync(
        ExploreDbContext context,
        Guid tenantId,
        string fullName,
        Guid? parentGroupId = null)
    {
        var group = NewGroup(tenantId, fullName);
        group.ParentGroupId = parentGroupId;
        context.Groups.Add(group);
        await context.SaveChangesAsync();
        return group;
    }

    private static Group NewGroup(Guid tenantId, string fullName)
    {
        return new Group
        {
            FullName = fullName,
            ApprovalStatusId = 1,
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid(),
        };
    }
}
