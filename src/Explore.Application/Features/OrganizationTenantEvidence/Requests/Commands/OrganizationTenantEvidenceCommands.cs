// ABOUTME: Authorized CQRS commands for OrganizationTenant legitimacy-evidence submission and review.
// ABOUTME: Keeps organization-admin submission separate from tenant-admin review.

using Explore.Application.Authorization;
using Explore.Application.DTOs.OrganizationTenantEvidence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationTenantEvidence.Requests.Commands;

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Organizations.SubmitEvidence)]
public sealed class CreateOrganizationTenantEvidenceUploadSessionCommand
    : IRequest<BaseCommandResponse<StorageUploadSessionDto>>, ISecureRequest
{
    public Guid OrganizationId { get; init; }
    public required CreateOrganizationTenantEvidenceUploadSessionDto Upload { get; init; }
    string? ISecureRequest.ResourceId => OrganizationId == Guid.Empty ? null : OrganizationId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["organizationId"] = OrganizationId.ToString()
    };
}

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Organizations.SubmitEvidence)]
public sealed class SubmitOrganizationTenantEvidenceCommand
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; init; }
    public required SubmitOrganizationTenantEvidenceDto Evidence { get; init; }
    string? ISecureRequest.ResourceId => OrganizationId == Guid.Empty ? null : OrganizationId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["organizationId"] = OrganizationId.ToString()
    };
}

[AuthorizeResource(ResourceKinds.Organization, AuthorizationActions.Organizations.ReviewEvidence)]
public sealed class ReviewOrganizationTenantEvidenceCommand
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid OrganizationId { get; init; }
    public Guid EvidenceId { get; init; }
    public required ReviewOrganizationTenantEvidenceDto Review { get; init; }
    string? ISecureRequest.ResourceId => OrganizationId == Guid.Empty ? null : OrganizationId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["organizationId"] = OrganizationId.ToString()
    };
}
