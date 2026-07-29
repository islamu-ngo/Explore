// ABOUTME: MediatR query for one canonical or tenant-contextual public Actor profile.
// ABOUTME: Carries optional tenant context while returning null for unavailable profiles.
using Explore.Application.DTOs.Actor;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Queries;

public class GetActorDetailsRequest : IRequest<ActorDto?>
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
}
