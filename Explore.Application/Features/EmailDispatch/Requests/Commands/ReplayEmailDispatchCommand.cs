// ABOUTME: Command contract for operator replay of a deferred EmailDispatch outbox row.
// ABOUTME: Requeues eligible rows by resetting durable PostgreSQL state without touching SMTP or RabbitMQ directly.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

public sealed class ReplayEmailDispatchCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public Guid OutboxId { get; set; }
    public Guid? ChangedBy { get; set; }
}
