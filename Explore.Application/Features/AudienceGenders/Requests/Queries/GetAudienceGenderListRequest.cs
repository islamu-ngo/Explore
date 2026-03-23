// ABOUTME: MediatR query request for fetching the full list of audience gender options.
// ABOUTME: Returns IEnumerable<AudienceGenderDto>.
using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.AudienceGender;
using MediatR;

namespace Explore.Application.Features.AudienceGenders.Requests.Queries;

public class GetAudienceGenderListRequest : IRequest<List<AudienceGenderListDto>>
{
}
