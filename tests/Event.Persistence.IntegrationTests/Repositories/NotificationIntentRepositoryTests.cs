// ABOUTME: Verifies normalized notification intent persistence and tenant isolation.
// ABOUTME: Exercises lookup-backed ownership, delivery, and external delegation repository paths.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public class NotificationIntentRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task LookupSeeder_SeedsNotificationIntentLookupTables()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();

        var categories = await context.NotificationCategories.AsNoTracking().CountAsync();
        var ownershipTypes = await context.NotificationOwnershipTypes.AsNoTracking().CountAsync();
        var intentStatuses = await context.NotificationIntentStatuses.AsNoTracking().CountAsync();
        var recipientKinds = await context.NotificationRecipientKinds.AsNoTracking().CountAsync();
        var deliveryStatuses = await context.NotificationDeliveryStatuses.AsNoTracking().CountAsync();
        var delegationStatuses = await context.NotificationExternalDelegationStatuses.AsNoTracking().CountAsync();
        var providerKinds = await context.ExternalWorkflowProviderKinds.AsNoTracking().CountAsync();
        var accountAuthorityKinds = await context.AccountAuthorityKinds.AsNoTracking().CountAsync();

        await Assert.That(categories).IsEqualTo(Enum.GetValues<NotificationCategoryEnum>().Length);
        await Assert.That(ownershipTypes).IsEqualTo(Enum.GetValues<NotificationOwnershipTypeEnum>().Length);
        await Assert.That(intentStatuses).IsEqualTo(Enum.GetValues<NotificationIntentStatusEnum>().Length);
        await Assert.That(recipientKinds).IsEqualTo(Enum.GetValues<NotificationRecipientKindEnum>().Length);
        await Assert.That(deliveryStatuses).IsEqualTo(Enum.GetValues<NotificationDeliveryStatusEnum>().Length);
        await Assert.That(delegationStatuses).IsEqualTo(Enum.GetValues<NotificationExternalDelegationStatusEnum>().Length);
        await Assert.That(providerKinds).IsEqualTo(Enum.GetValues<ExternalWorkflowProviderKindEnum>().Length);
        await Assert.That(accountAuthorityKinds).IsEqualTo(Enum.GetValues<AccountAuthorityKindEnum>().Length);
    }

    [Test]
    public async Task CreateIntentAsync_PersistsNormalizedIntentAndFindsExactTenantDeduplicationKey()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();

        var tenantA = CreateTenant("intent-a");
        var tenantB = CreateTenant("intent-b");
        var recipient = CreateTenantRecipient(tenantA, "intent-a");
        context.Tenants.AddRange(tenantA, tenantB);
        context.TenantUsers.Add(recipient.TenantUser);
        await context.SaveChangesAsync();

        var repository = new NotificationIntentRepository(context);
        var intent = CreateIntent(tenantA.Id, recipient.User.Id, "registration-approved:shared");

        var created = await repository.CreateIntentAsync(intent, CancellationToken.None);

        var matchingTenantExists = await repository.ExistsByDeduplicationKeyAsync(
            tenantA.Id,
            "registration-approved:shared",
            CancellationToken.None);
        var otherTenantDoesNotMatch = await repository.ExistsByDeduplicationKeyAsync(
            tenantB.Id,
            "registration-approved:shared",
            CancellationToken.None);
        var loaded = await repository.GetByTenantAndIdAsync(tenantA.Id, created.Id, CancellationToken.None);

        await Assert.That(created.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(matchingTenantExists).IsTrue();
        await Assert.That(otherTenantDoesNotMatch).IsFalse();
        await Assert.That(loaded).IsNotNull();
        await Assert.That(loaded!.CategoryId).IsEqualTo((int)NotificationCategoryEnum.RegistrationLifecycle);
        await Assert.That(loaded.OwnershipTypeId).IsEqualTo((int)NotificationOwnershipTypeEnum.IslamuEvent);
        await Assert.That(loaded.SafePayloadReference).IsEqualTo("notification-intents/registration-approved");
    }

    [Test]
    public async Task CreateGraphAsync_MapsOnlyNotificationIntentPrimaryKeyReplayToRecoverableConflict()
    {
        await fixture.ResetAsync();
        Guid stableIntentId = Guid.CreateVersion7();
        Guid tenantId;
        Guid recipientUserId;
        await using (var seedContext = fixture.CreateDbContext())
        {
            Tenant tenant = CreateTenant("intent-primary-key-replay");
            (User User, TenantUser TenantUser) recipient = CreateTenantRecipient(
                tenant,
                "intent-primary-key-replay");
            seedContext.Tenants.Add(tenant);
            seedContext.TenantUsers.Add(recipient.TenantUser);
            NotificationIntent committed = CreateIntent(
                tenant.Id,
                recipient.User.Id,
                "notification-intent:committed");
            committed.Id = stableIntentId;
            seedContext.NotificationIntents.Add(committed);
            await seedContext.SaveChangesAsync();
            tenantId = tenant.Id;
            recipientUserId = recipient.User.Id;
        }

        await using var retryContext = fixture.CreateDbContext();
        var repository = new NotificationIntentRepository(retryContext);
        NotificationIntent retry = CreateIntent(
            tenantId,
            recipientUserId,
            "notification-intent:retry-with-different-deduplication-key");
        retry.Id = stableIntentId;

        await Assert.ThrowsAsync<NotificationIntentDeduplicationConflictException>(() =>
            repository.CreateGraphAsync(retry, CancellationToken.None));
    }

    [Test]
    public async Task TenantFilter_ReturnsOnlyCurrentTenantNotificationIntents()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();

        var tenantA = CreateTenant("intent-filter-a");
        var tenantB = CreateTenant("intent-filter-b");
        var tenantARecipient = CreateTenantRecipient(tenantA, "intent-filter-a");
        var tenantBRecipient = CreateTenantRecipient(tenantB, "intent-filter-b");
        seedContext.Tenants.AddRange(tenantA, tenantB);
        seedContext.TenantUsers.AddRange(tenantARecipient.TenantUser, tenantBRecipient.TenantUser);
        await seedContext.SaveChangesAsync();

        var tenantAIntent = CreateIntent(tenantA.Id, tenantARecipient.User.Id, "tenant-a:intent");
        var tenantBIntent = CreateIntent(tenantB.Id, tenantBRecipient.User.Id, "tenant-b:intent");
        seedContext.NotificationIntents.AddRange(tenantAIntent, tenantBIntent);
        await seedContext.SaveChangesAsync();

        await using var tenantAContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantA.Id));
        var visibleIntentIds = await tenantAContext.NotificationIntents
            .AsNoTracking()
            .Select(intent => intent.Id)
            .ToListAsync();

        await Assert.That(visibleIntentIds).IsEquivalentTo([tenantAIntent.Id]);
    }

    [Test]
    public async Task AddDeliveryAndExternalDelegationAsync_PersistsAuditRowsForIntent()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();

        var tenant = CreateTenant("intent-audit");
        var recipient = CreateTenantRecipient(tenant, "intent-audit");
        context.Tenants.Add(tenant);
        context.TenantUsers.Add(recipient.TenantUser);
        await context.SaveChangesAsync();

        var repository = new NotificationIntentRepository(context);
        var intent = await repository.CreateIntentAsync(
            CreateIntent(tenant.Id, recipient.User.Id, "moderation-decision:audit"),
            CancellationToken.None);

        var delivery = new NotificationDelivery
        {
            TenantId = tenant.Id,
            NotificationIntentId = intent.Id,
            ChannelId = (int)NotificationPreferenceChannelEnum.Email,
            DeliveryPolicyId = (int)NotificationDeliveryPolicyEnum.RegistrationStatusOptional,
            IsRequired = false,
            PolicyVersion = 1,
            DisclosureLevel = "generic",
            TemplateKey = "registration.approved",
            TemplateVersion = 1,
            StatusId = (int)NotificationDeliveryStatusEnum.Queued,
            ProviderMessageId = "smtp-message-id-redacted",
            ProviderStatus = "queued",
            QueuedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        var delegation = new NotificationExternalDelegation
        {
            TenantId = tenant.Id,
            NotificationIntentId = intent.Id,
            ProviderKindId = (int)ExternalWorkflowProviderKindEnum.Coop,
            StatusId = (int)NotificationExternalDelegationStatusEnum.Requested,
            RecipientKindId = (int)NotificationRecipientKindEnum.Moderator,
            TemplateKey = "moderation.decision.delegate",
            SafePayloadHash = "sha256:moderation-safe-payload",
            ExternalProviderId = "coop-case-123",
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        await repository.AddDeliveryAsync(delivery, CancellationToken.None);
        await repository.AddExternalDelegationAsync(delegation, CancellationToken.None);

        var persistedDelivery = await context.NotificationDeliveries
            .AsNoTracking()
            .SingleAsync(row => row.NotificationIntentId == intent.Id);
        var persistedDelegation = await context.NotificationExternalDelegations
            .AsNoTracking()
            .SingleAsync(row => row.NotificationIntentId == intent.Id);

        await Assert.That(persistedDelivery.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Queued);
        await Assert.That(persistedDelegation.ProviderKindId).IsEqualTo((int)ExternalWorkflowProviderKindEnum.Coop);
        await Assert.That(persistedDelegation.SafePayloadHash).IsEqualTo("sha256:moderation-safe-payload");
    }

    [Test]
    public async Task AddExternalDelegationAsync_PersistsNormalizedAccountAuthorityAudit()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();

        var tenant = CreateTenant("intent-authority");
        var recipient = CreateTenantRecipient(tenant, "intent-authority");
        context.Tenants.Add(tenant);
        context.TenantUsers.Add(recipient.TenantUser);
        await context.SaveChangesAsync();

        var repository = new NotificationIntentRepository(context);
        var intent = await repository.CreateIntentAsync(
            CreateIntent(tenant.Id, recipient.User.Id, "identity-lifecycle:authority"),
            CancellationToken.None);

        var delegation = new NotificationExternalDelegation
        {
            TenantId = tenant.Id,
            NotificationIntentId = intent.Id,
            ProviderKindId = (int)ExternalWorkflowProviderKindEnum.None,
            AccountAuthorityKindId = (int)AccountAuthorityKindEnum.Keycloak,
            StatusId = (int)NotificationExternalDelegationStatusEnum.Requested,
            RecipientKindId = (int)NotificationRecipientKindEnum.User,
            TemplateKey = "identity.verify-email",
            SafePayloadHash = "sha256:identity-safe-payload",
            ExternalProviderId = "keycloak-required-action",
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        await repository.AddExternalDelegationAsync(delegation, CancellationToken.None);

        var persistedDelegation = await context.NotificationExternalDelegations
            .Include(row => row.AccountAuthorityKind)
            .AsNoTracking()
            .SingleAsync(row => row.NotificationIntentId == intent.Id);

        await Assert.That(persistedDelegation.ProviderKindId).IsEqualTo((int)ExternalWorkflowProviderKindEnum.None);
        await Assert.That(persistedDelegation.AccountAuthorityKindId).IsEqualTo((int)AccountAuthorityKindEnum.Keycloak);
        await Assert.That(persistedDelegation.AccountAuthorityKind!.MasterCode).IsEqualTo("KEYCLOAK");
    }

    private static Tenant CreateTenant(string slugPrefix)
    {
        return new Tenant
        {
            FullName = $"Notification Intent {slugPrefix}",
            Slug = $"{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
    }

    private static (User User, TenantUser TenantUser) CreateTenantRecipient(Tenant tenant, string emailPrefix)
    {
        DateTime createdAt = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"{emailPrefix}-{Guid.NewGuid():N}@example.test",
                FirstName = "Notification",
                LastName = "Recipient",
            },
            EmailVerified = true,
            CreatedAt = createdAt,
        };
        var tenantUser = new TenantUser
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = tenant,
            UserId = user.Id,
            User = user,
            StatusId = (int)TenantUserStatusEnum.Active,
            JoinedAt = createdAt,
            CreatedAt = createdAt,
        };

        return (user, tenantUser);
    }

    private static NotificationIntent CreateIntent(
        Guid tenantId,
        Guid recipientUserId,
        string deduplicationKey)
    {
        return new NotificationIntent
        {
            TenantId = tenantId,
            RecipientUserId = recipientUserId,
            CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
            OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
            RecipientKindId = (int)NotificationRecipientKindEnum.User,
            StatusId = (int)NotificationIntentStatusEnum.Pending,
            TemplateKey = "registration.approved",
            DeduplicationKey = deduplicationKey,
            SafePayloadReference = "notification-intents/registration-approved",
            SafePayloadHash = "sha256:registration-safe-payload",
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
        };
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}
