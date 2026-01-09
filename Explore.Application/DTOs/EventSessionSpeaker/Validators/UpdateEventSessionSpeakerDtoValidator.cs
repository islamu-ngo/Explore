using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventSessionSpeaker.Validators
{
    public class UpdateEventSessionSpeakerDtoValidator : AbstractValidator<UpdateEventSessionSpeakerDto>
    {
        private readonly IActorRepository _actorRepository;
        private readonly IEventSessionRepository _eventSessionRepository;

        public UpdateEventSessionSpeakerDtoValidator(
            IActorRepository actorRepository,
            IEventSessionRepository eventSessionRepository)
        {
            _actorRepository = actorRepository;
            _eventSessionRepository = eventSessionRepository;

            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.");

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

            RuleFor(p => p.TenantId)
                .NotEmpty().WithMessage("{PropertyName} is required.");
        }
    }
}
