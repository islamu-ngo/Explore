// ABOUTME: Proves two PostgreSQL refund reservations cannot exceed one payment's captured capacity.
// ABOUTME: Uses independent DbContexts and the production repository transaction and row-lock path.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Npgsql;
using TUnit.Core.Interfaces;
using Testcontainers.PostgreSql;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests;

[NotInParallel("PersistenceDb")]
[ClassDataSource<RefundPostgreSqlContainerFixture>(Shared = SharedType.PerClass)]
public sealed class RefundReservationPostgreSqlConcurrencyTests(RefundPostgreSqlContainerFixture fixture)
{
    private static readonly DateTime UtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task TwoRacingReservationsCannotExceedCapturedCapacity()
    {
        string connectionString = fixture.ConnectionString;
        Guid tenantId = Guid.CreateVersion7();
        Guid paymentId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        await using (ExploreDbContext seed = await CreateContextAsync(connectionString))
        {
            await SeedCapturedPaymentAsync(seed, tenantId, paymentId, orderId);
        }

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        RefundReservationResult[] results = await Task.WhenAll(
            ReserveAsync(connectionString, Refund(tenantId, paymentId, orderId, 750, "refund:race:a"), timeout.Token),
            ReserveAsync(connectionString, Refund(tenantId, paymentId, orderId, 750, "refund:race:b"), timeout.Token));

        await Assert.That(results.Count(result => result.Disposition == RefundReservationDisposition.Reserved)).IsEqualTo(1);
        await Assert.That(results.Count(result => result.Disposition == RefundReservationDisposition.CapacityExceeded)).IsEqualTo(1);
        await using ExploreDbContext verification = await CreateContextAsync(connectionString, ensureCreated: false);
        long reserved = await verification.RefundAttempts
            .Where(attempt => attempt.TenantId == tenantId && attempt.PaymentAttemptId == paymentId)
            .SumAsync(attempt => attempt.Allocation.TotalMinor, timeout.Token);
        await Assert.That(reserved).IsEqualTo(750L);
    }

    [Test]
    public async Task SequentialRoundedFeeReservationsRespectEveryPersistedLineConstraint()
    {
        string connectionString = fixture.ConnectionString;
        Guid tenantId = Guid.CreateVersion7();
        Guid paymentId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        long[] lineTotals = [1, 1, 1];
        await using (ExploreDbContext seed = await CreateContextAsync(connectionString))
        {
            await SeedCapturedPaymentAsync(seed, tenantId, paymentId, orderId, 3, 1, lineTotals);
        }

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        RefundReservationResult first = await ReserveAsync(
            connectionString,
            Refund(tenantId, paymentId, orderId, 1, "refund:line-fee:first", 3, 1, lineTotals),
            timeout.Token);
        RefundReservationResult second = await ReserveAsync(
            connectionString,
            Refund(tenantId, paymentId, orderId, 1, "refund:line-fee:second", 3, 1, lineTotals),
            timeout.Token);

        await using ExploreDbContext verification = await CreateContextAsync(connectionString, ensureCreated: false);
        RefundLineAllocation[] persisted = await verification.RefundLineAllocations
            .Where(line => line.TenantId == tenantId)
            .ToArrayAsync(timeout.Token);
        await Assert.That(first.Disposition).IsEqualTo(RefundReservationDisposition.Reserved);
        await Assert.That(second.Disposition).IsEqualTo(RefundReservationDisposition.Reserved);
        await Assert.That(persisted.All(line => line.PlatformFeeMinor <= line.OrganizerAmountMinor)).IsTrue();
        await Assert.That(persisted.Sum(line => line.OrganizerAmountMinor)).IsEqualTo(2L);
        await Assert.That(persisted.Sum(line => line.PlatformFeeMinor)).IsEqualTo(1L);
    }

    private static async Task<RefundReservationResult> ReserveAsync(
        string connectionString,
        RefundAttempt attempt,
        CancellationToken cancellationToken)
    {
        await using ExploreDbContext context = await CreateContextAsync(connectionString, ensureCreated: false);
        return await new RefundAttemptRepository(context).ReserveAsync(attempt, cancellationToken);
    }

