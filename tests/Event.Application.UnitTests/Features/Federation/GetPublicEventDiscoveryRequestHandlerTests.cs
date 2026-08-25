// ABOUTME: Verifies governed deterministic merging of local and typed ATProto public event projections.
// ABOUTME: Covers disablement, unsupported filters, later-page interleaving, echo precedence, and safe sources.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Features.Federation.Atproto.Handlers.Queries;
using Explore.Application.Features.Federation.Atproto.Requests.Queries;
using Explore.Application.Responses;
using Explore.Application.Services.Federation;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Federation;
using Explore.Domain.Settings;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Federation;

public sealed class GetPublicEventDiscoveryRequestHandlerTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task DisabledCapabilitySkipsProjectionReadsAndStripsFederationMetadata()
    {
        Guid recordId = Guid.CreateVersion7();
        var local = Local("Local", recordId);
        var fixture = CreateFixture(false, [local]);

        PaginatedResult<Explore.Application.DTOs.PublicExperience.EventDiscoveryItemDto> result =
            await fixture.Handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result.Items).Count().IsEqualTo(1);
        await Assert.That(result.Items[0].Event).IsSameReferenceAs(local);
        await Assert.That(result.Items[0].Federation).IsNull();
        await fixture.Projections.DidNotReceiveWithAnyArgs()
            .GetPublicWindowAsync(default!, default);
        await fixture.Projections.DidNotReceiveWithAnyArgs()
            .GetVisibleByRecordIdsAsync(default!, default);
    }

    [Test]
    public async Task UnsupportedLocalOnlyFilterDoesNotReadFederatedProjection()
    {
        var fixture = CreateFixture(true, [Local("Local")]);
        GetPublicEventDiscoveryRequest request = Request();
        request = request with
        {
            Criteria = request.Criteria with { CategoryId = Guid.CreateVersion7() }
        };

        PaginatedResult<Explore.Application.DTOs.PublicExperience.EventDiscoveryItemDto> result =
            await fixture.Handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Items).Count().IsEqualTo(1);
        await fixture.Projections.DidNotReceiveWithAnyArgs()
            .GetPublicWindowAsync(default!, default);
    }

    [Test]
    public async Task LaterPageInterleavesBothSourcesWithoutStarvation()
    {
        var fixture = CreateFixture(true, [Local("Alpha"), Local("Charlie"), Local("Echo"), Local("Golf")]);
        fixture.Projections.GetPublicWindowAsync(
                Arg.Any<AtprotoEventProjectionQuery>(),
                Arg.Any<CancellationToken>())
            .Returns((new[] { Federated("Bravo"), Federated("Delta"), Federated("Foxtrot"), Federated("Hotel") }, 4));
        GetPublicEventDiscoveryRequest request = Request(pageNumber: 2, pageSize: 2, sortBy: "title");

        PaginatedResult<Explore.Application.DTOs.PublicExperience.EventDiscoveryItemDto> result =
            await fixture.Handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Items.Select(Title).SequenceEqual(["Charlie", "Delta"])).IsTrue();
        await Assert.That(result.Items.Select(item => item.Source).SequenceEqual(["local", "atproto"])).IsTrue();
        await fixture.Projections.Received(1).GetPublicWindowAsync(
            Arg.Is<AtprotoEventProjectionQuery>(query => query.Take == 4),
            Arg.Any<CancellationToken>());
        await fixture.Local.Received(1).Handle(
            Arg.Is<GetEventListRequest>(query => query.PageNumber == 1 && query.PageSize == 4),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task WindowBeyondMaximumIsRejectedBeforeAnyEventRead()
    {
        var fixture = CreateFixture(true, []);

        async Task Act() => await fixture.Handler.Handle(
            Request(pageNumber: 11, pageSize: 100),
            CancellationToken.None);

        await Assert.That(Act).Throws<FluentValidation.ValidationException>();
        await fixture.Local.DidNotReceiveWithAnyArgs().Handle(default!, default);
        await fixture.Projections.DidNotReceiveWithAnyArgs().GetPublicWindowAsync(default!, default);
    }

    [Test]
    public async Task LocalEchoWinsAndReceivesSourceAffordanceFromTypedProjection()
    {
        Guid recordId = Guid.CreateVersion7();
        var local = Local("Owned", recordId);
        var fixture = CreateFixture(true, [local]);
        fixture.Projections.GetPublicWindowAsync(
                Arg.Any<AtprotoEventProjectionQuery>(),
                Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AtprotoEventProjection>(), 0));
        fixture.Projections.GetVisibleByRecordIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([Federated("Owned", recordId, "https://events.example/source")]);

        PaginatedResult<Explore.Application.DTOs.PublicExperience.EventDiscoveryItemDto> result =
            await fixture.Handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result.Items).Count().IsEqualTo(1);
        await Assert.That(result.Items[0].Source).IsEqualTo("local");
        await Assert.That(result.Items[0].Event).IsSameReferenceAs(local);
        await Assert.That(result.Items[0].Federation!.IsLocalEcho).IsTrue();
        await Assert.That(result.Items[0].Federation!.HasSourceLink).IsTrue();
    }

    [Test]
    public async Task LocalEchoOmitsSourceAffordanceWhenRepositoryDeniesProjection()
    {
        Guid recordId = Guid.CreateVersion7();
        var local = Local("Owned", recordId);
        var fixture = CreateFixture(true, [local]);

        PaginatedResult<Explore.Application.DTOs.PublicExperience.EventDiscoveryItemDto> result =
            await fixture.Handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result.Items).HasSingleItem();
        await Assert.That(result.Items[0].Source).IsEqualTo("local");
        await Assert.That(result.Items[0].Federation!.IsLocalEcho).IsTrue();
        await Assert.That(result.Items[0].Federation!.HasSourceLink).IsFalse();
        await fixture.Projections.Received(1).GetVisibleByRecordIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments("http://events.example/source")]
    [Arguments("https://user:secret@events.example/source")]
    [Arguments("https://events.example/source#fragment")]
    [Arguments("https://localhost/source")]
    [Arguments("https://tenant.localhost/source")]
    [Arguments("https://127.0.0.1/source")]
    [Arguments("https://10.0.0.1/source")]
    [Arguments("https://[::1]/source")]
    public async Task ExternalSourcePolicyRejectsUnsafeRedirects(string value)
    {
        await Assert.That(AtprotoExternalUriPolicy.Normalize(value)).IsNull();
    }

    [Test]
    public async Task ExternalSourcePolicyNormalizesPublicHttpsDnsSource()
    {
        await Assert.That(AtprotoExternalUriPolicy.Normalize("HTTPS://Events.Example/path?q=1"))
            .IsEqualTo("https://events.example/path?q=1");
    }

    [Test]
    public async Task SourceQueryDisabledCapabilityDoesNotReadProjection()
    {
        var projections = Substitute.For<IAtprotoEventProjectionRepository>();
        GetAtprotoEventSourceQueryHandler handler = CreateSourceHandler(false, projections);

        string? result = await handler.Handle(
            new GetAtprotoEventSourceQuery(Guid.CreateVersion7()),
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await projections.DidNotReceiveWithAnyArgs().GetVisibleByRecordIdAsync(default, default);
    }

    [Test]
    public async Task SourceQueryRevalidatesStoredUriBeforeRedirect()
    {
        Guid recordId = Guid.CreateVersion7();
        var projections = Substitute.For<IAtprotoEventProjectionRepository>();
        projections.GetVisibleByRecordIdAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Federated("Unsafe", recordId, "http://private.example/source"));
        GetAtprotoEventSourceQueryHandler handler = CreateSourceHandler(true, projections);

        string? result = await handler.Handle(
            new GetAtprotoEventSourceQuery(recordId),
            CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task SourceQueryReturnsNullWhenRepositoryDeniesProjection()
    {
        Guid recordId = Guid.CreateVersion7();
        var projections = Substitute.For<IAtprotoEventProjectionRepository>();
        projections.GetVisibleByRecordIdAsync(recordId, Arg.Any<CancellationToken>())
            .Returns((AtprotoEventProjection?)null);
        GetAtprotoEventSourceQueryHandler handler = CreateSourceHandler(true, projections);

        string? result = await handler.Handle(
            new GetAtprotoEventSourceQuery(recordId),
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await projections.Received(1).GetVisibleByRecordIdAsync(recordId, Arg.Any<CancellationToken>());
    }

    private static DiscoveryFixture CreateFixture(bool enabled, IReadOnlyList<EventListDto> localItems)
    {
        var local = Substitute.For<IRequestHandler<GetEventListRequest, PaginatedResult<EventListDto>>>();
        local.Handle(Arg.Any<GetEventListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => PaginatedResult<EventListDto>.Create(
                localItems.Take(call.ArgAt<GetEventListRequest>(0).PageSize).ToList(),
                localItems.Count,
                1,
                call.ArgAt<GetEventListRequest>(0).PageSize));
        var projections = Substitute.For<IAtprotoEventProjectionRepository>();
        projections.GetPublicWindowAsync(Arg.Any<AtprotoEventProjectionQuery>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AtprotoEventProjection>(), 0));
        projections.GetVisibleByRecordIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AtprotoEventProjection>());
        var settings = Substitute.For<IHierarchicalSettingsResolver>();
        settings.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<IEnumerable<string>>(0).Select(key => Setting(key, enabled)).ToArray());
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(TenantId);
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        var handler = new GetPublicEventDiscoveryRequestHandler(
            local,
            projections,
            new AtprotoEventGovernanceResolver(settings),
            tenant,
            time);
        return new DiscoveryFixture(handler, local, projections);
    }

    private static GetAtprotoEventSourceQueryHandler CreateSourceHandler(
        bool enabled,
        IAtprotoEventProjectionRepository projections)
    {
        var settings = Substitute.For<IHierarchicalSettingsResolver>();
        settings.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<IEnumerable<string>>(0).Select(key => Setting(key, enabled)).ToArray());
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(TenantId);
        return new GetAtprotoEventSourceQueryHandler(
            projections,
            new AtprotoEventGovernanceResolver(settings),
            tenant);
    }

    private static ResolvedSetting Setting(string key, bool enabled) => new()
    {
        Key = key,
        Value = key == GovernanceSettingKeys.Federation.AtprotoEventsEnabled
            ? enabled ? "true" : "false"
            : "\"platform\"",
        ValueType = key == GovernanceSettingKeys.Federation.AtprotoEventsEnabled
            ? SettingValueType.Boolean
            : SettingValueType.String,
        Source = SettingSource.SystemDefault
    };

    private static GetPublicEventDiscoveryRequest Request(
        int pageNumber = 1,
        int pageSize = 20,
        string sortBy = "date") => new(new GetEventListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDescending = false
        });

    private static EventListDto Local(string title, Guid? recordId = null) => new()
    {
        Id = Guid.CreateVersion7(),
        Title = title,
        EventTypeFullName = "Event",
        AudienceGenderFullName = "All",
        AudienceAgeFullName = "All",
        ActorDisplayName = "Organizer",
        ActorTypeFullName = "Organization",
        EventStatusFullName = "Published",
        VisibilityTypeFullName = "Public",
        EventFormatFullName = "Local",
        CreatedAtUtc = Now,
        FirstSessionStartUtc = Now.AddDays(1),
        AtprotoRecordId = recordId,
        TenantId = TenantId
    };

    private static AtprotoEventProjection Federated(
        string name,
        Guid? id = null,
        string? sourceUrl = null) => new()
        {
            AtprotoRecordId = id ?? Guid.CreateVersion7(),
            Name = name,
            CreatedAt = Now,
            StartsAt = Now.AddDays(1),
            SourceUrl = sourceUrl,
            MaterializedAt = Now.UtcDateTime
        };

    private static string Title(Explore.Application.DTOs.PublicExperience.EventDiscoveryItemDto item) =>
        item.Event?.Title ?? item.FederatedEvent?.Name ?? string.Empty;

    private sealed record DiscoveryFixture(
        GetPublicEventDiscoveryRequestHandler Handler,
        IRequestHandler<GetEventListRequest, PaginatedResult<EventListDto>> Local,
        IAtprotoEventProjectionRepository Projections);
}
