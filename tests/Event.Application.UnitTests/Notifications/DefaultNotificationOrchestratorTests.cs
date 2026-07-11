// ABOUTME: Unit tests for notification orchestration over ownership resolution and durable intent persistence.
// ABOUTME: Locks local, delegated, disabled, and validation paths without invoking delivery providers.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Enums;
using AppAccountAuthorityKind = Explore.Application.Notifications.AccountAuthorityKind;
using AppExternalWorkflowProviderKind = Explore.Application.Notifications.ExternalWorkflowProviderKind;
using AppNotificationCategory = Explore.Application.Notifications.NotificationCategory;
using AppNotificationOwnership = Explore.Application.Notifications.NotificationOwnership;

namespace Event.Application.UnitTests.Notifications;

public sealed class DefaultNotificationOrchestratorTests
{
    [Test]
    public async Task EnqueueAsync_CreatesPendingLocalDeliveryForIslamuOwnedNotification()
    {
        var repository = new CapturingNotificationIntentRepository();
        var orchestrator = CreateOrchestrator(
            repository,
            new NotificationOwnershipDecision(AppNotificationCategory.RegistrationLifecycle, AppNotificationOwnership.IslamuEvent));

        var result = await orchestrator.EnqueueAsync(CreateDraft(AppNotificationCategory.RegistrationLifecycle));

        await Assert.That(result.Intent.StatusId).IsEqualTo((int)NotificationIntentStatusEnum.Pending);
        await Assert.That(result.Intent.OwnershipTypeId).IsEqualTo((int)NotificationOwnershipTypeEnum.IslamuEvent);
        await Assert.That(result.Delivery).IsNotNull();
        await Assert.That(result.Delivery!.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Pending);
        await Assert.That(result.ExternalDelegation).IsNull();
        await Assert.That(repository.CreatedIntent).IsSameReferenceAs(result.Intent);
    }

    [Test]
    public async Task EnqueueAsync_CreatesAccountAuthorityDelegationWhenIslamuInitiated()
    {
        var repository = new CapturingNotificationIntentRepository();
        var orchestrator = CreateOrchestrator(
            repository,
            new NotificationOwnershipDecision(
                AppNotificationCategory.IdentityLifecycle,
                AppNotificationOwnership.AccountAuthority,
                AppAccountAuthorityKind.Keycloak,
                RequiresLocalAudit: true));

        var result = await orchestrator.EnqueueAsync(CreateDraft(AppNotificationCategory.IdentityLifecycle));

        await Assert.That(result.Intent.StatusId).IsEqualTo((int)NotificationIntentStatusEnum.Delegated);
        await Assert.That(result.Delivery).IsNull();
        await Assert.That(result.ExternalDelegation).IsNotNull();
        await Assert.That(result.ExternalDelegation!.ProviderKindId).IsEqualTo((int)ExternalWorkflowProviderKindEnum.None);
        await Assert.That(result.ExternalDelegation.AccountAuthorityKindId).IsEqualTo((int)AccountAuthorityKindEnum.Keycloak);
        await Assert.That(result.ExternalDelegation.StatusId).IsEqualTo((int)NotificationExternalDelegationStatusEnum.Requested);
    }

    [Test]
    public async Task EnqueueAsync_ResolvesAccountAuthorityWithoutDelegationWhenNotIslamuInitiated()
    {
        var repository = new CapturingNotificationIntentRepository();
        var orchestrator = CreateOrchestrator(
            repository,
            new NotificationOwnershipDecision(
                AppNotificationCategory.IdentityLifecycle,
                AppNotificationOwnership.AccountAuthority,
                AppAccountAuthorityKind.AtprotoPds,
                RequiresLocalAudit: false));

        var result = await orchestrator.EnqueueAsync(CreateDraft(
            AppNotificationCategory.IdentityLifecycle,
            isIslamuInitiated: false));

        await Assert.That(result.Intent.StatusId).IsEqualTo((int)NotificationIntentStatusEnum.Resolved);
        await Assert.That(result.Delivery).IsNull();
        await Assert.That(result.ExternalDelegation).IsNull();
    }

    [Test]
    public async Task EnqueueAsync_CreatesExternalWorkflowDelegationForAuditedProviderNotification()
    {
        var repository = new CapturingNotificationIntentRepository();
        var orchestrator = CreateOrchestrator(
            repository,
            new NotificationOwnershipDecision(
                AppNotificationCategory.TrustSafetyModeration,
                AppNotificationOwnership.ExternalWorkflowProvider,
                ExternalWorkflowProviderKind: AppExternalWorkflowProviderKind.Coop,
                RequiresLocalAudit: true));

        var result = await orchestrator.EnqueueAsync(CreateDraft(AppNotificationCategory.TrustSafetyModeration));

        await Assert.That(result.Intent.StatusId).IsEqualTo((int)NotificationIntentStatusEnum.Delegated);
        await Assert.That(result.ExternalDelegation).IsNotNull();
        await Assert.That(result.ExternalDelegation!.ProviderKindId).IsEqualTo((int)ExternalWorkflowProviderKindEnum.Coop);
        await Assert.That(result.ExternalDelegation.AccountAuthorityKindId).IsNull();
        await Assert.That(result.ExternalDelegation.SafePayloadHash).IsEqualTo("sha256:test-safe-payload");
    }

