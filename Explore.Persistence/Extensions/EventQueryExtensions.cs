// ABOUTME: Centralized Include chain for Event queries to eliminate duplication across EventRepository methods.
// ABOUTME: Only includes navigation properties — callers control tracking strategy (AsNoTracking, AsSplitQuery).

using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Extensions;

internal static class EventQueryExtensions
{
    internal static IQueryable<Event> IncludeStandardDetails(this IQueryable<Event> query)
    {
        return query
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(e => e.Actor)
                .ThenInclude(a => a!.ProfilePicture)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.Madhab)
            .Include(e => e.IslamicAspect)
                .ThenInclude(a => a!.PrimaryLanguage)
            .Include(e => e.TechAspect)
            .Include(e => e.RegistrationPolicy);
    }
}
