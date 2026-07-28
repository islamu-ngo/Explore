// ABOUTME: Typed not-found signal for event ticketing child targets.
// ABOUTME: Keeps resolver failures on the existing generic ticketing response path.

namespace Explore.Application.Exceptions;

public sealed class TicketingNotFoundException : Exception
{
    public TicketingNotFoundException()
        : base("The ticketing target was not found.")
    {
    }
}
