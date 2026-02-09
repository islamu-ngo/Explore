using System;
using Explore.Application.DTOs.TenantUser;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Requests.Queries;

public class GetTenantUserDetailsRequest : IRequest<TenantUserDto>
{
    public Guid Id { get; set; }
}
