// ABOUTME: Defines event add-on public, management, and order-scoped CQRS reads.
// ABOUTME: Keeps tenant and caller authority outside transport-selected query fields.

using Explore.Application.DTOs.EventAddOns;
using MediatR;

namespace Explore.Application.Features.EventAddOns.Requests.Queries;

public sealed record GetEventAddOnCatalogQuery(
    Guid EventId,
    bool ManagementView) : IRequest<EventAddOnCatalogDto?>;

public sealed record GetRegistrationOrderAddOnsQuery(
    Guid EventId,
    Guid RegistrationOrderId,
    string? Capability) : IRequest<RegistrationOrderAddOnSummaryDto?>;
