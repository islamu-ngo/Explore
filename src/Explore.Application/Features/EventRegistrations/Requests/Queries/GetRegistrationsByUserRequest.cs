// ABOUTME: MediatR query for fetching all registrations for a user.
// ABOUTME: Returns IEnumerable<EventRegistrationDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventRegistration;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Queries;

public class GetRegistrationsByUserRequest : IRequest<List<EventRegistrationListDto>>
{
    public Guid UserId { get; set; }
}
