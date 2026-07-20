// ABOUTME: Unit tests for fenced notification fanout audience page processing.
// ABOUTME: Proves recipient commits precede checkpoints and stale or corrupt work fails closed.

using System.Text.Json;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Notifications;

public sealed class NotificationFanoutPageProcessorTests
{
    [Test]
    public async Task MultiPageAudienceUsesStableCursorAndCompletes()
    {
        var fixture = new Fixture();
        NotificationFanoutAudienceMember first = fixture.Member(1);
        NotificationFanoutAudienceMember second = fixture.Member(2);
        NotificationFanoutAudienceMember third = fixture.Member(3);
        fixture.ConfigurePages(after => after switch
        {
            null => [first, second],
            { UserId: var userId } when userId == second.UserId => [third],
            { UserId: var userId } when userId == third.UserId => [],
            _ => throw new InvalidOperationException("Unexpected cursor.")
        });
        var materializedRecipients = new List<Guid>();
        fixture.MaterializationService.MaterializeAsync(
                fixture.Occurrence,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Guid recipientUserId = call.ArgAt<Guid>(1);
                materializedRecipients.Add(recipientUserId);
                return fixture.ResultFor(recipientUserId);
            });

        NotificationFanoutPageProcessingResult result = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 2,
            fixture.LeaseDuration);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutPageProcessingOutcome.Completed);
        await Assert.That(result.PagesCheckpointed).IsEqualTo(2);
        await Assert.That(result.RecipientsMaterialized).IsEqualTo(3);
        await Assert.That(result.NotificationsCreated).IsEqualTo(3);
        await Assert.That(materializedRecipients).Count().IsEqualTo(3);
        await Assert.That(materializedRecipients[0]).IsEqualTo(first.UserId);
        await Assert.That(materializedRecipients[1]).IsEqualTo(second.UserId);
        await Assert.That(materializedRecipients[2]).IsEqualTo(third.UserId);
        await fixture.RunRepository.Received(1).TryCheckpointAsync(
            Arg.Is<NotificationFanoutClaim>(claim => claim.Cursor == null),
            null,
            new NotificationFanoutAudienceCursor(second.FirstEligibleRegistrationCreatedAt, second.UserId),
            2,
            2,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.RunRepository.Received(1).TryCheckpointAsync(
            Arg.Is<NotificationFanoutClaim>(claim =>
                claim.Cursor.HasValue && claim.Cursor.Value.UserId == second.UserId),
            new NotificationFanoutAudienceCursor(second.FirstEligibleRegistrationCreatedAt, second.UserId),
            new NotificationFanoutAudienceCursor(third.FirstEligibleRegistrationCreatedAt, third.UserId),
            1,
            1,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.RunRepository.Received(1).TryCompleteAsync(
            Arg.Is<NotificationFanoutClaim>(claim =>
                claim.Cursor.HasValue && claim.Cursor.Value.UserId == third.UserId),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecipientFailurePropagatesWithoutCheckpoint()
    {
        var fixture = new Fixture();
        NotificationFanoutAudienceMember first = fixture.Member(1);
        NotificationFanoutAudienceMember second = fixture.Member(2);
        fixture.ConfigurePages(_ => [first, second]);
        fixture.MaterializationService.MaterializeAsync(
                fixture.Occurrence,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Guid>(1) == first.UserId
                ? fixture.ResultFor(first.UserId)
                : throw new InvalidOperationException("recipient transaction failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 2,
            fixture.LeaseDuration));

        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryCheckpointAsync(
            default!, default, default, default, default, default, default);
        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryCompleteAsync(default!, default, default);
    }

    [Test]
    public async Task ReplayAfterPartialPageConvergesAndAdvancesOnce()
    {
        var fixture = new Fixture();
        NotificationFanoutAudienceMember first = fixture.Member(1);
        NotificationFanoutAudienceMember second = fixture.Member(2);
        fixture.ConfigurePages(after => after is null ? [first, second] : []);
        var committedRecipients = new HashSet<Guid>();
        int materializationCalls = 0;
        bool failSecondRecipientOnce = true;
        fixture.MaterializationService.MaterializeAsync(
                fixture.Occurrence,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Guid recipientUserId = call.ArgAt<Guid>(1);
                materializationCalls++;
                if (recipientUserId == second.UserId && failSecondRecipientOnce)
                {
                    failSecondRecipientOnce = false;
                    throw new InvalidOperationException("worker crashed");
                }

                committedRecipients.Add(recipientUserId);
                return fixture.ResultFor(recipientUserId);
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 2,
            fixture.LeaseDuration));
        NotificationFanoutPageProcessingResult replay = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 2,
            fixture.LeaseDuration);

        await Assert.That(replay.Outcome).IsEqualTo(NotificationFanoutPageProcessingOutcome.Completed);
        await Assert.That(committedRecipients).Count().IsEqualTo(2);
        await Assert.That(materializationCalls).IsEqualTo(4);
        await fixture.RunRepository.Received(1).TryCheckpointAsync(
            Arg.Any<NotificationFanoutClaim>(),
            null,
            new NotificationFanoutAudienceCursor(second.FirstEligibleRegistrationCreatedAt, second.UserId),
            2,
            2,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailedCheckpointReturnsStaleAfterCommittedRecipients()
    {
        var fixture = new Fixture();
        NotificationFanoutAudienceMember first = fixture.Member(1);
        NotificationFanoutAudienceMember second = fixture.Member(2);
        fixture.ConfigurePages(_ => [first, second]);
        fixture.RunRepository.TryCheckpointAsync(
                Arg.Any<NotificationFanoutClaim>(),
                Arg.Any<NotificationFanoutAudienceCursor?>(),
                Arg.Any<NotificationFanoutAudienceCursor>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        NotificationFanoutPageProcessingResult result = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 2,
            fixture.LeaseDuration);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutPageProcessingOutcome.StaleClaim);
        await Assert.That(result.PagesCheckpointed).IsEqualTo(0);
        await Assert.That(result.RecipientsMaterialized).IsEqualTo(2);
        await Assert.That(result.NotificationsCreated).IsEqualTo(2);
        await fixture.RunRepository.Received(1).TryCheckpointAsync(
            Arg.Any<NotificationFanoutClaim>(),
            null,
            Arg.Any<NotificationFanoutAudienceCursor>(),
            2,
            2,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryCompleteAsync(default!, default, default);
    }

    [Test]
    public async Task StaleRenewalStopsBeforeNextRecipient()
    {
        var fixture = new Fixture();
        NotificationFanoutAudienceMember first = fixture.Member(1);
        NotificationFanoutAudienceMember second = fixture.Member(2);
        fixture.ConfigurePages(_ => [first, second]);
        int renewals = 0;
        fixture.RunRepository.TryRenewClaimAsync(
                Arg.Any<NotificationFanoutClaim>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => ++renewals < 3);

        NotificationFanoutPageProcessingResult result = await fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 2,
            fixture.LeaseDuration);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutPageProcessingOutcome.StaleClaim);
        await Assert.That(result.RecipientsMaterialized).IsEqualTo(1);
        await fixture.MaterializationService.Received(1).MaterializeAsync(
            fixture.Occurrence,
            first.UserId,
            Arg.Any<CancellationToken>());
        await fixture.MaterializationService.DidNotReceive().MaterializeAsync(
            fixture.Occurrence,
            second.UserId,
            Arg.Any<CancellationToken>());
        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryCheckpointAsync(
            default!, default, default, default, default, default, default);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task CorruptOrNonMonotonicPageFailsClosed(bool duplicateRecipient)
    {
        var fixture = new Fixture();
        NotificationFanoutAudienceMember first = fixture.Member(2);
        NotificationFanoutAudienceMember invalid = duplicateRecipient
            ? new NotificationFanoutAudienceMember(
                first.UserId,
                first.FirstEligibleRegistrationCreatedAt.AddSeconds(1))
            : fixture.Member(1);
        fixture.ConfigurePages(_ => [first, invalid]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 2,
            fixture.LeaseDuration));

        await fixture.MaterializationService.DidNotReceiveWithAnyArgs().MaterializeAsync(
            default!, default, default);
        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryCheckpointAsync(
            default!, default, default, default, default, default, default);
    }

    [Test]
    public async Task WrongMaterializationAuthorityFailsClosed()
    {
        var fixture = new Fixture();
        NotificationFanoutAudienceMember member = fixture.Member(1);
        fixture.ConfigurePages(_ => [member]);
        RecipientNotificationMaterializationResult wrong = fixture.ResultFor(member.UserId);
        wrong.Intent.TenantId = Guid.CreateVersion7();
        fixture.MaterializationService.MaterializeAsync(
                fixture.Occurrence,
                member.UserId,
                Arg.Any<CancellationToken>())
            .Returns(wrong);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 1,
            fixture.LeaseDuration));

        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryCheckpointAsync(
            default!, default, default, default, default, default, default);
    }

    [Test]
    public async Task MissingRequiredInAppNotificationFailsClosed()
    {
        var fixture = new Fixture();
        NotificationFanoutAudienceMember member = fixture.Member(1);
        fixture.ConfigurePages(_ => [member]);
        RecipientNotificationMaterializationResult valid = fixture.ResultFor(member.UserId);
        fixture.MaterializationService.MaterializeAsync(
                fixture.Occurrence,
                member.UserId,
                Arg.Any<CancellationToken>())
            .Returns(valid with { Notification = null });

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 1,
            fixture.LeaseDuration));

        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryCheckpointAsync(
            default!, default, default, default, default, default, default);
    }

    [Test]
    public async Task UnknownTemplateNeverCompletesAnEmptyAudience()
    {
        var fixture = new Fixture("event.unsupported");

        await Assert.ThrowsAsync<JsonException>(() => fixture.Processor.ProcessAsync(
            fixture.Claim,
            pageSize: 1,
            fixture.LeaseDuration));

        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryRenewClaimAsync(
            default!, default, default, default);
        await fixture.RegistrationIntentRepository.DidNotReceiveWithAnyArgs()
            .GetNotificationFanoutAudienceBatchAsync(
                default, default, default, default, default, default, default, default);
        await fixture.RunRepository.DidNotReceiveWithAnyArgs().TryCompleteAsync(default!, default, default);
    }

    private sealed class Fixture
    {
        private static readonly DateTime Now = new(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);

        public Fixture(string templateKey = NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey)
        {
            Occurrence = CreateOccurrence(templateKey);
            Claim = new NotificationFanoutClaim(
                Guid.CreateVersion7(),
                Occurrence.TenantId,
                Occurrence.Id,
                Guid.CreateVersion7(),
                1,
                1,
                null);
            OccurrenceRepository.GetByPointerAsync(
                    Arg.Is<Explore.Application.Models.InternalEvents.NotificationFanoutOccurrenceRequested>(pointer =>
                        pointer.TenantId == Occurrence.TenantId
                        && pointer.OccurrenceId == Occurrence.Id),
                    false,
                    Arg.Any<CancellationToken>())
                .Returns(Occurrence);
            RunRepository.TryRenewClaimAsync(
                    Arg.Any<NotificationFanoutClaim>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            RunRepository.TryCheckpointAsync(
                    Arg.Any<NotificationFanoutClaim>(),
                    Arg.Any<NotificationFanoutAudienceCursor?>(),
                    Arg.Any<NotificationFanoutAudienceCursor>(),
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            RunRepository.TryCompleteAsync(
                    Arg.Any<NotificationFanoutClaim>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            MaterializationService.MaterializeAsync(
                    Occurrence,
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => ResultFor(call.ArgAt<Guid>(1)));
            Processor = new NotificationFanoutPageProcessor(
                OccurrenceRepository,
                RegistrationIntentRepository,
                RunRepository,
                MaterializationService,
                new NotificationFanoutRecipientTemplateFactory(),
                new FixedTimeProvider(Now));
        }

        public INotificationFanoutOccurrenceRepository OccurrenceRepository { get; } =
            Substitute.For<INotificationFanoutOccurrenceRepository>();

        public IEventRegistrationIntentRepository RegistrationIntentRepository { get; } =
            Substitute.For<IEventRegistrationIntentRepository>();

        public INotificationFanoutRunRepository RunRepository { get; } =
            Substitute.For<INotificationFanoutRunRepository>();

        public INotificationFanoutRecipientMaterializationService MaterializationService { get; } =
            Substitute.For<INotificationFanoutRecipientMaterializationService>();

        public NotificationFanoutOccurrence Occurrence { get; }
        public NotificationFanoutClaim Claim { get; }
        public TimeSpan LeaseDuration { get; } = TimeSpan.FromMinutes(1);
        public NotificationFanoutPageProcessor Processor { get; }

        public NotificationFanoutAudienceMember Member(int second) =>
            new(Guid.Parse($"{second:D8}-0000-0000-0000-000000000001"), Now.AddSeconds(second));

        public void ConfigurePages(
            Func<NotificationFanoutAudienceCursor?, IReadOnlyList<NotificationFanoutAudienceMember>> select)
        {
            RegistrationIntentRepository.GetNotificationFanoutAudienceBatchAsync(
                    Occurrence.TenantId,
                    Occurrence.EventId,
                    Occurrence.SessionId,
                    Occurrence.AudienceCutoffAt,
                    Occurrence.DeliveryPolicyId,
                    Arg.Any<NotificationFanoutAudienceCursor?>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => select(call.ArgAt<NotificationFanoutAudienceCursor?>(5)));
        }

        public RecipientNotificationMaterializationResult ResultFor(Guid recipientUserId)
        {
            var intent = new NotificationIntent
            {
                Id = Guid.CreateVersion7(),
                TenantId = Occurrence.TenantId,
                CategoryId = (int)NotificationCategoryEnum.EventLifecycle,
                OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
                RecipientKindId = (int)NotificationRecipientKindEnum.User,
                StatusId = (int)NotificationIntentStatusEnum.Resolved,
                TemplateKey = Occurrence.TemplateKey,
                DeduplicationKey = $"{Occurrence.Id:N}:{recipientUserId:N}",
                RecipientUserId = recipientUserId,
                FanoutOccurrenceId = Occurrence.Id
            };
            var notification = new Notification
            {
                Id = Guid.CreateVersion7(),
                TenantId = Occurrence.TenantId,
                Tenant = null!,
                NotificationIntentId = intent.Id,
                NotificationIntent = intent,
                UserId = recipientUserId,
                User = null!,
                NotificationTypeId = (int)NotificationTypeEnum.EventUpdated,
                NotificationType = null!,
                Title = "Event updated",
                DeduplicationKey = $"{intent.DeduplicationKey}:in-app",
                NotificationScopeId = (int)ActorTypeEnum.User,
                NotificationScope = null!,
                CreatedAt = Now
            };
            return new RecipientNotificationMaterializationResult(intent, [], notification, null);
        }

        private static NotificationFanoutOccurrence CreateOccurrence(string templateKey)
        {
            string snapshot = NotificationFanoutTemplateJson.Serialize(
                new NotificationFanoutSnapshotV1(
                    "Immutable event",
                    null,
                    new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    "UTC",
                    null));
            return NotificationFanoutOccurrence.Create(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                null,
                Now,
                Now,
                Guid.CreateVersion7(),
                NotificationFanoutTemplateJson.Serialize(
                    new NotificationFanoutChangeSetV1(
                        [NotificationFanoutChangeField.StartTime])),
                snapshot,
                snapshot,
                templateKey,
                1,
                (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
                1,
                30,
                Now,
                "event",
                Guid.CreateVersion7(),
                $"event:{Guid.CreateVersion7():N}",
                null);
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
