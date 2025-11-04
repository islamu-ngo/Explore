using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.AudienceGender;
using MediatR;

namespace Explore.Application.Features.AudienceGenders.Requests.Queries
{
    public class GetAudienceGenderListRequest : IRequest<List<AudienceGenderListDto>>
    {
        public int Id { get; set; }
        public string FullName { get; set; }
    }
}
