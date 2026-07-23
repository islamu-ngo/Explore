// ABOUTME: Requests bounded privacy-erasure status for a receipt-authenticated intent.
// ABOUTME: Carries no subject identifier or provider payload across the API boundary.

using Explore.Application.DTOs.PrivacyErasure;
using MediatR;

namespace Explore.Application.Features.PrivacyErasure.Requests.Queries;

public sealed record GetPrivacyErasureStatusQuery(Guid IntentId)
    : IRequest<PrivacyErasureStatusDto?>;
