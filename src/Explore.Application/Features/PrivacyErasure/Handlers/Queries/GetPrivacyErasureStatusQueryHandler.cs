// ABOUTME: Resolves bounded privacy-erasure progress after receipt authentication.
// ABOUTME: Delegates receipt-state lookup to the shared platform erasure workflow.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.PrivacyErasure;
using Explore.Application.Features.PrivacyErasure.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.PrivacyErasure.Handlers.Queries;

public sealed class GetPrivacyErasureStatusQueryHandler(IPrivacyErasureService service)
    : IRequestHandler<GetPrivacyErasureStatusQuery, PrivacyErasureStatusDto?>
{
    public Task<PrivacyErasureStatusDto?> Handle(
        GetPrivacyErasureStatusQuery request,
        CancellationToken cancellationToken) =>
        service.GetStatusAsync(request.IntentId, cancellationToken);
}
