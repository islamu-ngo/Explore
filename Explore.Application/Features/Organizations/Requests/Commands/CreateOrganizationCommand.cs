using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Organization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands
{
    public class CreateOrganizationCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateOrganizationDto OrganizationDto { get; set; }
        public string? UserId { get; set; }
    }
}
