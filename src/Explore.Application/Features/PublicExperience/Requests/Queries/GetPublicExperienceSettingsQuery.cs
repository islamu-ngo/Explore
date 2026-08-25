// ABOUTME: Query request for resolving effective public experience settings for the current tenant context.
// ABOUTME: Used by startup/home routing and white-label UI components.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.PublicExperience.Requests.Queries;

public sealed record GetPublicExperienceSettingsQuery : IRequest<PublicExperienceSettingsDto>
{
}
