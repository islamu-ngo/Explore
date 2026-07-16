// ABOUTME: MediatR command for grouped route-ID updates to a session-language link.
// ABOUTME: Carries the route id, If-Match concurrency stamp, and grouped payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public class UpdateEventSessionLanguageCommand : IRequest<BaseCommandResponse<int>>, ISecureRequest
{
    public int EventSessionLanguageId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateEventSessionLanguageDto EventSessionLanguageDto { get; set; }

    public Guid EventSessionId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionId.ToString();

}
