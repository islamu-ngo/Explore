// ABOUTME: MediatR query for fetching all organizations a user belongs to.
// ABOUTME: Returns IEnumerable<OrganizationListDto>.
using Explore.Application.DTOs.Organization;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

/// <summary>
/// Request to get all organizations a user is a member of.
/// </summary>
public sealed record GetUserOrganizationsRequest(Guid UserId = default) : IRequest<List<OrganizationListDto>>;
