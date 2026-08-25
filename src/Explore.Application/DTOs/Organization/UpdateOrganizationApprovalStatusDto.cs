using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Organization;

public sealed record UpdateOrganizationApprovalStatusDto
{
    public int ApprovalStatusId { get; init; }
}
