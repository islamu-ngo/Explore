// ABOUTME: Thrown when an EventSession insert/update would place a session in the same room as an existing session whose time range overlaps.
// ABOUTME: Raised by Layer B (serializable transaction re-check) when a racing write slipped past Layer A's async validator.

using System;
using System.Collections.Generic;

namespace Explore.Application.Exceptions;

public class RoomScheduleConflictException : ApplicationException
{
    public Guid RoomId { get; }
    public IReadOnlyList<Guid> ConflictingSessionIds { get; }

    public RoomScheduleConflictException(Guid roomId, IReadOnlyList<Guid> conflictingSessionIds)
        : base(CreateMessage(conflictingSessionIds))
    {
        RoomId = roomId;
        ConflictingSessionIds = conflictingSessionIds;
    }

    private static string CreateMessage(IReadOnlyList<Guid> conflictingSessionIds)
    {
        return conflictingSessionIds.Count == 0
            ? "The selected room already has an overlapping session in the requested time range."
            : $"The selected room already has {conflictingSessionIds.Count} overlapping session(s) in the requested time range.";
    }
}
