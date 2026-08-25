// ABOUTME: Exercises admission delivery through the real production intent dispatcher and composite outbox route.
// ABOUTME: Proves typed cancellation/failure boundaries, protected pending state, and acknowledged ciphertext retirement.

using System.Diagnostics.Metrics;
using System.Text.Json;
using Explore.Application.Caching;
using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Models;
using Explore.Application.Services;
using Explore.Application.Services.Registration;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Infrastructure.Messaging;
using Explore.Infrastructure.Services.Moderation;
using Explore.Infrastructure.Services.Registration;
using Explore.Persistence;
using Explore.Persistence.Services;
using Explore.Tests.Shared.Telemetry;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class AdmissionCompositeDispatchTests
{
    [Test]
    public async Task ProductionAdmissionDispatcherRoutesThroughCompositeAndRetiresOnlyAfterAcknowledgement()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var context = new ExploreDbContext(options);
        Guid tenantId = Guid.CreateVersion7();
        context.TenantContext = new AdmissionTenantContext(tenantId);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        DateTime now = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
        const string bearer = "restart-safe-admission-bearer-canary";
        const string recipient = "attendee@example.test";
        var envelopeProtector = new AdmissionDeliveryEnvelopeProtector(new EphemeralDataProtectionProvider());
        AdmissionProtectedDeliveryMaterial protectedMaterial = envelopeProtector.Protect(
            new AdmissionCredentialDeliveryEnvelope(recipient, bearer));
        var intent = new AdmissionDeliveryIntent(
            Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            protectedMaterial.Ciphertext, protectedMaterial.ProtectionVersion, now);
        context.AdmissionDeliveryIntents.Add(intent);
        await context.SaveChangesAsync();
        EmailMessage? observedMessage = null;
        var email = Substitute.For<IEmailService>();
        email.SendAsync(Arg.Do<EmailMessage>(message => observedMessage = message), Arg.Any<CancellationToken>())
            .Returns(EmailResult.Ok("accepted"));
        var channel = new AdmissionEmailCredentialDeliveryChannel(email);

        CompositeOutboxMessageDispatcher composite = CreateComposite(
            new AdmissionCredentialDeliveryOutboxHandler(context, envelopeProtector, channel, new FixedTimeProvider(now)));
        var dispatcher = new AdmissionDeliveryIntentDispatcher(
            context, composite, NullLogger<AdmissionDeliveryIntentDispatcher>.Instance);
        var unsafeMessage = new OutboxMessage
        {
            Id = intent.Id,
            AggregateType = nameof(AdmissionTicket),
            AggregateId = intent.AdmissionTicketId,
            EventType = AdmissionDeliveryEvents.CredentialDeliveryRequested,
            Payload = JsonSerializer.Serialize(new
            {
                TenantId = tenantId,
                AdmissionTicketId = intent.AdmissionTicketId,
                DeliveryIntentId = intent.Id,
                PlaintextCredential = "must-not-cross-outbox-boundary"
            })
        };

        await Assert.That(async () => await composite.DispatchAsync(unsafeMessage))
            .Throws<InvalidOperationException>();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        AdmissionDeliveryDispatchResult cancelledResult = await dispatcher.DispatchAsync(
            new AdmissionDeliveryDispatchRequest(intent.Id), cancelled.Token);
        AdmissionDeliveryDispatchResult malformedResult = await dispatcher.DispatchAsync(
            new AdmissionDeliveryDispatchRequest(Guid.Empty), CancellationToken.None);
        AdmissionDeliveryDispatchResult routed = await dispatcher.DispatchAsync(
            new AdmissionDeliveryDispatchRequest(intent.Id), CancellationToken.None);
        context.ChangeTracker.Clear();
        AdmissionDeliveryIntent pending = await context.AdmissionDeliveryIntents.SingleAsync();

        await Assert.That(cancelledResult).IsEqualTo(new AdmissionDeliveryDispatchResult(
            AdmissionDeliveryOutcome.RecoverablePending, AdmissionDeliveryFailure.Cancelled));
        await Assert.That(malformedResult).IsEqualTo(new AdmissionDeliveryDispatchResult(
            AdmissionDeliveryOutcome.Unrecoverable, AdmissionDeliveryFailure.InvalidIntent));
        await Assert.That(routed).IsEqualTo(new AdmissionDeliveryDispatchResult(AdmissionDeliveryOutcome.Delivered));
        await Assert.That(observedMessage).IsNotNull();
        await Assert.That(observedMessage!.To).IsEqualTo(recipient);
        await Assert.That(observedMessage.PlainTextBody).Contains(bearer);
        await Assert.That(observedMessage.CustomHeaders["X-Admission-Delivery-Idempotency-Key"])
            .IsEqualTo(intent.Id.ToString("N"));
        await Assert.That(pending.RoutedAt).IsEqualTo(now);
        await Assert.That(pending.HandoffCompletedAt).IsEqualTo(now);
        await Assert.That(pending.HandoffReceiptId).IsEqualTo($"smtp:{intent.Id:N}");
        await Assert.That(pending.ProtectedCredential).IsEmpty();
    }

    private static CompositeOutboxMessageDispatcher CreateComposite(
        IAdmissionCredentialDeliveryOutboxHandler admissionHandler)
    {
        HybridCache cache = new MinimalHybridCache();
        var correctionPlanner = Substitute.For<IAtprotoLocationPrivacyCorrectionPlanner>();
        correctionPlanner.PlanLocationPrivacyCorrectionAsync(
                Arg.Any<AtprotoLocationPrivacyCorrectionInput>(), Arg.Any<CancellationToken>())
            .Returns(AtprotoPublicationPlanningResult.Skipped("correction_already_planned"));
        var refundRepository = Substitute.For<IRefundAttemptRepository>();
        var campaignRepository = Substitute.For<IRefundCampaignRepository>();
        var refundCreator = Substitute.For<IRefundCreator>();
        return new CompositeOutboxMessageDispatcher(
            CreateNotificationHandoff(),
            Substitute.For<IEventPublishedNotificationFanoutService>(),
            Substitute.For<IEventModerationNotificationFanoutService>(),
            Substitute.For<IReportProviderSyncDispatcher>(),
            new LocationPrivacyCorrectionDispatcher(cache, correctionPlanner, EventLocationPrivacyMetricsFactory.Create()),
            new PrivacyErasureCacheInvalidationDispatcher(cache),
            admissionHandler,
            Substitute.For<IAdmissionRecoveryDeliveryOutboxHandler>(),
            Substitute.For<IOutboxRepository>(),
            campaignRepository,
            new RefundCampaignProcessor(
                campaignRepository, refundRepository,
                Substitute.For<IRegistrationMaterialChangeChoiceRepository>(),
                Substitute.For<IRegistrationPaymentAttemptRepository>(), TimeProvider.System),
            new RefundDispatchService(refundRepository, refundCreator, TimeProvider.System),
            new RefundReconciliationService(
                refundRepository, refundCreator, Substitute.For<IRefundRetriever>(), TimeProvider.System),
            new RegistrationPaymentCancellationService(
                Substitute.For<IRegistrationPaymentAttemptRepository>(), refundRepository, campaignRepository,
                Substitute.For<IPaymentCancellationProvider>(), TimeProvider.System),
            CreateMetrics(),
            TimeProvider.System,
            Substitute.For<IMediator>(),
            NullLogger<CompositeOutboxMessageDispatcher>.Instance);
    }

    private static NotificationFanoutOccurrenceHandoffService CreateNotificationHandoff()
    {
        Type type = typeof(NotificationFanoutOccurrenceHandoffService);
        var constructor = type.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length).First();
        object?[] arguments = constructor.GetParameters()
            .Select(parameter => Substitute.For([parameter.ParameterType], []))
            .ToArray();
        return (NotificationFanoutOccurrenceHandoffService)constructor.Invoke(arguments);
    }

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }

    private sealed record AdmissionTenantContext(Guid TenantId) : ITenantContext;

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class MinimalHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null, CancellationToken cancellationToken = default) =>
            factory(state, cancellationToken);
        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
