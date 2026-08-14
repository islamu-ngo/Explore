// ABOUTME: API-hosted organizer payment readiness reconciliation worker composition tests.
// ABOUTME: Proves Testing registration stays disabled and RunOnce delegates through a fresh scoped service graph.

using Event.Api.IntegrationTests.Fixtures;
using Explore.API.BackgroundServices;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.OrganizerPaymentConnections;
using Explore.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Event.Api.IntegrationTests.Features;

public sealed class OrganizerPaymentReadinessReconciliationWorkerTests
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid ActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000020");
    private static readonly DateTime Now = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ApiHost_DoesNotRegisterOrganizerPaymentReadinessWorkerInTesting()
    {
        await using var factory = new CustomWebApplicationFactory();

        Type[] hostedServiceTypes = factory.Services.GetServices<IHostedService>()
            .Select(service => service.GetType())
            .ToArray();

        await Assert.That(hostedServiceTypes).DoesNotContain(typeof(OrganizerPaymentReadinessReconciliationWorker));
    }

    [Test]
    public async Task WorkerRunOnce_DelegatesThroughScopedReconciliationService()
    {
        var repository = new FakeOrganizerPaymentConnectionRepository();
        OrganizerPaymentProviderConnection connection = OrganizerPaymentProviderConnection.Create(
            Guid.CreateVersion7(),
            TenantId,
            ActorId,
            "stripe",
            "platform-main",
            "acct_worker",
            Now.AddMinutes(-30));
        repository.Connections.Add(connection);
        var provider = new FakeOrganizerPaymentOnboardingProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IOrganizerPaymentProviderConnectionRepository>(repository);
        services.AddSingleton<IOrganizerPaymentOnboardingProvider>(provider);
        services.AddSingleton<IUnitOfWork, InlineUnitOfWork>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Options.Create(new OrganizerPaymentReadinessReconciliationOptions
        {
            BatchSize = 25,
            StaleIntervalMinutes = 5,
            InitialDelaySeconds = 0,
            PollingIntervalSeconds = 60
        }));
        services.AddScoped<OrganizerPaymentReadinessReconciliationService>();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var worker = new OrganizerPaymentReadinessReconciliationWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OrganizerPaymentReadinessReconciliationOptions { InitialDelaySeconds = 0 }),
            NullLogger<OrganizerPaymentReadinessReconciliationWorker>.Instance);

        OrganizerPaymentReadinessReconciliationResult result = await worker.RunOnceAsync(CancellationToken.None);

        await Assert.That(result.UpdatedCount).IsEqualTo(1);
        await Assert.That(provider.ReadinessCalls).IsEqualTo(1);
        await Assert.That(connection.StatusId).IsEqualTo((int)Explore.Domain.Enums.OrganizerPaymentProviderConnectionStatusEnum.Ready);
    }

    private sealed class FakeOrganizerPaymentConnectionRepository : IOrganizerPaymentProviderConnectionRepository
    {
        public List<OrganizerPaymentProviderConnection> Connections { get; } = [];

        public Task<OrganizerPaymentProviderConnection?> GetActiveByScopeAsync(Guid tenantId, Guid organizerActorId, string providerCode, string connectPlatformId, CancellationToken cancellationToken) =>
            Task.FromResult<OrganizerPaymentProviderConnection?>(null);

        public Task<OrganizerPaymentProviderConnection?> GetHistoricalByExternalAccountAsync(string providerCode, string connectPlatformId, string externalAccountId, CancellationToken cancellationToken) =>
            Task.FromResult<OrganizerPaymentProviderConnection?>(null);

        public Task<IReadOnlyList<OrganizerPaymentProviderConnection>> ListHistoricalByExternalAccountAsync(string providerCode, string externalAccountId, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrganizerPaymentProviderConnection>>([]);

        public Task<IReadOnlyList<OrganizerPaymentProviderConnection>> ListDueReadinessChecksAsync(DateTime observedBefore, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrganizerPaymentProviderConnection>>(Connections.Take(limit).ToArray());

        public Task<OrganizerPaymentProviderConnection?> GetByTenantProviderAndExternalAccountForUpdateAsync(Guid tenantId, string providerCode, string externalAccountId, CancellationToken cancellationToken) =>
            Task.FromResult(Connections.SingleOrDefault(connection => connection.TenantId == tenantId && connection.ProviderCode == providerCode && connection.ExternalAccountId == externalAccountId));

        public Task<OrganizerPaymentProviderConnection?> GetByTenantAndIdForUpdateAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken) =>
            Task.FromResult(Connections.SingleOrDefault(connection => connection.TenantId == tenantId && connection.Id == connectionId));

        public Task<IReadOnlyList<OrganizerPaymentProviderConnection>> ListByTenantAndActorAsync(Guid tenantId, Guid organizerActorId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrganizerPaymentProviderConnection>>([]);

        public Task CreateAsync(OrganizerPaymentProviderConnection connection, CancellationToken cancellationToken)
        {
            Connections.Add(connection);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeOrganizerPaymentOnboardingProvider : IOrganizerPaymentOnboardingProvider
    {
        public int ReadinessCalls { get; private set; }

        public Task<OrganizerPaymentProviderAccountCreationResult> CreateAccountAsync(OrganizerPaymentProviderAccountCreationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(OrganizerPaymentProviderAccountCreationResult.Failed("not_used"));

        public Task<OrganizerPaymentOnboardingLinkCreationResult> CreateOnboardingLinkAsync(OrganizerPaymentOnboardingLinkRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(OrganizerPaymentOnboardingLinkCreationResult.Failed("not_used"));

        public Task<OrganizerPaymentProviderReadinessResult> GetReadinessAsync(OrganizerPaymentProviderReadinessRequest request, CancellationToken cancellationToken)
        {
            ReadinessCalls++;
            return Task.FromResult(OrganizerPaymentProviderReadinessResult.Retrieved(new OrganizerPaymentProviderReadiness(
                true,
                OrganizerPaymentProviderCapabilityState.Active,
                OrganizerPaymentProviderCapabilityState.Active,
                OrganizerPaymentProviderRequirementsState.Satisfied,
                [],
                [],
                [],
                null,
                "BE",
                ["EUR"],
                Now,
                "worker-readiness")));
        }
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }
}
