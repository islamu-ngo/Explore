// ABOUTME: MediatR query request for fetching a single tenant settings record by ID.
// ABOUTME: Returns TenantSettingsDto.
using Explore.Application.DTOs.TenantSettings;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Requests.Queries;

public class GetTenantSettingsDetailsRequest : IRequest<TenantSettingsDto>
{
    public Guid Id { get; set; }
}
