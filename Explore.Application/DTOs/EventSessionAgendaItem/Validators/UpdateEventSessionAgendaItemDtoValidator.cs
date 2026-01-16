using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventSessionAgendaItem.Validators
{
    public class UpdateEventSessionAgendaItemDtoValidator : AbstractValidator<UpdateEventSessionAgendaItemDto>
    {
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly ILocationRepository _locationRepository;

        public UpdateEventSessionAgendaItemDtoValidator(
            IEventSessionRepository eventSessionRepository,
            ILocationRepository locationRepository)
        {
            _eventSessionRepository = eventSessionRepository;
            _locationRepository = locationRepository;

            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.");

            RuleFor(p => p.EventSessionId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .MustAsync(async (id, cancellation) =>
                {
                    var exists = await _eventSessionRepository.Exists(id);
                    return exists;
                }).WithMessage("EventSession does not exist.");

            RuleFor(p => p.Title)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

            RuleFor(p => p.Description)
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

            RuleFor(p => p.StartTime)
                .NotEmpty().WithMessage("{PropertyName} is required.");

            RuleFor(p => p.EndTime)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .GreaterThan(p => p.StartTime).WithMessage("EndTime must be after StartTime.");

            RuleFor(p => p.LocationId)
                .MustAsync(async (id, cancellation) =>
                {
                    if (!id.HasValue) return true;
                    var exists = await _locationRepository.Exists(id.Value);
                    return exists;
                }).WithMessage("Location does not exist.");

            // TenantId is set by the handler from context, not by the client
            // No validation needed here
        }
    }
}
