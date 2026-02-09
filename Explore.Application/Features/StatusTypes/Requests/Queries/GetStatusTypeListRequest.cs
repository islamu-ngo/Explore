using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.StatusType;
using MediatR;

namespace Explore.Application.Features.StatusTypes.Requests.Queries;

public class GetStatusTypeListRequest : IRequest<List<StatusTypeListDto>>
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
