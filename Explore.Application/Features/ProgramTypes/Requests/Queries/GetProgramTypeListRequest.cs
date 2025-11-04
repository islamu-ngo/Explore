using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.ProgramType;
using MediatR;

namespace Explore.Application.Features.ProgramTypes.Requests.Queries
{
    public class GetProgramTypeListRequest : IRequest<List<ProgramTypeListDto>>
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string? Description { get; set; }
    }
}
