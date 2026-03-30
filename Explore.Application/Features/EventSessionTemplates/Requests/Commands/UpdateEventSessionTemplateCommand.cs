// ABOUTME: Command request for updating an event session template with full definition replacement.
// ABOUTME: Keeps tenant-governed update semantics aligned with the create flow.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplates.Requests.Commands;

[AuthorizeResource("tenant", PermissionAction.Update)]
public class UpdateEventSessionTemplateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventSessionTemplateDto SessionTemplateDto { get; set; }

    string? ISecureRequest.ResourceId => SessionTemplateDto.Id.ToString();
}
