// ABOUTME: Command contract for operator parking of an unsafe EmailDispatch outbox row.
// ABOUTME: Captures tenant scope, target row, audit actor, and bounded reason before durable state mutation.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

public sealed class ParkEmailDispatchCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public Guid OutboxId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ChangedBy { get; set; }
}
