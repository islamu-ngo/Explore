// ABOUTME: Query for instance-admin usage reporting across tenants.
// ABOUTME: Returns aggregated request counts and credit usage per API key without secret material.

using Explore.Application.DTOs.ExternalApiKey;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Requests.Queries;

public class GetExternalApiKeyUsageReportRequest : IRequest<List<ExternalApiKeyUsageReportDto>>
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public Guid? TenantId { get; set; }
}
