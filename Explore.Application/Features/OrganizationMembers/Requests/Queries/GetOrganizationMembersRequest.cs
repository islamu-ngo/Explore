using System;
using System.Collections.Generic;
using Explore.Application.DTOs.OrganizationMember;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Queries;

public class GetOrganizationMembersRequest : IRequest<List<OrganizationMemberDto>>
{
    public Guid OrganizationId { get; set; }
}
