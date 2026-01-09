using Explore.Application.DTOs.EventRegistration;
using MediatR;
using System;

namespace Explore.Application.Features.EventRegistrations.Requests.Queries
{
    public class GetEventRegistrationDetailsRequest : IRequest<EventRegistrationDto>
    {
        public Guid Id { get; set; }
    }
}
