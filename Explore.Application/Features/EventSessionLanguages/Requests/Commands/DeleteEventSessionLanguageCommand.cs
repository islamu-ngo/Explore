// ABOUTME: MediatR command for removing a language from an event session.
// ABOUTME: Carries the junction record ID.
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Commands;

[AuthorizeResource("event_session", AuthorizationActions.Update)]
public class DeleteEventSessionLanguageCommand : IRequest<bool>, ISecureRequest
{
    public int Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
