// ABOUTME: Command request for updating an event template with full definition replacement.
// ABOUTME: Keeps tenant-governed update semantics aligned with the create flow.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplates.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateEventTemplateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventTemplateDto TemplateDto { get; set; }

    string? ISecureRequest.ResourceId => TemplateDto.Id.ToString();
}
