// ABOUTME: Command request for creating an event session template with optional nested property definitions.
// ABOUTME: Uses tenant-level authorization since session templates are tenant-governed configuration catalogs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplates.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class CreateEventSessionTemplateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventSessionTemplateDto SessionTemplateDto { get; set; }

    string? ISecureRequest.ResourceId => null;
}
