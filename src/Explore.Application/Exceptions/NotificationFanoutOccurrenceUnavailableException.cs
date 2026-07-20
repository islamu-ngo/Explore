// ABOUTME: Signals that a recipient graph lost fanout authority before its transaction could persist.
// ABOUTME: Lets the worker stop a superseded occurrence without treating precedence as an operational failure.

namespace Explore.Application.Exceptions;

public sealed class NotificationFanoutOccurrenceUnavailableException : Exception
{
    public NotificationFanoutOccurrenceUnavailableException()
        : base("The notification fanout occurrence is no longer available for recipient materialization.")
    {
    }
}
