using Explore.Application.DTOs.ProgramRegistration;
using MediatR;
using System;
using System.Collections.Generic;

namespace Explore.Application.Features.ProgramRegistration.Requests.Queries
{
    public class GetMyProgramRegistrationsRequest : IRequest<List<ProgramRegistrationListDto>>
    {
        public Guid UserId { get; set; }
    }
}
