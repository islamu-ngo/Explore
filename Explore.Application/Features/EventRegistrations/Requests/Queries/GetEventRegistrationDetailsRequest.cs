// ABOUTME: MediatR query request for fetching a single registration by ID.
// ABOUTME: Returns EventRegistrationDto.
using System;
using Explore.Application.DTOs.EventRegistration;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Queries;

public class GetEventRegistrationDetailsRequest : IRequest<EventRegistrationDto>
{
    public Guid Id { get; set; }
}
