using Explore.Application.DTOs.OrganizationMember;
using MediatR;
using System;
using System.Collections.Generic;

namespace Explore.Application.Features.OrganizationMembers.Requests.Queries
{
    public class GetOrganizationMembersRequest : IRequest<List<OrganizationMemberDto>>
    {
        public Guid OrganizationId { get; set; }
    }
}
