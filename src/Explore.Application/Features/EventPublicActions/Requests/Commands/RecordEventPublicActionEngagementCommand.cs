// ABOUTME: Metric-only CQRS request for event-public-action redirect engagement.
// ABOUTME: Carries only closed action-kind and surface facts for bounded telemetry labels.

using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventPublicActions.Requests.Commands;

public sealed record RecordEventPublicActionEngagementCommand(
    EventPublicActionKindEnum ActionKind,
    string? Surface) : IRequest<Unit>;
