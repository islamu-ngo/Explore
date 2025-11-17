using Explore.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Application.DTOs.Organization;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Queries
{
    public class GetOrganizationListRequest : IRequest<List<OrganizationListDto>>
    {
        public Guid Id { get; set; }
    }
}
