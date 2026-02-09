using Explore.Application.DTOs.Organization;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

/// <summary>
/// Request to get all organizations a user is a member of.
/// </summary>
public class GetUserOrganizationsRequest : IRequest<List<OrganizationListDto>>
{
    /// <summary>
    /// The user ID to get organizations for.
    /// </summary>
    public Guid UserId { get; set; }
}
