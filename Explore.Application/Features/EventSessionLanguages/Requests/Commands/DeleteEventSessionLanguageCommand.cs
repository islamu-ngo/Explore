using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands;

[AuthorizeResource("event_session", PermissionAction.Update)]
public class DeleteEventSessionLanguageCommand : IRequest<bool>, ISecureRequest
{
    public int Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
