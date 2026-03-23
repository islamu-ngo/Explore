// ABOUTME: MediatR query request for fetching full organization details.
// ABOUTME: Returns OrganizationDto.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Application.DTOs.Organization;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Queries;

public class GetOrganizationDetailsRequest : IRequest<OrganizationDto>
{
    public Guid Id { get; set; }
}
