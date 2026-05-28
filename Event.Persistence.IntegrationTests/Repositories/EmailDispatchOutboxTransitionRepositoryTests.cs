// ABOUTME: PostgreSQL-backed tests for EmailDispatch operator replay and parking transitions.
// ABOUTME: Verifies durable state-machine changes that future RabbitMQ consumers and admin actions reuse.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EmailDispatchOutboxTransitionRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task TryParkForOperatorMarksEligibleRowAsParked()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "park");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.DeadLettered);
        var actorId = Guid.NewGuid();
        var parkedAt = DateTime.UtcNow;
        var repository = new EmailDispatchOutboxRepository(context);

        var parked = await repository.TryParkForOperator(
            tenant.Id,
            dispatch.Id,
            "operator quarantine",
            actorId,
            parkedAt,
            CancellationToken.None);

        await Assert.That(parked).IsTrue();

        var row = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .SingleAsync(outbox => outbox.Id == dispatch.Id);
        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Parked);
        await Assert.That(row.ParkedAt).IsEqualTo(parkedAt);
        await Assert.That(row.LastFailureCategory).IsEqualTo("operator_parked");
        await Assert.That(row.LastError).IsEqualTo("operator quarantine");
        await Assert.That(row.UpdatedBy).IsEqualTo(actorId);
        await Assert.That(row.NextAttemptAt).IsNull();
        await Assert.That(row.ProcessingLeaseToken).IsNull();
    }

    [Test]
    public async Task TryReplayForOperatorResetsDeferredRowToPending()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "replay");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.DeadLettered);
        var actorId = Guid.NewGuid();
        var replayAt = DateTime.UtcNow;
        var repository = new EmailDispatchOutboxRepository(context);

        var replayed = await repository.TryReplayForOperator(
            tenant.Id,
            dispatch.Id,
            actorId,
            replayAt,
            CancellationToken.None);

        await Assert.That(replayed).IsTrue();

        var row = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .SingleAsync(outbox => outbox.Id == dispatch.Id);
        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Pending);
        await Assert.That(row.NextAttemptAt).IsNull();
        await Assert.That(row.DeadLetteredAt).IsNull();
        await Assert.That(row.ParkedAt).IsNull();
        await Assert.That(row.UnknownAt).IsNull();
        await Assert.That(row.LastFailureCategory).IsNull();
        await Assert.That(row.LastError).IsNull();
        await Assert.That(row.UpdatedBy).IsEqualTo(actorId);
    }

    [Test]
    public async Task TryReplayForOperatorDoesNotReplaySentRow()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = await SeedTenantAsync(context, "sent");
        var dispatch = await SeedDispatchAsync(context, tenant.Id, EmailDispatchStatus.Sent);
        var repository = new EmailDispatchOutboxRepository(context);

        var replayed = await repository.TryReplayForOperator(
            tenant.Id,
            dispatch.Id,
            Guid.NewGuid(),
            DateTime.UtcNow,
            CancellationToken.None);

        await Assert.That(replayed).IsFalse();

        var row = await context.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .SingleAsync(outbox => outbox.Id == dispatch.Id);
        await Assert.That(row.Status).IsEqualTo(EmailDispatchStatus.Sent);
    }

    private static async Task<Tenant> SeedTenantAsync(ExploreDbContext context, string slugPrefix)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = $"Email Dispatch {slugPrefix}",
            Slug = $"email-dispatch-{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant;
    }

    private static async Task<EmailDispatchOutbox> SeedDispatchAsync(
        ExploreDbContext context,
        Guid tenantId,
        EmailDispatchStatus status)
    {
        var now = DateTime.UtcNow;
        var dispatch = new EmailDispatchOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PublishEventId = Guid.NewGuid(),
            Kind = EmailDispatchKind.RegistrationConfirmation,
            SourceType = "event_registration_intent",
            SourceId = Guid.NewGuid(),
            RecipientEmail = "recipient@example.test",
            Subject = "Registration confirmation",
            PlainTextBody = "plain body",
            HtmlBody = "<p>html body</p>",
            Status = status,
            AttemptCount = status == EmailDispatchStatus.Pending ? 0 : 3,
            MaxAttempts = 5,
            NextAttemptAt = status == EmailDispatchStatus.RetryScheduled ? now.AddHours(1) : null,
            DeadLetteredAt = status == EmailDispatchStatus.DeadLettered ? now : null,
            ParkedAt = status == EmailDispatchStatus.Parked ? now : null,
            UnknownAt = status == EmailDispatchStatus.Unknown ? now : null,
            SentAt = status == EmailDispatchStatus.Sent ? now : null,
            LastFailureCategory = status == EmailDispatchStatus.Pending ? null : "smtp_send_failed",
            LastError = status == EmailDispatchStatus.Pending ? null : "previous failure",
            LastFailureAt = status == EmailDispatchStatus.Pending ? null : now,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.EmailDispatchOutbox.Add(dispatch);
        await context.SaveChangesAsync();
        return dispatch;
    }
}
