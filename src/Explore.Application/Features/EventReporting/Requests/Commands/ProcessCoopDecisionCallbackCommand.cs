// ABOUTME: MediatR command for processing signed Coop review-decision callbacks.
// ABOUTME: Keeps provider decision capture and local execution orchestration inside Application.

using Explore.Application.DTOs.EventReporting;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Commands;

public sealed class ProcessCoopDecisionCallbackCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CoopDecisionCallbackRequestDto Request { get; init; }
}
