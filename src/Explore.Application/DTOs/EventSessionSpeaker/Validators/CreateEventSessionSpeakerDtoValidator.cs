using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventSessionSpeaker.Validators;

public class CreateEventSessionSpeakerDtoValidator : AbstractValidator<CreateEventSessionSpeakerDto>
{
    private readonly IActorRepository _actorRepository;
    private readonly IEventSessionRepository _eventSessionRepository;

    public CreateEventSessionSpeakerDtoValidator(
        IActorRepository actorRepository,
        IEventSessionRepository eventSessionRepository)
    {
        _actorRepository = actorRepository;
        _eventSessionRepository = eventSessionRepository;

        RuleFor(p => p.ActorId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _actorRepository.Exists(id);
                return exists;
            }).WithMessage("Actor does not exist.");

        RuleFor(p => p.EventSessionId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _eventSessionRepository.Exists(id);
                return exists;
            }).WithMessage("EventSession does not exist.");

        // TenantId is set by the handler from context, not by the client
        // No validation needed here
    }
}
