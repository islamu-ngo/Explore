// ABOUTME: MediatR query request for fetching full tenant details by ID.
// ABOUTME: Returns TenantDto.
using Explore.Application.DTOs.Tenant;
using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Queries;

public sealed record GetTenantDetailsRequest(Guid Id = default) : IRequest<TenantDto>;
