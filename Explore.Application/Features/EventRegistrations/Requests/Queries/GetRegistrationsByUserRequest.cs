using Explore.Application.DTOs.EventRegistration;
using MediatR;
using System;
using System.Collections.Generic;

namespace Explore.Application.Features.EventRegistrations.Requests.Queries
{
    public class GetRegistrationsByUserRequest : IRequest<List<EventRegistrationListDto>>
    {
        public Guid UserId { get; set; }
    }
}
