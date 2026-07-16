// ABOUTME: Shared EF query predicates for anonymous event program and agenda eligibility.
// ABOUTME: Keeps public child reads subordinate to published public parent events and published scheduled sessions.

using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Persistence.Extensions;

internal static class PublicEventEligibilityQueryExtensions
{
    internal static IQueryable<EventSession> WherePubliclyEligible(this IQueryable<EventSession> query) =>
        query.Where(session =>
            session.EventSessionStatusId == (int)EventSessionStatusEnum.Published
            && session.StartTime != null
            && session.EndTime != null
            && (session.EventDayId == null || session.EventDay!.IsPublished)
            && session.Event.EventStatusId == (int)EventStatusEnum.Published
            && session.Event.VisibilityTypeId == (int)VisibilityTypeEnum.Public);

    internal static IQueryable<EventSessionGroup> WherePubliclyEligible(this IQueryable<EventSessionGroup> query) =>
        query.Where(group =>
            group.IsPublished
            && group.Event.EventStatusId == (int)EventStatusEnum.Published
            && group.Event.VisibilityTypeId == (int)VisibilityTypeEnum.Public);

    internal static IQueryable<EventSessionGroupSession> WherePubliclyEligible(this IQueryable<EventSessionGroupSession> query) =>
        query.Where(assignment =>
            assignment.EventSessionGroup.IsPublished
            && assignment.EventSessionGroup.Event.EventStatusId == (int)EventStatusEnum.Published
            && assignment.EventSessionGroup.Event.VisibilityTypeId == (int)VisibilityTypeEnum.Public
            && assignment.EventSession.EventSessionStatusId == (int)EventSessionStatusEnum.Published
            && assignment.EventSession.StartTime != null
            && assignment.EventSession.EndTime != null
            && (assignment.EventSession.EventDayId == null || assignment.EventSession.EventDay!.IsPublished)
            && assignment.EventSession.Event.EventStatusId == (int)EventStatusEnum.Published
            && assignment.EventSession.Event.VisibilityTypeId == (int)VisibilityTypeEnum.Public);

    internal static IQueryable<EventAgendaItem> WherePubliclyEligible(this IQueryable<EventAgendaItem> query) =>
        query.Where(item =>
            (item.EventDayId == null || item.EventDay!.IsPublished)
            && item.Event.EventStatusId == (int)EventStatusEnum.Published
            && item.Event.VisibilityTypeId == (int)VisibilityTypeEnum.Public);

    internal static IQueryable<EventSessionAgendaItem> WherePubliclyEligible(this IQueryable<EventSessionAgendaItem> query) =>
        query.Where(item =>
            item.EventSession.EventSessionStatusId == (int)EventSessionStatusEnum.Published
            && item.EventSession.StartTime != null
            && item.EventSession.EndTime != null
            && (item.EventSession.EventDayId == null || item.EventSession.EventDay!.IsPublished)
            && item.EventSession.Event.EventStatusId == (int)EventStatusEnum.Published
            && item.EventSession.Event.VisibilityTypeId == (int)VisibilityTypeEnum.Public);
}
