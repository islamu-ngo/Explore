// ABOUTME: MediatR query that resolves persisted administrative authority for a specific user.
// ABOUTME: Supplies instance, tenant, organization, and group scopes to trusted BFF consumers.

using Explore.Application.DTOs.User;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

/// <summary>
/// Query to resolve the admin authority of a user across all hierarchy levels.
/// Returns <see cref="AdminAuthorityDto"/> with instance, tenant, organization, and group admin status.
/// </summary>
public sealed record GetAdminAuthorityRequest(Guid UserId = default) : IRequest<AdminAuthorityDto>;
