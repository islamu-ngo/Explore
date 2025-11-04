using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Program;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Programs.Requests.Commands
{
    public class CreateProgramCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateProgramDto ProgramDto { get; set; }
    }
}
