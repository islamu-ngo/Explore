using Explore.Application.DTOs.EventRegistration;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.EventRegistrations.Requests.Queries
{
    public class GetEventRegistrationListRequest : IRequest<List<EventRegistrationListDto>>
    {
    }
}
