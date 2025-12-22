using Explore.Application.DTOs.ProgramRegistration;
using MediatR;
using System;
using System.Collections.Generic;

namespace Explore.Application.Features.ProgramRegistration.Requests.Queries
{
    public class GetProgramRegistrationsRequest : IRequest<List<ProgramRegistrationListDto>>
    {
        public Guid ProgramId { get; set; }
    }
}