    [Test]
    public async Task EnqueueAsync_CreatesSkippedIntentForDisabledNotification()
    {
        var repository = new CapturingNotificationIntentRepository();
        var orchestrator = CreateOrchestrator(
            repository,
            new NotificationOwnershipDecision(AppNotificationCategory.Marketing, AppNotificationOwnership.Disabled, RequiresLocalAudit: false));

        var result = await orchestrator.EnqueueAsync(CreateDraft(AppNotificationCategory.Marketing));

        await Assert.That(result.Intent.StatusId).IsEqualTo((int)NotificationIntentStatusEnum.Skipped);
        await Assert.That(result.Delivery).IsNull();
        await Assert.That(result.ExternalDelegation).IsNull();
    }

    [Test]
    public async Task EnqueueAsync_RequiresTenantTemplateAndDeduplicationKey()
    {
        var orchestrator = CreateOrchestrator(
            new CapturingNotificationIntentRepository(),
            new NotificationOwnershipDecision(AppNotificationCategory.RegistrationLifecycle, AppNotificationOwnership.IslamuEvent));

        await Assert.That(async () => await orchestrator.EnqueueAsync(CreateDraft(AppNotificationCategory.RegistrationLifecycle) with
        {
            TenantId = null
        })).Throws<InvalidOperationException>();
        await Assert.That(async () => await orchestrator.EnqueueAsync(CreateDraft(AppNotificationCategory.RegistrationLifecycle) with
        {
            TemplateKey = " "
        })).Throws<InvalidOperationException>();
        await Assert.That(async () => await orchestrator.EnqueueAsync(CreateDraft(AppNotificationCategory.RegistrationLifecycle) with
        {
            DeduplicationKey = null
        })).Throws<InvalidOperationException>();
    }

    private static DefaultNotificationOrchestrator CreateOrchestrator(
        CapturingNotificationIntentRepository repository,
        NotificationOwnershipDecision decision)
    {
        return new DefaultNotificationOrchestrator(new FixedNotificationOwnershipResolver(decision), repository);
    }

    private static NotificationIntentDraft CreateDraft(
        AppNotificationCategory category,
        bool isIslamuInitiated = true)
    {
        return new NotificationIntentDraft(
            category,
            TenantId: Guid.CreateVersion7(),
            RecipientKind: "User",
            TemplateKey: "registration.approved",
            SafePayloadReference: "notification-intents/test-safe-payload",
            SafePayloadHash: "sha256:test-safe-payload",
            IsUserFacing: true,
            IsIslamuInitiated: isIslamuInitiated,
            DeduplicationKey: $"{category}:test-deduplication",
            CorrelationId: Guid.NewGuid().ToString("N"),
            UserId: Guid.CreateVersion7(),
            ExternalProviderId: "external-provider-safe-id",
            ExternalCorrelationId: "external-correlation-safe-id");
    }

    private sealed class FixedNotificationOwnershipResolver(NotificationOwnershipDecision decision)
        : INotificationOwnershipResolver
    {
        public Task<NotificationOwnershipDecision> ResolveAsync(
            NotificationIntentDraft draft,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(decision);
        }
    }

    private sealed class CapturingNotificationIntentRepository : INotificationIntentRepository
    {
        public NotificationIntent? CreatedIntent { get; private set; }

        public Task<NotificationIntent?> GetById(Guid id) => Task.FromResult<NotificationIntent?>(CreatedIntent?.Id == id ? CreatedIntent : null);

        public Task<IReadOnlyList<NotificationIntent>> GetAll() => Task.FromResult<IReadOnlyList<NotificationIntent>>(Array.Empty<NotificationIntent>());

        public Task<(IReadOnlyList<NotificationIntent> Items, int TotalCount)> GetAllPaged(int pageNumber, int pageSize)
        {
            return Task.FromResult<(IReadOnlyList<NotificationIntent>, int)>((Array.Empty<NotificationIntent>(), 0));
        }

        public Task<bool> Exists(Guid id) => Task.FromResult(CreatedIntent?.Id == id);

        public Task<NotificationIntent> Create(NotificationIntent entity) => CreateIntentAsync(entity);

        public Task Update(NotificationIntent entity) => Task.CompletedTask;

        public Task Delete(NotificationIntent entity) => Task.CompletedTask;

        public Task<NotificationIntent> CreateIntentAsync(
            NotificationIntent intent,
            CancellationToken cancellationToken = default)
        {
            CreatedIntent = intent;
            return Task.FromResult(intent);
        }

        public Task<NotificationIntent?> GetByTenantAndIdAsync(
            Guid tenantId,
            Guid intentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreatedIntent?.TenantId == tenantId && CreatedIntent.Id == intentId ? CreatedIntent : null);
        }

        public Task<bool> ExistsByDeduplicationKeyAsync(
            Guid tenantId,
            string deduplicationKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreatedIntent?.TenantId == tenantId && CreatedIntent.DeduplicationKey == deduplicationKey);
        }

        public Task<NotificationDelivery> AddDeliveryAsync(
            NotificationDelivery delivery,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(delivery);
        }

        public Task<NotificationExternalDelegation> AddExternalDelegationAsync(
            NotificationExternalDelegation delegation,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(delegation);
        }
    }
}
