// ABOUTME: Query request for the typed public-experience shell read model.
// ABOUTME: Resolves anonymous-safe tenant-local posture, catalog, primary organization, and footer projection.

using Explore.Application.DTOs.PublicExperience;
using MediatR;

namespace Explore.Application.Features.PublicExperience.Requests.Queries;

public class GetPublicExperienceShellQuery : IRequest<PublicExperienceShellDto>
{
}
