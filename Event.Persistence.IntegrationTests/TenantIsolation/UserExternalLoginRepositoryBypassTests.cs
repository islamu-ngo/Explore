// ABOUTME: Verifies user external-login authentication bypass is bounded by provider and provider key.
// ABOUTME: Proves cross-tenant identity resolution does not leak unrelated ambient tenant login rows.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.TenantIsolation;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public class UserExternalLoginRepositoryBypassTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task GetByProviderAndKey_WithAmbientTenant_ReturnsOnlyExactExternalLogin()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();

        var tenantA = CreateTenant("external-login-a");
        var tenantB = CreateTenant("external-login-b");
        seedContext.Tenants.AddRange(tenantA, tenantB);
        await seedContext.SaveChangesAsync();

        var userA = CreateUser("external-login-a");
        var userB = CreateUser("external-login-b");
        seedContext.Users.AddRange(userA, userB);
        await seedContext.SaveChangesAsync();

        var tenantALogin = CreateExternalLogin(tenantA.Id, userA.Id, "keycloak", "subject-a");
        var tenantBLogin = CreateExternalLogin(tenantB.Id, userB.Id, "keycloak", "subject-b");
        seedContext.UserExternalLogins.AddRange(tenantALogin, tenantBLogin);
        await seedContext.SaveChangesAsync();

        await using var tenantBContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantB.Id));
        var visibleWithoutBypass = await tenantBContext.UserExternalLogins
            .AsNoTracking()
            .Select(login => login.Id)
            .ToListAsync();

        var repository = new UserExternalLoginRepository(tenantBContext);
        var tenantAByProviderKey = await repository.GetByProviderAndKey("keycloak", "subject-a");
        var wrongProvider = await repository.GetByProviderAndKey("google", "subject-a");
        var tenantAByUserWithoutBypass = await repository.GetByUser(userA.Id);

        await Assert.That(visibleWithoutBypass).IsEquivalentTo([tenantBLogin.Id]);
        await Assert.That(tenantAByProviderKey).IsNotNull();
        await Assert.That(tenantAByProviderKey!.Id).IsEqualTo(tenantALogin.Id);
        await Assert.That(tenantAByProviderKey.TenantId).IsEqualTo(tenantA.Id);
        await Assert.That(wrongProvider).IsNull();
        await Assert.That(tenantAByUserWithoutBypass).IsEmpty();
    }

    private static Tenant CreateTenant(string slugPrefix)
    {
        return new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = $"External Login {slugPrefix}",
            Slug = $"{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
    }

    private static User CreateUser(string emailPrefix)
    {
        return new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com",
                FirstName = "External",
                LastName = "Login",
            },
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static UserExternalLogin CreateExternalLogin(
        Guid tenantId,
        Guid userId,
        string provider,
        string providerKey)
    {
        return new UserExternalLogin
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            Provider = provider,
            ProviderKey = providerKey,
            ProviderDisplayName = provider,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}
