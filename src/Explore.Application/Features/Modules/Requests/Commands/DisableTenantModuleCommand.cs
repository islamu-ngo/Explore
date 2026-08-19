// ABOUTME: Command request for disabling a module capability for the current tenant.
// ABOUTME: Uses tenant update authorization so module governance follows tenant-admin policy.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Modules.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public sealed class DisableTenantModuleCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required Guid TenantId { get; init; }
    public required string ModuleKey { get; init; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);
}
