// ABOUTME: Failing-first contract tests for internal ATProto event import command orchestration.
// ABOUTME: Proves validated tenant plans attach to the existing atomic Jetstream repository request.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Handlers.Commands;
using Explore.Application.Features.Federation.Atproto.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Federation;
using FluentValidation;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Federation;

public sealed class ImportAtprotoFederatedEventCommandHandlerTests
{
    private static readonly Guid RecordId = Guid.CreateVersion7();
    private static readonly Guid VisibleTenantId = Guid.CreateVersion7();
    private static readonly Guid HiddenTenantId = Guid.CreateVersion7();
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Handle_ValidProjection_AttachesOnePlanPerVisibleTenantToAtomicRepositoryRequest()
    {
        var repository = Substitute.For<IAtprotoJetstreamRepository>();
        repository.TryApplyAndAdvanceAsync(
                Arg.Any<AtprotoJetstreamApplyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new ImportAtprotoFederatedEventCommandHandler(repository);
        AtprotoJetstreamApplyRequest applyRequest = CreateApplyRequest(
            CreateProjection(),
            [
                Presentation(VisibleTenantId, isVisible: true),
                Presentation(HiddenTenantId, isVisible: false)
            ]);

        bool result = await handler.Handle(
            new ImportAtprotoFederatedEventCommand(applyRequest),
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await repository.Received(1).TryApplyAndAdvanceAsync(
            Arg.Is<AtprotoJetstreamApplyRequest>(request =>
                request.EventImports.Count == 1
                && request.EventImports[0].TenantId == VisibleTenantId
                && request.EventImports[0].AtprotoRecordId == RecordId
                && request.EventImports[0].Did == "did:plc:community-owner"
                && request.EventImports[0].AtUri == "at://did:plc:community-owner/community.lexicon.calendar.event/event-1"
                && request.EventImports[0].Name == "Community iftar"
                && request.EventImports[0].CreatedAt == CreatedAt
                && request.EventImports[0].SourceUrl == "https://events.example.org/program/iftar"
                && request.EventImports[0].StartsAt == CreatedAt.AddDays(2)
                && request.EventImports[0].EndsAt == CreatedAt.AddDays(2).AddHours(2)
                && request.EventImports[0].Mode == "#hybrid"
                && request.EventImports[0].Status == "#scheduled"
                && request.EventImports[0].RsvpExpected == true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_InvalidProjection_FailsBeforeAtomicRepositoryCall()
    {
        var repository = Substitute.For<IAtprotoJetstreamRepository>();
        var handler = new ImportAtprotoFederatedEventCommandHandler(repository);
        AtprotoEventProjection invalidProjection = CreateProjection();
        invalidProjection.Name = " ";
        invalidProjection.CreatedAt = default;
        AtprotoJetstreamApplyRequest applyRequest = CreateApplyRequest(
            invalidProjection,
            [Presentation(VisibleTenantId, isVisible: true)]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                new ImportAtprotoFederatedEventCommand(applyRequest),
                CancellationToken.None));

        await repository.DidNotReceiveWithAnyArgs()
            .TryApplyAndAdvanceAsync(default!, default);
    }

    [Test]
    public async Task Handle_Tombstone_AttachesNoImportPlansAndStillUsesAtomicRepository()
    {
        var repository = Substitute.For<IAtprotoJetstreamRepository>();
        repository.TryApplyAndAdvanceAsync(
                Arg.Any<AtprotoJetstreamApplyRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new ImportAtprotoFederatedEventCommandHandler(repository);
        AtprotoJetstreamApplyRequest applyRequest = CreateApplyRequest(
            eventProjection: null,
            presentations: [],
            invalidation: new AtprotoEventProjectionInvalidation(
                "did:plc:community-owner",
                "community.lexicon.calendar.event",
                "event-1",
                102));

        bool result = await handler.Handle(
            new ImportAtprotoFederatedEventCommand(applyRequest),
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await repository.Received(1).TryApplyAndAdvanceAsync(
            Arg.Is<AtprotoJetstreamApplyRequest>(request => request.EventImports.Count == 0),
            Arg.Any<CancellationToken>());
    }

    private static AtprotoJetstreamApplyRequest CreateApplyRequest(
        AtprotoEventProjection? eventProjection,
        IReadOnlyList<AtprotoRecordTenantPresentation> presentations,
        AtprotoEventProjectionInvalidation? invalidation = null)
    {
        var claim = new AtprotoJetstreamClaim(
            Guid.CreateVersion7(),
            "wss://jetstream.example.org/subscribe",
            Cursor: 100,
            Guid.CreateVersion7(),
            LeaseFence: 2);
        var record = new AtprotoRecord
        {
            Id = RecordId,
            Did = "did:plc:community-owner",
            Collection = "community.lexicon.calendar.event",
            RecordKey = "event-1",
            Uri = "at://did:plc:community-owner/community.lexicon.calendar.event/event-1",
            Direction = AtprotoRecordDirection.Inbound,
            Provenance = AtprotoRecordProvenance.Jetstream,
            SourceVersion = 101,
            UpdatedAt = CreatedAt.UtcDateTime
        };

        return new AtprotoJetstreamApplyRequest(
            claim,
            ExpectedCursor: 100,
            NextCursor: 101,
            Record: invalidation is null ? record : null,
            Presentations: presentations,
            Quarantine: null,
            ObservedAt: CreatedAt.UtcDateTime,
            AdvanceCursor: true,
            EventProjection: eventProjection,
            EventProjectionInvalidation: invalidation);
    }

    private static AtprotoEventProjection CreateProjection() => new()
    {
        AtprotoRecordId = RecordId,
        Name = "Community iftar",
        Description = "A public community event.",
        CreatedAt = CreatedAt,
        StartsAt = CreatedAt.AddDays(2),
        EndsAt = CreatedAt.AddDays(2).AddHours(2),
        Mode = "#hybrid",
        Status = "#scheduled",
        RsvpExpected = true,
        SourceUrl = "https://events.example.org/program/iftar",
        SourceVersion = 101,
        MaterializedAt = CreatedAt.UtcDateTime
    };

    private static AtprotoRecordTenantPresentation Presentation(Guid tenantId, bool isVisible) => new()
    {
        TenantId = tenantId,
        AtprotoRecordId = RecordId,
        IsVisible = isVisible,
        SourceVersion = 101,
        EvaluatedAt = CreatedAt.UtcDateTime
    };
}
