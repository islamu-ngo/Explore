// ABOUTME: Stateless domain-service implementation converting UTC intervals to cached local projection fields using TimeZoneInfo.
// ABOUTME: DST-aware and boundary-safe; handlers, validators, mappers, and seeders are forbidden from reimplementing this logic elsewhere.

using System;

namespace Explore.Domain.Services.Scheduling;

public sealed class EventScheduleProjectionCalculator : IEventScheduleProjectionCalculator
{
    public LocalScheduleProjection Project(DateTimeOffset startUtc, DateTimeOffset endUtc, string? timezoneId)
    {
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("endUtc must be strictly greater than startUtc.", nameof(endUtc));
        }

        var timezone = ScheduleTimeZoneResolver.ResolveOrUtc(timezoneId);

        var localStart = TimeZoneInfo.ConvertTime(startUtc, timezone);
        var localEnd = TimeZoneInfo.ConvertTime(endUtc, timezone);

        var startDate = DateOnly.FromDateTime(localStart.DateTime);
        var endDate = DateOnly.FromDateTime(localEnd.DateTime);
        var startTime = TimeOnly.FromDateTime(localStart.DateTime);
        var endTime = TimeOnly.FromDateTime(localEnd.DateTime);

        var startMinute = (startTime.Hour * 60) + startTime.Minute;
        var endMinute = (endTime.Hour * 60) + endTime.Minute;

        return new LocalScheduleProjection(
            startDate,
            endDate,
            startTime,
            endTime,
            startMinute,
            endMinute);
    }

}
