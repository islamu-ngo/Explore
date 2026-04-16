// ABOUTME: Immutable value object holding the six cached local projection fields computed from a UTC interval and IANA timezone.
// ABOUTME: Returned by IEventScheduleProjectionCalculator; entities copy these fields into their persisted columns via aggregate methods.

using System;

namespace Explore.Domain.Services.Scheduling;

public readonly record struct LocalScheduleProjection(
    DateOnly LocalStartDate,
    DateOnly LocalEndDate,
    TimeOnly LocalStartTime,
    TimeOnly LocalEndTime,
    int LocalStartMinuteOfDay,
    int LocalEndMinuteOfDay);
