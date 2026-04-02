// ABOUTME: Command request for creating an event template with optional nested property definitions.
// ABOUTME: Uses tenant-level authorization since templates are tenant-governed configuration catalogs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplates.Requests.Commands;

[AuthorizeResource("tenant", AuthorizationActions.Update)]
public class CreateEventTemplateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventTemplateDto TemplateDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
