// ABOUTME: Handles instance-admin usage report queries with optional per-tenant filtering.
// ABOUTME: Requires instance-admin or tenant-admin authority; maps repository summaries to safe DTOs.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Features.ExternalApiKeys.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ExternalApiKeys.Handlers.Queries;

public class GetExternalApiKeyUsageReportRequestHandler : IRequestHandler<GetExternalApiKeyUsageReportRequest, List<ExternalApiKeyUsageReportDto>>
{
    private readonly IExternalApiKeyQuotaRepository _quotaRepository;
    private readonly IAdminContext _adminContext;

    public GetExternalApiKeyUsageReportRequestHandler(
        IExternalApiKeyQuotaRepository quotaRepository,
        IAdminContext adminContext)
    {
        _quotaRepository = quotaRepository;
        _adminContext = adminContext;
    }

    public async Task<List<ExternalApiKeyUsageReportDto>> Handle(GetExternalApiKeyUsageReportRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<TenantApiKeyUsageSummary> summaries;

        if (request.TenantId.HasValue)
        {
            var isTenantAdmin = await _adminContext.IsTenantAdminAsync(request.TenantId.Value, cancellationToken);
            var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);

            if (!isTenantAdmin && !isInstanceAdmin)
            {
                return [];
            }

            summaries = await _quotaRepository.GetUsageByTenant(request.TenantId.Value, request.From, request.To, cancellationToken);
        }
        else
        {
            if (!await _adminContext.IsInstanceAdminAsync(cancellationToken))
            {
                return [];
            }

            summaries = await _quotaRepository.GetUsagePlatformWide(request.From, request.To, cancellationToken);
        }

        return summaries
            .Select(s => new ExternalApiKeyUsageReportDto
            {
                ApiKeyId = s.ApiKeyId,
                ApiKeyName = s.ApiKeyName,
                TenantId = s.TenantId,
                OwnerType = (ExternalApiKeyOwnerType)s.OwnerType,
                OwnerId = s.OwnerId,
                TotalRequestCount = s.TotalRequestCount,
                TotalCreditsUsed = s.TotalCreditsUsed,
                CreditLimit = s.CreditLimit
            })
            .ToList();
    }
}
