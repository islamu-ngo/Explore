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

    public Guid TenantId { get; set; }

    public Guid EventId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionLanguageDto.EventSessionId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["eventId"] = EventId.ToString()
    };
}
