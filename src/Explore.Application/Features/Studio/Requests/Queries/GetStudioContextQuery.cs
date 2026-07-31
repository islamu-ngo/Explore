// ABOUTME: Requests private actor-scoped Studio navigation context for the current authenticated user.
// ABOUTME: Carries only an optional actor hint that the handler validates against server-side authority.

using Explore.Application.DTOs.Studio;
using MediatR;

namespace Explore.Application.Features.Studio.Requests.Queries;

public sealed record GetStudioContextQuery(Guid? ActorId = null) : IRequest<StudioContextDto>;
