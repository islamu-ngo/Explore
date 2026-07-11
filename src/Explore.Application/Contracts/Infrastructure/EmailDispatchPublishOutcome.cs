// ABOUTME: Enumerates broker-level outcomes for RabbitMQ EmailDispatch pointer publishes.
// ABOUTME: Keeps transport result semantics explicit without leaking RabbitMQ.Client types upward.

namespace Explore.Application.Contracts.Infrastructure;

public enum EmailDispatchPublishOutcome
{
    Disabled = 0,
    Confirmed = 1,
    Returned = 2,
    Nacked = 3,
    Failed = 4
}
