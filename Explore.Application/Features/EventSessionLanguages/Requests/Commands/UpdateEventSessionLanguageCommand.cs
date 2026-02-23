using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands;

[AuthorizeResource("event_session", PermissionAction.Update)]
public class UpdateEventSessionLanguageCommand : IRequest<BaseCommandResponse<int>>, ISecureRequest
{
    public required UpdateEventSessionLanguageDto EventSessionLanguageDto { get; set; }

    string? ISecureRequest.ResourceId => EventSessionLanguageDto.EventSessionId.ToString();
}
