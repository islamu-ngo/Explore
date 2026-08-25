// ABOUTME: MediatR command for processing authenticated Osprey signal callbacks.
// ABOUTME: Keeps provider callback persistence inside Application while API owns transport authentication.

using Explore.Application.DTOs.EventReporting;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Commands;

public sealed record RecordOspreySignalCallbackCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required OspreySignalCallbackRequestDto Request { get; init; }
}