    private static async Task<ExploreDbContext> CreateContextAsync(string connectionString, bool ensureCreated = true)
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(connectionString, provider => provider.EnableRetryOnFailure(3))
            .UseSnakeCaseNamingConvention()
            .Options;
        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Phase 19 PostgreSQL refund race test setup.");
        if (ensureCreated)
        {
            await context.Database.EnsureCreatedAsync();
        }
        return context;
    }

    private static async Task SeedCapturedPaymentAsync(
        ExploreDbContext context,
        Guid tenantId,
        Guid paymentId,
        Guid orderId,
        long organizerMinor = 1_000,
        long platformFeeMinor = 75,
        IReadOnlyList<long>? lineTotals = null)
    {
        await context.Database.OpenConnectionAsync();
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = replica;");
        var tenant = new Tenant
        {
            Id = tenantId,
            FullName = "Refund race tenant",
            Slug = $"refund-{tenantId:N}",
            TenantStatusId = 1,
            TenantStatus = null!,
            CreatedAt = UtcNow
        };
        RegistrationOrder order = RegistrationOrder.Create(
            orderId,
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(),
            RegistrationParticipationSnapshot.Create(
                Guid.CreateVersion7(), 1, 1, 1, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            null,
            null,
            "EUR",
            UtcNow,
            UtcNow.AddMinutes(30));
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "stripe", "platform-live-eu", "acct_original",
            "BE", "EUR", Guid.CreateVersion7(), null, UtcNow);
        PaymentAttempt payment = PaymentAttempt.Create(
            paymentId, tenantId, orderId, recipient, "OrganizerDirect", "2026-08-20.acacia", "refund-race",
            organizerMinor, platformFeeMinor, 0, $"payment:{tenantId:N}:{paymentId:N}", UtcNow, UtcNow.AddMinutes(30));
        payment.AttachAcceptance(Acceptance(
            tenantId, paymentId, orderId, organizerMinor, platformFeeMinor, lineTotals));
        payment.MarkSucceeded(PaymentProviderId(paymentId), UtcNow.AddSeconds(1), "req_payment");
        context.Tenants.Add(tenant);
        context.RegistrationOrders.Add(order);
        context.PaymentAttempts.Add(payment);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = origin;");
    }

    private static RefundAttempt Refund(
        Guid tenantId,
        Guid paymentId,
        Guid orderId,
        long totalMinor,
        string idempotencyKey,
        long organizerMinor = 1_000,
        long platformFeeMinor = 75,
        IReadOnlyList<long>? lineTotals = null) =>
        RefundAttempt.Create(
            Guid.CreateVersion7(), tenantId, paymentId,
            Acceptance(tenantId, paymentId, orderId, organizerMinor, platformFeeMinor, lineTotals), "acct_original",
            PaymentProviderId(paymentId), idempotencyKey, totalMinor, UtcNow.AddMinutes(1));

    private static string PaymentProviderId(Guid paymentId) => $"pi_{paymentId:N}";

    private static PaidOrderAcceptanceSnapshot Acceptance(
        Guid tenantId,
        Guid paymentId,
        Guid orderId,
        long organizerMinor = 1_000,
        long platformFeeMinor = 75,
        IReadOnlyList<long>? lineTotals = null) =>
        PaidOrderAcceptanceSnapshot.Create(
            paymentId, tenantId, tenantId, orderId, Guid.CreateVersion7(), "refund-race", "disclosure-1",
            "Example Organizer", PaidCheckoutOperatorDisclosure.Create(
                Guid.CreateVersion7(), "Example Operator", false, "https://events.example.test", "BE",
                "https://events.example.test", "https://events.example.test/legal", "https://events.example.test/terms",
                "https://events.example.test/privacy", "complaints@example.test", "Trust and Safety", "Payments Operations",
                "Dispute Operations", "Payment Reconciliation", "approved"),
            PaidOrderDeliverySnapshot.Create(
                DateTimeOffset.Parse("2026-09-10T17:00:00Z"), DateTimeOffset.Parse("2026-09-10T20:00:00Z"), "Europe/Brussels"),
            "EUR", organizerMinor, platformFeeMinor, 0, organizerMinor, Guid.CreateVersion7(), 7,
            "Refunds follow accepted policy v7.", "en-GB", "support@example.test",
            PaidCheckoutProviderDisclosure.Create(
                "stripe", "OrganizerDirect", "direct-charge", "EXAMPLE EVENT", "test", "instance-operator"),
            (lineTotals ?? [organizerMinor]).Select((total, index) => PaidOrderAcceptanceLineFact.Create(
                Guid.CreateVersion7(), $"Line {index + 1}", 1, total, 0, total)).ToArray(), UtcNow);
}

public sealed class RefundPostgreSqlContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("refund_test")
        .WithUsername("postgres")
        .WithPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(24)))
        .Build();

    public string ConnectionString
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
            {
                SearchPath = "islamu_event"
            };
            return builder.ConnectionString;
        }
    }

    public Task InitializeAsync() => container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await container.StopAsync();
        await container.DisposeAsync();
    }
}
