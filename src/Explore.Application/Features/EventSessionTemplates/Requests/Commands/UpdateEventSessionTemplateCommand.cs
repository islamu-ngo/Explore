// ABOUTME: Command request for partially updating an event session template by route-owned identity.
// ABOUTME: Authorization binds persisted tenant context before the handler checks concurrency.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplates.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public class UpdateEventSessionTemplateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid SessionTemplateId { get; set; }
    public required UpdateEventSessionTemplateDto SessionTemplateDto { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object> { ["tenantId"] = TenantId.ToString() };
}
