// ABOUTME: MediatR command for removing a language from an event session.
// ABOUTME: Carries the junction record ID.
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public class DeleteEventSessionLanguageCommand : IRequest<bool>, ISecureRequest
{
    public int Id { get; set; }

    public Guid EventSessionId { get; set; }

    public Guid TenantId { get; set; }

    public Guid EventId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["eventId"] = EventId.ToString()
    };
}
