// ABOUTME: Tests bounded cross-tenant webhook retention orchestration and mandatory system audit.
// ABOUTME: Proves tenant context, unit-of-work boundaries, aggregate counts, and no-op audit suppression.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Models.Tenants;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

public sealed class WebhookRetentionCleanupServiceTests
{
    [Test]
    public async Task CleanupAllTenantsAsync_BoundsTenantBatchAndAuditsEachMutatingTransaction()
    {
        var tenantIds = new[] { Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7() };
        var fixture = CreateFixture(tenantIds, new WebhookRetentionCleanupResult(1, 0, 0, 0, 0, 0, 0, 0, false));
        var utcNow = new DateTime(2026, 7, 14, 14, 0, 0, DateTimeKind.Utc);

        var run = await fixture.Service.CleanupAllTenantsAsync(utcNow, CancellationToken.None);

        await Assert.That(run.TenantCount).IsEqualTo(2);
        await Assert.That(run.SucceededTenantCount).IsEqualTo(2);
        await Assert.That(run.FailedTenantCount).IsEqualTo(0);
        await Assert.That(run.Aggregate.OutboundPayloadsCleared).IsEqualTo(2);
        await Assert.That(fixture.CleanupTenantIds).Count().IsEqualTo(2);
        await Assert.That(fixture.CleanupTenantIds.Distinct().Count()).IsEqualTo(2);
        await Assert.That(fixture.UnitOfWork.TransactionCount).IsEqualTo(2);
        await Assert.That(fixture.TenantContextAccessor.IsResolved).IsFalse();
        await fixture.AuditWriter.Received(2).AppendAsync(
            Arg.Is<WebhookAuditWriteRequest>(audit =>
                audit.Action == WebhookAuditAction.RetentionCleanupCompleted &&
                audit.TargetKind == WebhookAuditTargetKind.CleanupRun &&
                audit.PrincipalKind == WebhookAuditPrincipalKind.System &&
                audit.PrincipalReference == "system:webhook-retention-cleanup" &&
                audit.SafeAfterJson != null &&
                !audit.SafeAfterJson.Contains("tenant", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupAllTenantsAsync_WhenNoRowsChange_DoesNotWriteAudit()
    {
        var fixture = CreateFixture(
            [Guid.CreateVersion7()],
            new WebhookRetentionCleanupResult(0, 0, 0, 0, 0, 0, 0, 0, false));

        var run = await fixture.Service.CleanupAllTenantsAsync(
            new DateTime(2026, 7, 14, 14, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        await Assert.That(run.Aggregate.TotalAffected).IsEqualTo(0);
        await fixture.AuditWriter.DidNotReceive().AppendAsync(
            Arg.Any<WebhookAuditWriteRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static Fixture CreateFixture(
        IReadOnlyCollection<Guid> tenantIds,
        WebhookRetentionCleanupResult repositoryResult)
    {
        var tenantLookupSource = Substitute.For<ITenantLookupSource>();
        tenantLookupSource.GetTenantLookupsAsync(Arg.Any<CancellationToken>())
            .Returns(tenantIds.Select(tenantId => new TenantLookupRecord
            {
                TenantId = tenantId,
                Slug = $"tenant-{tenantId:N}"
            }).ToArray());
        var tenantContextAccessor = new TrackingTenantContextAccessor();
        var cleanupTenantIds = new List<Guid>();
        var repository = Substitute.For<IWebhookRetentionCleanupRepository>();
        repository.CleanupTenantAsync(
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var tenantId = call.ArgAt<Guid>(0);
                if (tenantContextAccessor.TenantId != tenantId)
                {
                    throw new InvalidOperationException("Cleanup tenant context does not match repository scope.");
                }

                cleanupTenantIds.Add(tenantId);
                return repositoryResult;
            });
        var auditWriter = Substitute.For<IWebhookAuditEventWriter>();
        auditWriter.AppendAsync(Arg.Any<WebhookAuditWriteRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => CreateAudit(call.ArgAt<WebhookAuditWriteRequest>(0)));
        var unitOfWork = new TrackingUnitOfWork();
        var services = new ServiceCollection();
        services.AddSingleton(tenantLookupSource);
        services.AddSingleton<ITenantContextAccessor>(tenantContextAccessor);
        services.AddSingleton(repository);
        services.AddSingleton(auditWriter);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        var serviceProvider = services.BuildServiceProvider();
        var settings = Options.Create(new WebhookRetentionSettings
        {
            MaxTenantsPerPass = 2,
            BatchSize = 17
        });
        var retentionPolicyResolver = new WebhookRetentionPolicyResolver(
            new StaticOptionsMonitor<WebhookRetentionSettings>(settings.Value));
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        var service = new WebhookRetentionCleanupService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            settings,
            retentionPolicyResolver,
            new BusinessMetrics(meterFactory),
            NullLogger<WebhookRetentionCleanupService>.Instance);
        return new Fixture(
            serviceProvider,
            service,
            repository,
            auditWriter,
            tenantContextAccessor,
            unitOfWork,
            cleanupTenantIds);
    }

    private static WebhookAuditEvent CreateAudit(WebhookAuditWriteRequest request) =>
        WebhookAuditEvent.Create(
            request.TenantId,
            request.PrincipalKind!.Value,
            request.PrincipalReference!,
            request.EffectiveScopeKind,
            request.EffectiveScopeId ?? request.TenantId,
            request.Action,
            request.TargetKind,
            request.TargetId,
            request.SafeBeforeJson,
            request.SafeAfterJson,
            request.ConfigurationVersion,
            request.CorrelationId,
            request.ReasonCode,
            request.Outcome,
            "webhook-retention-test-v1",
            DateTime.UtcNow.AddDays(365));

    private sealed record Fixture(
        ServiceProvider ServiceProvider,
        WebhookRetentionCleanupService Service,
        IWebhookRetentionCleanupRepository Repository,
        IWebhookAuditEventWriter AuditWriter,
        TrackingTenantContextAccessor TenantContextAccessor,
        TrackingUnitOfWork UnitOfWork,
        List<Guid> CleanupTenantIds) : IDisposable
    {
        public void Dispose() => ServiceProvider.Dispose();
    }

    private sealed class TrackingTenantContextAccessor : ITenantContextAccessor
    {
        public Guid? TenantId { get; private set; }
        public bool IsResolved => TenantId.HasValue;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
        public void Clear() => TenantId = null;
    }

    private sealed class TrackingUnitOfWork : IUnitOfWork
    {
        public int TransactionCount { get; private set; }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            TransactionCount++;
            await operation(ct);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            TransactionCount++;
            return await operation(ct);
        }

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => ExecuteInTransactionAsync(operation, ct);
    }

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
