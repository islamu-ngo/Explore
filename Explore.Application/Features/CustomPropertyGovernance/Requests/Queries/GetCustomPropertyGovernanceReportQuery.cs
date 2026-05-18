// ABOUTME: Query for the Rule 12 operational governance surface listing all active Layer 3 definitions with promotion recommendations.
// ABOUTME: Implements Atlassian 4-question matrix; authorized via property_governance_admin policy.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyGovernance;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CustomPropertyGovernance.Requests.Queries;

[AuthorizeResource(ResourceKinds.CustomPropertyGovernance, AuthorizationActions.View)]
public class GetCustomPropertyGovernanceReportQuery : IRequest<PaginatedResult<CustomPropertyGovernanceRowDto>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public GovernanceReportFilterDto Filter { get; set; } = new();

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
