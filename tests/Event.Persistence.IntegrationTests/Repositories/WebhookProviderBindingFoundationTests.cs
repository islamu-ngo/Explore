// ABOUTME: PostgreSQL integration tests for persisted webhook provider-binding authority.
// ABOUTME: Verifies tenant ownership, normalized identities, lookup parity, and fenced writes.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class WebhookProviderBindingFoundationTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task VerifiedLookup_NormalizesIdentityAndRejectsStaleVersionOrFence()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = CreateTenant("binding-verified");
        var otherTenant = CreateTenant("binding-other");
        var consumer = CreateConsumer(tenant.Id);
        context.Tenants.AddRange(tenant, otherTenant);
        context.WebhookConsumers.Add(consumer);
        await context.SaveChangesAsync();

        var repository = new WebhookConsumerProviderBindingRepository(context);
        var binding = CreatePendingBinding(tenant.Id, consumer.Id, "Production");
        await repository.CreateAsync(binding, CancellationToken.None);

        var verified = await repository.TryVerifyAsync(
            tenant.Id,
            binding.Id,
            expectedConcurrencyVersion: 1,
            expectedVerificationFence: 1,
            "app_Custom",
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        var byConsumer = await repository.GetVerifiedByConsumerAsync(
            tenant.Id,
            consumer.Id,
            WebhookProviderKind.Svix,
            "  production  ",
            CancellationToken.None);
        var byProviderIdentity = await repository.GetVerifiedByProviderIdentityAsync(
            tenant.Id,
            WebhookProviderKind.Svix,
            "PRODUCTION",
            " APP_custom ",
            binding.ApplicationUid.ToUpperInvariant(),
            CancellationToken.None);
        var substitutedTenant = await repository.GetVerifiedByConsumerAsync(
            otherTenant.Id,
            consumer.Id,
            WebhookProviderKind.Svix,
            "production",
            CancellationToken.None);

        await Assert.That(verified).IsTrue();
        await Assert.That(byConsumer).IsNotNull();
        await Assert.That(byProviderIdentity?.Id).IsEqualTo(binding.Id);
        await Assert.That(substitutedTenant).IsNull();
        await Assert.That(byConsumer!.VerificationStateId)
            .IsEqualTo((int)WebhookProviderBindingVerificationState.Verified);
        await Assert.That(byConsumer.ConcurrencyVersion).IsEqualTo(2);
        await Assert.That(byConsumer.VerificationFence).IsEqualTo(2);

        var staleVersion = await repository.TryDisableAsync(
            tenant.Id,
            binding.Id,
            expectedConcurrencyVersion: 1,
            expectedVerificationFence: 2,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        var staleFence = await repository.TryDisableAsync(
            tenant.Id,
            binding.Id,
            expectedConcurrencyVersion: 2,
            expectedVerificationFence: 1,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        var disabled = await repository.TryDisableAsync(
            tenant.Id,
            binding.Id,
            expectedConcurrencyVersion: 2,
            expectedVerificationFence: 2,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        var staleRebind = await repository.TryRebindAsync(
            tenant.Id,
            binding.Id,
            expectedConcurrencyVersion: 2,
            expectedVerificationFence: 2,
            "app_rebound",
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        var rebound = await repository.TryRebindAsync(
            tenant.Id,
            binding.Id,
            expectedConcurrencyVersion: 3,
            expectedVerificationFence: 3,
            "app_rebound",
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        await Assert.That(staleVersion).IsFalse();
        await Assert.That(staleFence).IsFalse();
        await Assert.That(disabled).IsTrue();
        await Assert.That(staleRebind).IsFalse();
        await Assert.That(rebound).IsTrue();
    }

    [Test]
    public async Task CompositeConsumerForeignKey_RejectsCrossTenantBinding()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var consumerTenant = CreateTenant("binding-consumer-tenant");
        var bindingTenant = CreateTenant("binding-foreign-tenant");
        var consumer = CreateConsumer(consumerTenant.Id);
        context.Tenants.AddRange(consumerTenant, bindingTenant);
        context.WebhookConsumers.Add(consumer);
        await context.SaveChangesAsync();

        context.WebhookConsumerProviderBindings.Add(
            CreatePendingBinding(bindingTenant.Id, consumer.Id, "production"));

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task NormalizedConsumerProviderEnvironmentIdentity_RejectsDuplicates()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = CreateTenant("binding-duplicate");
        var consumer = CreateConsumer(tenant.Id);
        context.Tenants.Add(tenant);
        context.WebhookConsumers.Add(consumer);
        await context.SaveChangesAsync();

        context.WebhookConsumerProviderBindings.AddRange(
            CreatePendingBinding(tenant.Id, consumer.Id, "Production"),
            CreatePendingBinding(tenant.Id, consumer.Id, " production "));

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task VerificationLookup_EnumRuntimeRowsAndTenantFilterRemainAligned()
    {
        await fixture.ResetAsync();
        await using var setupContext = fixture.CreateDbContext();
        var tenant = CreateTenant("binding-filter");
        var consumer = CreateConsumer(tenant.Id);
        setupContext.Tenants.Add(tenant);
        setupContext.WebhookConsumers.Add(consumer);
        setupContext.WebhookConsumerProviderBindings.Add(
            CreatePendingBinding(tenant.Id, consumer.Id, "production"));
        await setupContext.SaveChangesAsync();

        var expectedIds = Enum.GetValues<WebhookProviderBindingVerificationState>()
            .Select(state => (int)state)
            .Order()
            .ToArray();
        var databaseIds = await setupContext.WebhookProviderBindingVerificationStates
            .AsNoTracking()
            .Select(state => state.Id)
            .Order()
            .ToArrayAsync();
        await using var noTenantContext = fixture.CreateTenantFilteredDbContext();
        await using var tenantContext = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(tenant.Id));

        await Assert.That(databaseIds).IsEquivalentTo(expectedIds);
        await Assert.That(await noTenantContext.WebhookConsumerProviderBindings.CountAsync()).IsEqualTo(0);
        await Assert.That(await tenantContext.WebhookConsumerProviderBindings.CountAsync()).IsEqualTo(1);
    }

    private static WebhookConsumerProviderBinding CreatePendingBinding(
        Guid tenantId,
        Guid consumerId,
        string environment)
    {
        var profile = WebhookProviderCapabilityProfile.Create(
            WebhookProviderKind.Svix,
            "1.84.0",
            WebhookProviderCapability.AppPortal | WebhookProviderCapability.EndpointManagement,
            "svix-1.84.0-v1",
            DateTimeOffset.UtcNow);

        return WebhookConsumerProviderBinding.CreatePending(
            tenantId,
            consumerId,
            Guid.CreateVersion7(),
            environment,
            profile,
            WebhookProviderCapability.AppPortal | WebhookProviderCapability.EndpointManagement);
    }

    private static Tenant CreateTenant(string slugPrefix) => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = "Webhook Binding Test Tenant",
        Slug = $"{slugPrefix}-{Guid.NewGuid():N}",
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!
    };

    private static WebhookConsumer CreateConsumer(Guid tenantId) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        ConsumerKind = WebhookConsumerKind.Tenant,
        Name = $"Binding Consumer {Guid.NewGuid():N}",
        Status = WebhookConsumerStatus.Active,
        ProviderMode = WebhookProviderMode.Svix
    };

    private sealed class StaticTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }
}
