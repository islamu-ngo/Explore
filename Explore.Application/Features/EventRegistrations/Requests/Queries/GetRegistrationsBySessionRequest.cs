using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventRegistration;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Queries;

public class GetRegistrationsBySessionRequest : IRequest<List<EventRegistrationListDto>>
{
    public Guid EventSessionId { get; set; }
}
