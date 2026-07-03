// ABOUTME: MediatR query for retrieving the canonical outgoing webhook event catalog.
// ABOUTME: Reads provider-neutral event metadata used by LocalProvider, SvixProvider, and documentation.

using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

public sealed record GetWebhookEventTypesQuery : IRequest<IReadOnlyList<WebhookEventTypeDto>>;
