// ABOUTME: Query request for the bounded tenant-local public home discovery payload.
// ABOUTME: Carries only a stable area ID and area/online/all mode, never a browser origin.

using Explore.Application.DTOs.PublicExperience;
using MediatR;

namespace Explore.Application.Features.PublicExperience.Requests.Queries;

public sealed record GetHomeDiscoveryQuery(
    Guid? AreaId = null,
    string? Mode = null) : IRequest<HomeDiscoveryDto>;
