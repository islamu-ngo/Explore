// ABOUTME: MediatR command for deleting a tenant navigation link by ID.
// ABOUTME: Carries the target nav link ID.
using System;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Commands.DeleteTenantNavLink;

/// <summary>
/// Command to delete a tenant navigation link.
/// Returns a boolean indicating success or failure.
/// </summary>
public sealed record DeleteTenantNavLinkCommand(Guid Id = default) : IRequest<BaseCommandResponse<bool>>;
