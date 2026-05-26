// ABOUTME: Command contract for idempotent tenant-level Basic Dispatch Mode pause and resume controls.
// ABOUTME: Keeps operator write actions in Application while workers read durable PostgreSQL control state.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

public sealed class SetEmailDispatchTenantPauseStateCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public Guid? ChangedBy { get; set; }
}
