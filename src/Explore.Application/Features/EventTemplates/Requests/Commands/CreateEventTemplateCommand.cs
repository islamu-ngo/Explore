// ABOUTME: Command request for creating an event template with optional nested property definitions.
// ABOUTME: Uses tenant-level authorization since templates are tenant-governed configuration catalogs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplates.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public sealed record CreateEventTemplateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventTemplateDto TemplateDto { get; init; }

    string? ISecureRequest.ResourceId => null;
}
