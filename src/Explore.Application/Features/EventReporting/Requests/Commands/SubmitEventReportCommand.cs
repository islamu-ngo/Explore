// ABOUTME: MediatR command for authenticated event-report intake.
// ABOUTME: Carries user-entered report details plus server-derived reporter hashes for deduplication.

using Explore.Application.DTOs.EventReporting;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Commands;

public sealed record SubmitEventReportCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required SubmitEventReportDto Request { get; init; }
    public string? ReporterIpHash { get; init; }
    public string? ReporterUserAgentHash { get; init; }
    public string? CorrelationId { get; init; }
}
