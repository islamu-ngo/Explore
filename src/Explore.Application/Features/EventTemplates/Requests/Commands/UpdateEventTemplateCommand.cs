// ABOUTME: Command request for partially updating an event template by route-owned identity.
// ABOUTME: Authorization binds persisted tenant context before the handler checks concurrency.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplates.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateEventTemplateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TemplateId { get; set; }
    public required UpdateEventTemplateDto TemplateDto { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object> { ["tenantId"] = TenantId.ToString() };
}
