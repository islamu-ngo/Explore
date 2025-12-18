using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Organization;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Commands
{
    public class UpdateOrganizationCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public UpdateOrganizationApprovalStatusDto OrganizationApprovalStatusDto { get; set; }
    }
}
