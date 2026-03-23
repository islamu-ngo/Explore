// ABOUTME: MediatR query request for fetching all tenant settings records.
// ABOUTME: Returns IEnumerable<TenantSettingsDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.TenantSettings;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Requests.Queries;

public class GetTenantSettingsListRequest : IRequest<List<TenantSettingsListDto>>
{
}
