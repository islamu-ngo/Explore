using Explore.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Application.DTOs.Organization;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Queries
{
    public class GetOrganizationDetailsRequest : IRequest<OrganizationDto>
    {
        public Guid Id { get; set; }
    }
}
