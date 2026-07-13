// ABOUTME: PostgreSQL integration tests for competing webhook delivery workers and fenced settlement.
// ABOUTME: Proves one database claim owner wins and stale lease or fence completions cannot mutate the attempt.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Explore.Infrastructure.Tests.Infrastructure.Webhooks;

[NotInParallel("WebhookDeliveryClaimPostgreSql")]
public sealed class WebhookDeliveryClaimIntegrationTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("webhook_claim_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        await LookupTableSeeder.SeedAsync(context);
    }

    [Test]
    public async Task CompetingWorkers_ProduceOneOwnerAndRejectStaleSettlement()
    {
        var now = DateTime.UtcNow;
        var attempt = await SeedAttemptAsync(now);
        var request = new WebhookDeliveryClaimRequest(
            BatchSize: 1,
            CandidateBatchSize: 10,
            GlobalInFlightLimit: 10,
            TenantOrder: [attempt.TenantId],
            ClaimedAt: now,
            LeaseDuration: TimeSpan.FromMinutes(2));
        var limits = new Dictionary<Guid, WebhookDeliveryClaimLimits>
        {
            [attempt.TenantId] = new(10, 1, 1)
        };

        var firstWorker = ClaimAsync(request, limits);
        var secondWorker = ClaimAsync(request, limits);
        var workerClaims = await Task.WhenAll(firstWorker, secondWorker);
        var winningClaim = workerClaims.SelectMany(claims => claims).Single();

        await Assert.That(workerClaims.Count(claims => claims.Count == 1)).IsEqualTo(1);
        await Assert.That(workerClaims.Count(claims => claims.Count == 0)).IsEqualTo(1);
        await Assert.That(winningClaim.ProcessingFence).IsEqualTo(1);

        await using var settlementContext = CreateDbContext();
        var repository = new WebhookDeliveryAttemptRepository(settlementContext);
        var staleLeaseResult = await repository.MarkSucceededAsync(
            attempt.TenantId,
            attempt.Id,
            Guid.CreateVersion7(),
            winningClaim.ProcessingFence,
            now,
            now.AddSeconds(1),
            204,
            10,
            CancellationToken.None);
        var staleFenceResult = await repository.MarkSucceededAsync(
            attempt.TenantId,
            attempt.Id,
            winningClaim.LeaseToken,
            winningClaim.ProcessingFence + 1,
            now,
            now.AddSeconds(1),
            204,
            10,
            CancellationToken.None);
        var ownerResult = await repository.MarkSucceededAsync(
            attempt.TenantId,
            attempt.Id,
            winningClaim.LeaseToken,
            winningClaim.ProcessingFence,
            now,
            now.AddSeconds(1),
            204,
            10,
            CancellationToken.None);

        await Assert.That(staleLeaseResult).IsFalse();
        await Assert.That(staleFenceResult).IsFalse();
        await Assert.That(ownerResult).IsTrue();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task<IReadOnlyList<WebhookDeliveryClaim>> ClaimAsync(
        WebhookDeliveryClaimRequest request,
        IReadOnlyDictionary<Guid, WebhookDeliveryClaimLimits> limits)
    {
        await using var context = CreateDbContext();
        return await new WebhookDeliveryAttemptRepository(context)
            .ClaimDueAsync(request, limits, CancellationToken.None);
    }

    private async Task<WebhookDeliveryAttempt> SeedAttemptAsync(DateTime now)
    {
        await using var context = CreateDbContext();
        var tenantId = Guid.CreateVersion7();
        var consumerId = Guid.CreateVersion7();
        var endpointId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        var tenant = new Tenant
        {
            Id = tenantId,
            FullName = "Webhook Claim Test Tenant",
            Slug = $"webhook-claim-{tenantId:N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
            CreatedAt = now
        };
        var consumer = new WebhookConsumer
        {
            Id = consumerId,
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.Tenant,
            Name = "Claim integration consumer",
            Status = WebhookConsumerStatus.Active,
            ProviderMode = WebhookProviderMode.Local,
            CreatedAt = now
        };
        var endpoint = new WebhookEndpoint
        {
            Id = endpointId,
            TenantId = tenantId,
            ConsumerId = consumerId,
            Url = "https://hooks.example.test/webhook",
            Status = WebhookEndpointStatus.Active,
            SecretRef = "claim-integration-secret",
            SecretVersion = 1,
            MaxAttempts = 8,
            TimeoutSeconds = 15,
            CreatedAt = now
        };
        var message = WebhookMessage.Create(
            messageId,
            tenantId,
            "event.published",
            "claim-integration-event",
            "Event",
            Guid.CreateVersion7(),
            consumerId,
            "{\"type\":\"event.published\"}"u8,
            "application/json",
            "utf-8",
            now,
            now.AddDays(14),
            now);
        var attempt = new WebhookDeliveryAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            MessageId = messageId,
            EndpointId = endpointId,
            AttemptNumber = 1,
            Outcome = WebhookDeliveryAttemptOutcome.Scheduled,
            ScheduledAt = now.AddSeconds(-1),
            CreatedAt = now
        };

        context.AddRange(tenant, consumer, endpoint, message, attempt);
        await context.SaveChangesAsync();
        return attempt;
    }

    private ExploreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Webhook delivery claim integration test.");
        return context;
    }
}
