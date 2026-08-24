// ABOUTME: Verifies registration-provider subscription lifecycle sweep scheduling without live provider I/O.
// ABOUTME: Covers missed notification recovery and next periodic sweep scheduling after checkpoint settlement.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Services.Registration;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using System.Text;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationProviderSubscriptionLifecycleServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task DrainOnceAsync_BindsClaimTenantAndRejectsInactiveRenewal()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid previousTenantId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            Guid.CreateVersion7(), tenantId, "Formbricks", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.SelfHosted, "FORMBRICKS", "SELF_HOSTED", "v1",
            "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-11", "https://forms.example.test",
            "https://forms.example.test", "tenant", Guid.CreateVersion7(), null, Now.AddDays(-1));
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            RegistrationProviderPresentationModeEnum.Embed,
            RegistrationProviderCollectionModeEnum.ProviderHosted,
            RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.CompletionOnly,
            null,
            Now.AddDays(-1));
        typeof(RegistrationProviderBinding).GetProperty(nameof(RegistrationProviderBinding.Connection))!.SetValue(binding, connection);
        RegistrationProviderSubscriptionState state = RegistrationProviderSubscriptionState.Create(
            tenantId, binding.Id, "RESPONSES", "watch-1", Now.AddHours(1), null, Now.AddDays(-1));
        state.Claim(Guid.CreateVersion7(), Now.AddMinutes(2), Now);

        IRegistrationProviderSubscriptionStateRepository states = Substitute.For<IRegistrationProviderSubscriptionStateRepository>();
        states.GetExpiringAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        states.ClaimDueRenewalsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([state]);
        states.ClaimDueSweepsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([]);
        IRegistrationProviderRepository providers = Substitute.For<IRegistrationProviderRepository>();
        providers.GetBindingAsync(tenantId, binding.Id, Arg.Any<CancellationToken>()).Returns(binding);
        var tenantAccessor = new RecordingTenantContextAccessor(previousTenantId);
        var descriptor = new RenewalDescriptor(
            connection,
            tenantAccessor,
            new(false, "watch-1", ExpiresAtUtc: Now.AddDays(5)));
        IRegistrationProviderRegistry registry = Substitute.For<IRegistrationProviderRegistry>();
        registry.TryResolve(Arg.Any<RegistrationProviderTuple>()).Returns(descriptor);

        await new RegistrationProviderSubscriptionLifecycleService(
            states,
            providers,
            registry,
            tenantAccessor,
            Substitute.For<IRegistrationProviderCallbackUriBuilder>(),
            Substitute.For<IIncomingWebhookMessageRepository>(),
            Substitute.For<IIncomingWebhookEffectOutboxRepository>(),
            Substitute.For<IRegistrationProviderCallbackReceiptProtector>(),
            new ImmediateUnitOfWork(),
            CreateMetrics(),
            new FixedTimeProvider(Now)).DrainOnceAsync(CancellationToken.None);

        await Assert.That(descriptor.ObservedTenantId).IsEqualTo(tenantId);
        await Assert.That(tenantAccessor.TenantId).IsEqualTo(previousTenantId);
        await Assert.That(state.FailureCategory).IsEqualTo("renewal_rejected");
        await Assert.That(state.LastRenewalSuccessAt).IsNull();
    }

    [Test]
    public async Task DrainOnceAsync_WhenRenewalOutcomeIsUncertainParksWithoutAutomaticRetry()
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            Guid.CreateVersion7(), tenantId, "Formbricks", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.SelfHosted, "FORMBRICKS", "SELF_HOSTED", "v1",
            "ISLAMU_EVENT_FORMBRICKS_V1", "2026-08-11", "https://forms.example.test",
            "https://forms.example.test", "tenant", Guid.CreateVersion7(), null, Now.AddDays(-1));
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            RegistrationProviderPresentationModeEnum.Embed,
            RegistrationProviderCollectionModeEnum.ProviderHosted,
            RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.CompletionOnly,
            null,
            Now.AddDays(-1));
        typeof(RegistrationProviderBinding).GetProperty(nameof(RegistrationProviderBinding.Connection))!.SetValue(binding, connection);
        RegistrationProviderSubscriptionState state = RegistrationProviderSubscriptionState.Create(
            tenantId, binding.Id, "RESPONSES", "watch-1", Now.AddHours(1), null, Now.AddDays(-1));
        state.Claim(Guid.CreateVersion7(), Now.AddMinutes(2), Now);
        IRegistrationProviderSubscriptionStateRepository states = Substitute.For<IRegistrationProviderSubscriptionStateRepository>();
        states.GetExpiringAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        states.ClaimDueRenewalsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([state]);
        states.ClaimDueSweepsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([]);
        IRegistrationProviderRepository providers = Substitute.For<IRegistrationProviderRepository>();
        providers.GetBindingAsync(tenantId, binding.Id, Arg.Any<CancellationToken>()).Returns(binding);
        IRegistrationProviderRegistry registry = Substitute.For<IRegistrationProviderRegistry>();
        registry.TryResolve(Arg.Any<RegistrationProviderTuple>()).Returns(new ThrowingRenewalDescriptor(connection));

        await new RegistrationProviderSubscriptionLifecycleService(
            states,
            providers,
            registry,
            new RecordingTenantContextAccessor(null),
            Substitute.For<IRegistrationProviderCallbackUriBuilder>(),
            Substitute.For<IIncomingWebhookMessageRepository>(),
            Substitute.For<IIncomingWebhookEffectOutboxRepository>(),
            Substitute.For<IRegistrationProviderCallbackReceiptProtector>(),
            new ImmediateUnitOfWork(),
            CreateMetrics(),
            new FixedTimeProvider(Now)).DrainOnceAsync(CancellationToken.None);

        await Assert.That(state.FailureCategory).IsEqualTo("renewal_in_doubt");
        await Assert.That(state.NextRenewalAttemptAt).IsNull();
        await Assert.That(state.LastRenewalSuccessAt).IsNull();
    }

    [Test]
    public async Task DrainOnceAsync_ProcessesPeriodicMissedNotificationSweepAndSchedulesNextSweep()
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            Guid.CreateVersion7(), tenantId, "Google Forms", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas, "GOOGLE_FORMS", "GOOGLE_WORKSPACE", "v1",
            "ISLAMU_EVENT_GOOGLE_FORMS_PUBSUB_V1", "2026-08-11", "https://forms.googleapis.com/v1",
            "https://docs.google.com", "workspace", Guid.CreateVersion7(), null, Now.AddDays(-1));
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), RegistrationProviderPresentationModeEnum.Embed,
            RegistrationProviderCollectionModeEnum.ProviderHosted, RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.CompletionOnly, null, Now.AddDays(-1));
        typeof(RegistrationProviderBinding).GetProperty(nameof(RegistrationProviderBinding.Connection))!.SetValue(binding, connection);
        RegistrationProviderSubscriptionState state = RegistrationProviderSubscriptionState.Create(
            tenantId, binding.Id, "RESPONSES", "watch-1", Now.AddDays(1), "2026-08-11T05:00:00.0000000Z", Now.AddDays(-1));
        state.Claim(Guid.CreateVersion7(), Now.AddMinutes(2), Now);
        IRegistrationProviderSubscriptionStateRepository states = Substitute.For<IRegistrationProviderSubscriptionStateRepository>();
        states.GetExpiringAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        states.ClaimDueRenewalsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([]);
        states.ClaimDueSweepsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([state]);
        IRegistrationProviderRepository providers = Substitute.For<IRegistrationProviderRepository>();
        providers.GetBindingAsync(tenantId, binding.Id, Arg.Any<CancellationToken>()).Returns(binding);
        var descriptor = new ReconciliationDescriptor(connection);
        IRegistrationProviderRegistry registry = Substitute.For<IRegistrationProviderRegistry>();
        registry.TryResolve(Arg.Any<RegistrationProviderTuple>()).Returns(descriptor);

        int processed = await new RegistrationProviderSubscriptionLifecycleService(
            states,
            providers,
            registry,
            Substitute.For<ITenantContextAccessor>(),
            Substitute.For<IRegistrationProviderCallbackUriBuilder>(),
            Substitute.For<IIncomingWebhookMessageRepository>(),
            Substitute.For<IIncomingWebhookEffectOutboxRepository>(),
            Substitute.For<IRegistrationProviderCallbackReceiptProtector>(),
            new ImmediateUnitOfWork(),
            CreateMetrics(),
            new FixedTimeProvider(Now)).DrainOnceAsync(CancellationToken.None);

        await Assert.That(processed).IsEqualTo(1);
        await Assert.That(descriptor.ReconcileCalls).IsEqualTo(1);
        await Assert.That(state.PendingNotificationAt).IsNull();
        await Assert.That(state.LastSweepSuccessAt).IsEqualTo(Now);
        await Assert.That(state.NextSweepAttemptAt).IsEqualTo(Now.AddHours(6));
        await Assert.That(state.ResponseCheckpoint).IsEqualTo("2026-08-11T12:01:00.0000000Z");
    }

    [Test]
    public async Task DrainOnceAsync_SavesDurableEffectPointerBeforeAdvancingCheckpoint()
    {
        Setup setup = CreateSetup(new RegistrationProviderReconciliationResult(
            1,
            false,
            [new("response-1", "revision-1", Now.AddMinutes(1))],
            "2026-08-11T12:01:00.0000000Z"));
        IIncomingWebhookEffectOutboxRepository pointers = Substitute.For<IIncomingWebhookEffectOutboxRepository>();

        await CreateService(setup, pointers: pointers).DrainOnceAsync(CancellationToken.None);

        await pointers.Received(1).AddAsync(Arg.Any<IncomingWebhookEffectOutbox>(), Arg.Any<CancellationToken>());
        await pointers.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await setup.States.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await Assert.That(setup.State.ResponseCheckpoint).IsEqualTo("2026-08-11T12:01:00.0000000Z");
    }

    [Test]
    public async Task DrainOnceAsync_QueuesIdentifiersOnlyWithoutProviderAnswerPayloadOrHeaders()
    {
        const string token = "attempt-secret-token";
        const string fileId = "drive-file-123";
        const string fileName = "private-passport.pdf";
        const string mimeType = "application/pdf";
        Setup setup = CreateSetup(new RegistrationProviderReconciliationResult(
            1,
            false,
            [new("response-1", "revision-1", Now.AddMinutes(1))],
            "2026-08-11T12:01:00.0000000Z"));
        IIncomingWebhookMessageRepository messages = Substitute.For<IIncomingWebhookMessageRepository>();
        IncomingWebhookMessage? persisted = null;
        messages.TryCreateAsync(Arg.Do<IncomingWebhookMessage>(message => persisted = message), Arg.Any<CancellationToken>()).Returns(true);

        await CreateService(setup, messages: messages).DrainOnceAsync(CancellationToken.None);

        await Assert.That(persisted).IsNotNull();
        string payload = Encoding.UTF8.GetString(persisted!.PayloadBytes.Span);
        string headers = persisted.HeadersJson ?? string.Empty;
        string combined = payload + headers;
        await Assert.That(payload).IsEqualTo("{}");
        await Assert.That(combined).DoesNotContain(token);
        await Assert.That(combined).DoesNotContain(fileId);
        await Assert.That(combined).DoesNotContain(fileName);
        await Assert.That(combined).DoesNotContain(mimeType);
    }

    [Test]
    public async Task DrainOnceAsync_WhenEffectPointerSaveFailsLeavesCheckpointUnchanged()
    {
        Setup setup = CreateSetup(new RegistrationProviderReconciliationResult(
            1,
            false,
            [new("response-1", "revision-1", Now.AddMinutes(1))],
            "2026-08-11T12:01:00.0000000Z"));
        IIncomingWebhookEffectOutboxRepository pointers = Substitute.For<IIncomingWebhookEffectOutboxRepository>();
        pointers.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("db unavailable"));

        await CreateService(setup, pointers: pointers).DrainOnceAsync(CancellationToken.None);

        await Assert.That(setup.State.ResponseCheckpoint).IsEqualTo("2026-08-11T05:00:00.0000000Z");
        await Assert.That(setup.State.LastSweepSuccessAt).IsNull();
        await Assert.That(setup.State.FailureCategory).IsEqualTo("sweep_failed");
    }

    [Test]
    public async Task DrainOnceAsync_WhenProviderCannotReturnFullPageIdentitiesLeavesCheckpointUnchanged()
    {
        Setup setup = CreateSetup(new RegistrationProviderReconciliationResult(
            2,
            false,
            [new("response-1", "revision-1", Now.AddMinutes(1))],
            "2026-08-11T12:01:00.0000000Z"));

        await CreateService(setup).DrainOnceAsync(CancellationToken.None);

        await Assert.That(setup.State.ResponseCheckpoint).IsEqualTo("2026-08-11T05:00:00.0000000Z");
        await Assert.That(setup.State.LastSweepSuccessAt).IsNull();
        await Assert.That(setup.State.FailureCategory).IsEqualTo("sweep_failed");
    }

    [Test]
    public async Task DrainOnceAsync_WhenProviderHasMorePagesStoresContinuationCursorAndRetriesImmediately()
    {
        Setup setup = CreateSetup(new RegistrationProviderReconciliationResult(
            1,
            true,
            [new("response-1", "revision-1", Now.AddMinutes(1))],
            null,
            "registration-provider-cursor:test"));

        await CreateService(setup).DrainOnceAsync(CancellationToken.None);

        await Assert.That(setup.State.ResponseCheckpoint).IsEqualTo("registration-provider-cursor:test");
        await Assert.That(setup.State.LastSweepSuccessAt).IsEqualTo(Now);
        await Assert.That(setup.State.NextSweepAttemptAt).IsEqualTo(Now);
    }

    [Test]
    public async Task DrainOnceAsync_SweepFailuresUseGrowingBackoffAndSuccessResetsOnlySweepLane()
    {
        Setup setup = CreateSetup(new RegistrationProviderReconciliationResult(
            1,
            false,
            [new("response-1", "revision-1", Now.AddMinutes(1))],
            "2026-08-11T12:01:00.0000000Z"));
        IIncomingWebhookEffectOutboxRepository pointers = Substitute.For<IIncomingWebhookEffectOutboxRepository>();
        pointers.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("db unavailable"));

        await CreateService(setup, pointers: pointers).DrainOnceAsync(CancellationToken.None);
        DateTime first = setup.State.NextSweepAttemptAt!.Value;
        setup.State.Claim(Guid.CreateVersion7(), Now.AddMinutes(2), Now);
        await CreateService(setup, pointers: pointers).DrainOnceAsync(CancellationToken.None);
        DateTime second = setup.State.NextSweepAttemptAt!.Value;
        setup.State.Claim(Guid.CreateVersion7(), Now.AddMinutes(2), Now);
        await CreateService(setup, pointers: pointers).DrainOnceAsync(CancellationToken.None);
        DateTime third = setup.State.NextSweepAttemptAt!.Value;

        await Assert.That(first).IsEqualTo(Now.AddMinutes(1));
        await Assert.That(second).IsEqualTo(Now.AddMinutes(2));
        await Assert.That(third).IsEqualTo(Now.AddMinutes(4));
        await Assert.That(setup.State.RenewalFailureCount).IsEqualTo(0);

        setup.State.Claim(Guid.CreateVersion7(), Now.AddMinutes(2), Now);
        await CreateService(setup).DrainOnceAsync(CancellationToken.None);

        await Assert.That(setup.State.SweepFailureCount).IsEqualTo(0);
        await Assert.That(setup.State.RenewalFailureCount).IsEqualTo(0);
    }

    private static Setup CreateSetup(RegistrationProviderReconciliationResult result)
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(
            Guid.CreateVersion7(), tenantId, "Google Forms", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas, "GOOGLE_FORMS", "GOOGLE_WORKSPACE", "v1",
            "ISLAMU_EVENT_GOOGLE_FORMS_PUBSUB_V1", "2026-08-11", "https://forms.googleapis.com/v1",
            "https://docs.google.com", "workspace", Guid.CreateVersion7(), null, Now.AddDays(-1));
        RegistrationProviderBinding binding = RegistrationProviderBinding.Create(
            tenantId, connection.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), RegistrationProviderPresentationModeEnum.Embed,
            RegistrationProviderCollectionModeEnum.ProviderHosted, RegistrationProviderCompletionModeEnum.Callback,
            RegistrationProviderTrustLevelEnum.CompletionOnly, null, Now.AddDays(-1));
        typeof(RegistrationProviderBinding).GetProperty(nameof(RegistrationProviderBinding.Connection))!.SetValue(binding, connection);
        RegistrationProviderSubscriptionState state = RegistrationProviderSubscriptionState.Create(
            tenantId, binding.Id, "RESPONSES", "watch-1", Now.AddDays(1), "2026-08-11T05:00:00.0000000Z", Now.AddDays(-1));
        state.Claim(Guid.CreateVersion7(), Now.AddMinutes(2), Now);
        IRegistrationProviderSubscriptionStateRepository states = Substitute.For<IRegistrationProviderSubscriptionStateRepository>();
        states.GetExpiringAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        states.ClaimDueRenewalsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([]);
        states.ClaimDueSweepsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns([state]);
        IRegistrationProviderRepository providers = Substitute.For<IRegistrationProviderRepository>();
        providers.GetBindingAsync(tenantId, binding.Id, Arg.Any<CancellationToken>()).Returns(binding);
        return new(states, providers, connection, binding, state, result);
    }

    private static RegistrationProviderSubscriptionLifecycleService CreateService(
        Setup setup,
        IIncomingWebhookEffectOutboxRepository? pointers = null,
        IIncomingWebhookMessageRepository? messages = null)
    {
        var descriptor = new ReconciliationDescriptor(setup.Connection, setup.ReconciliationResult);
        IRegistrationProviderRegistry registry = Substitute.For<IRegistrationProviderRegistry>();
        registry.TryResolve(Arg.Any<RegistrationProviderTuple>()).Returns(descriptor);
        messages ??= Substitute.For<IIncomingWebhookMessageRepository>();
        messages.TryCreateAsync(Arg.Any<IncomingWebhookMessage>(), Arg.Any<CancellationToken>()).Returns(true);
        IRegistrationProviderCallbackReceiptProtector receipts = Substitute.For<IRegistrationProviderCallbackReceiptProtector>();
        receipts.Protect(Arg.Any<RegistrationProviderCallbackReceipt>()).Returns("receipt:v1:test");
        return new(
            setup.States,
            setup.Providers,
            registry,
            Substitute.For<ITenantContextAccessor>(),
            Substitute.For<IRegistrationProviderCallbackUriBuilder>(),
            messages,
            pointers ?? Substitute.For<IIncomingWebhookEffectOutboxRepository>(),
            receipts,
            new ImmediateUnitOfWork(),
            CreateMetrics(),
            new FixedTimeProvider(Now));
    }

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }

    private sealed class ReconciliationDescriptor(RegistrationProviderConnection connection, RegistrationProviderReconciliationResult? result = null) : IRegistrationProviderDescriptor, IRegistrationProviderReconciliationProvider
    {
        public int ReconcileCalls { get; private set; }
        public RegistrationProviderTuple Tuple { get; } = new(connection.ProviderCode, connection.ProviderDeploymentCode, connection.ApiVersion, connection.AdapterPolicyVersion, connection.ConformanceEvidenceRevision);
        public RegistrationProviderCapabilitySet ProvenCapabilities => RegistrationProviderCapabilitySet.None;

        public Task<RegistrationProviderReconciliationResult> ReconcileAsync(RegistrationProviderReconciliationRequest request, CancellationToken cancellationToken)
        {
            ReconcileCalls++;
            return Task.FromResult(result ?? new RegistrationProviderReconciliationResult(0, false, [], "2026-08-11T12:01:00.0000000Z"));
        }
    }

    private sealed class RenewalDescriptor(
        RegistrationProviderConnection connection,
        ITenantContextAccessor tenantAccessor,
        RegistrationProviderSubscriptionResult result)
        : IRegistrationProviderDescriptor, IRegistrationProviderSubscriptionManager
    {
        public Guid? ObservedTenantId { get; private set; }
        public RegistrationProviderTuple Tuple { get; } = new(
            connection.ProviderCode,
            connection.ProviderDeploymentCode,
            connection.ApiVersion,
            connection.AdapterPolicyVersion,
            connection.ConformanceEvidenceRevision);
        public RegistrationProviderCapabilitySet ProvenCapabilities => RegistrationProviderCapabilitySet.None;

        public Task<RegistrationProviderSubscriptionResult> EnsureSubscriptionAsync(
            RegistrationProviderSubscriptionRequest request,
            CancellationToken cancellationToken)
        {
            ObservedTenantId = tenantAccessor.TenantId;
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingRenewalDescriptor(RegistrationProviderConnection connection)
        : IRegistrationProviderDescriptor, IRegistrationProviderSubscriptionManager
    {
        public RegistrationProviderTuple Tuple { get; } = new(
            connection.ProviderCode,
            connection.ProviderDeploymentCode,
            connection.ApiVersion,
            connection.AdapterPolicyVersion,
            connection.ConformanceEvidenceRevision);
        public RegistrationProviderCapabilitySet ProvenCapabilities => RegistrationProviderCapabilitySet.None;

        public Task<RegistrationProviderSubscriptionResult> EnsureSubscriptionAsync(
            RegistrationProviderSubscriptionRequest request,
            CancellationToken cancellationToken) => throw new HttpRequestException("response lost");
    }

    private sealed class RecordingTenantContextAccessor(Guid? tenantId) : ITenantContextAccessor
    {
        public Guid? TenantId { get; private set; } = tenantId;
        public bool IsResolved => TenantId.HasValue;
        public void SetTenant(Guid value) => TenantId = value;
        public void Clear() => TenantId = null;
    }

    private sealed record Setup(
        IRegistrationProviderSubscriptionStateRepository States,
        IRegistrationProviderRepository Providers,
        RegistrationProviderConnection Connection,
        RegistrationProviderBinding Binding,
        RegistrationProviderSubscriptionState State,
        RegistrationProviderReconciliationResult ReconciliationResult);

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
