// ABOUTME: Unit tests for the event calendar download controller contract.
// ABOUTME: Verifies .ics file responses, sanitized filenames, and missing calendar exports.

using System.Text;
using Explore.API.Controllers;
using Explore.API.Hateoas;
using Explore.API.Services.Calendar;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features.Calendar;

public sealed class EventControllerCalendarTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPublicUrlBuilder _publicUrlBuilder = Substitute.For<IPublicUrlBuilder>();
    private readonly EventController _controller;

    public EventControllerCalendarTests()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            TraceIdentifier = "trace-calendar-test"
        };
        httpContext.Request.Path = "/api/event/22222222-3333-4444-5555-666666666666/calendar";

        _controller = new EventController(
            _mediator,
            Substitute.For<ILogger<EventController>>(),
            Substitute.For<IResourceAssembler<EventDto, EventListDto>>(),
            new IcalNetEventCalendarFileBuilder(),
            _publicUrlBuilder)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Test]
    public async Task GetCalendarForPublishedPublicEventReturnsTextCalendarAttachment()
    {
        var eventId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var export = new EventCalendarExportDto(
            eventId,
            "Community Iftar",
            "Bring a dish to share.",
            "Community Iftar 2026!",
            new DateTimeOffset(2026, 5, 1, 18, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 1, 20, 0, 0, TimeSpan.Zero),
            "Main Hall, Brussels");

        _mediator.Send(Arg.Is<GetEventCalendarExportRequest>(request => request.EventId == eventId), Arg.Any<CancellationToken>())
            .Returns(export);
        _publicUrlBuilder.GetEventUrl(eventId)
            .Returns("https://events.example.org/events/11111111-2222-3333-4444-555555555555");

        IActionResult result = await _controller.GetCalendar(eventId, CancellationToken.None);

        var file = result as FileContentResult;
        await Assert.That(file).IsNotNull();
        await Assert.That(file!.ContentType).IsEqualTo("text/calendar; charset=utf-8");
        await Assert.That(file.FileDownloadName).IsEqualTo("community-iftar-2026.ics");

        string calendar = Encoding.UTF8.GetString(file.FileContents);
        await Assert.That(calendar).Contains("BEGIN:VCALENDAR");
        await Assert.That(calendar).Contains("BEGIN:VEVENT");
        await Assert.That(calendar).Contains($"UID:{eventId:D}");
        await Assert.That(calendar).Contains("SUMMARY:Community Iftar");
        await Assert.That(calendar).Contains("DTSTART:20260501T183000Z");
        await Assert.That(calendar).Contains("DTEND:20260501T200000Z");
        await Assert.That(calendar).Contains("LOCATION:Main Hall\\, Brussels");
        await Assert.That(calendar).Contains("URL:https://events.example.org/events/11111111-2222-3333-4444-555555555555");
    }

    [Test]
    public async Task GetCalendarWhenExportIsUnavailableReturnsNotFound()
    {
        var eventId = Guid.Parse("22222222-3333-4444-5555-666666666666");
        _mediator.Send(Arg.Is<GetEventCalendarExportRequest>(request => request.EventId == eventId), Arg.Any<CancellationToken>())
            .Returns((EventCalendarExportDto?)null);

        IActionResult result = await _controller.GetCalendar(eventId, CancellationToken.None);

        await Assert.That(result).IsTypeOf<ObjectResult>();
        var objectResult = (ObjectResult)result;
        await Assert.That(objectResult.StatusCode).IsEqualTo(404);
    }
}
