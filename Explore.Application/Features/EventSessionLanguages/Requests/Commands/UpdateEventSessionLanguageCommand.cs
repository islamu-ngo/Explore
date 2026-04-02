// ABOUTME: MediatR command for updating a session-language link.
// ABOUTME: Carries the UpdateEventSessionLanguageDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands;

[AuthorizeResource("event_session", AuthorizationActions.Update)]
public class UpdateEventSessionLanguageCommand : IRequest<BaseCommandResponse<int>>, ISecureRequest
{
    public required UpdateEventSessionLanguageDto EventSessionLanguageDto { get; set; }

    string? ISecureRequest.ResourceId => EventSessionLanguageDto.EventSessionId.ToString();
}
