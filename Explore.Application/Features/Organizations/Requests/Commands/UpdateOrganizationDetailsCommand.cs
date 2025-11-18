using System;
using Explore.Application.DTOs.Organization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands
{
    public class UpdateOrganizationDetailsCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public UpdateOrganizationDto OrganizationDto { get; set; }
    }
}
