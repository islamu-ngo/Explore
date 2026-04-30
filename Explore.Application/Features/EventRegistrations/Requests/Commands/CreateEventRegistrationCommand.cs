// ABOUTME: MediatR command for registering a user for an event.
// ABOUTME: Carries the CreateEventRegistrationDto payload.
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

public class CreateEventRegistrationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateEventRegistrationDto EventRegistrationDto { get; set; }
}
