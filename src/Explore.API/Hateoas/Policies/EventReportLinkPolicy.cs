// ABOUTME: HATEOAS link policies for reporter-facing event-report resources.
// ABOUTME: Emits only public event navigation and authenticated submit/status affordances.

namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Hateoas;

public sealed class EventReportOptionsDetailLinkPolicy : ILinkPolicy<EventReportOptionsDto>
{
    public IEnumerable<LinkDefinition> GetLinks(EventReportOptionsDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventReportOptions,
            new { eventId = dto.EventId },
            "GET",
            "Event report options");

        yield return new LinkDefinition(
            LinkRelations.Event,
            RouteNames.GetEventById,
            new { id = dto.EventId },
            "GET",
            "Event details");

        if (dto.IsReportable)
        {
            yield return new LinkDefinition(
                LinkRelations.ReportEvent,
                RouteNames.SubmitEventReport,
                null,
                "POST",
                "Report event",
                RequiresAuth: true)
                .AdvertisedWhenAnonymous();
        }
    }
}

public sealed class EventReportOptionsCollectionLinkPolicy : ICollectionLinkPolicy<EventReportOptionsDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(EventReportOptionsDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventReportOptions,
            new { eventId = dto.EventId },
            "GET",
            "Event report options");

        yield return new LinkDefinition(
            LinkRelations.Event,
            RouteNames.GetEventById,
            new { id = dto.EventId },
            "GET",
            "Event details");

        if (dto.IsReportable)
        {
            yield return new LinkDefinition(
                LinkRelations.ReportEvent,
                RouteNames.SubmitEventReport,
                null,
                "POST",
                "Report event",
                RequiresAuth: true)
                .AdvertisedWhenAnonymous();
        }
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield break;
    }
}

public sealed class MyEventReportDetailLinkPolicy : ILinkPolicy<MyEventReportDto>
{
    public IEnumerable<LinkDefinition> GetLinks(MyEventReportDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetMyEventReport,
            new { reportId = dto.Id },
            "GET",
            "My event report",
            RequiresAuth: true);

        yield return new LinkDefinition(
            LinkRelations.Event,
            RouteNames.GetEventById,
            new { id = dto.EventId },
            "GET",
            "Event details");
    }
}

public sealed class MyEventReportCollectionLinkPolicy : ICollectionLinkPolicy<MyEventReportDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(MyEventReportDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetMyEventReport,
            new { reportId = dto.Id },
            "GET",
            "My event report",
            RequiresAuth: true);

        yield return new LinkDefinition(
            LinkRelations.Event,
            RouteNames.GetEventById,
            new { id = dto.EventId },
            "GET",
            "Event details");
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield break;
    }
}
