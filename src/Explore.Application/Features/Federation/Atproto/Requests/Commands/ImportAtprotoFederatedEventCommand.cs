// ABOUTME: Requests atomic canonical ATProto persistence with validated tenant-local event import plans.
// ABOUTME: Carries the existing fenced Jetstream apply request without adding outbound federation dependencies.

using Explore.Application.Contracts.Persistence;
using MediatR;

namespace Explore.Application.Features.Federation.Atproto.Requests.Commands;

public sealed record ImportAtprotoFederatedEventCommand(
    AtprotoJetstreamApplyRequest ApplyRequest) : IRequest<bool>;
