// ABOUTME: MediatR command for a reporter changing communication consent on their own report.
// ABOUTME: Carries the route-owned report id separately from the two explicit consent purposes.

using Explore.Application.DTOs.EventReporting;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Commands;

public sealed class UpdateMyReportCommunicationConsentCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid ReportId { get; init; }
    public required UpdateMyReportCommunicationConsentDto Request { get; init; }
}
