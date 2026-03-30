using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.DTOs.EventSession.Validators;

public class CreateEventSessionDtoValidator : AbstractValidator<CreateEventSessionDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly IEventSessionTemplateRepository _eventSessionTemplateRepository;

    public CreateEventSessionDtoValidator(
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        IEventSessionTemplateRepository eventSessionTemplateRepository)
    {
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _registrationModeRepository = registrationModeRepository;
        _eventSessionTemplateRepository = eventSessionTemplateRepository;

        RuleFor(p => p.EventId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _eventRepository.Exists(id);
                return exists;
            }).WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.StartTime)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(p => p.EndTime)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .GreaterThan(p => p.StartTime).WithMessage("{PropertyName} must be after StartTime.");

        RuleFor(p => p.LocationId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var exists = await _locationRepository.Exists(id.Value);
                return exists;
            }).WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.Title)
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.Title))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Description)
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.Description))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Slug)
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.Slug))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.MaxAudienceAttendees)
            .GreaterThan(0).When(p => p.MaxAudienceAttendees.HasValue)
            .WithMessage("{PropertyName} must be greater than 0.");

        RuleFor(p => p.RegistrationModeId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var exists = await _registrationModeRepository.Exists(id.Value);
                return exists;
            }).WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.Price)
            .GreaterThanOrEqualTo(0).When(p => p.Price.HasValue)
            .WithMessage("{PropertyName} must be greater than or equal to 0.");

        RuleFor(p => p.CurrencyCode)
            .MaximumLength(3).When(p => !string.IsNullOrWhiteSpace(p.CurrencyCode))
            .WithMessage("{PropertyName} must be a valid 3-letter currency code.");

        RuleFor(p => p)
            .Must(p =>
            {
                if (p.IslamicAspect == null)
                {
                    return true;
                }

                if (p.IslamicAspect.StartTimeType == SessionStartTimeType.Fixed)
                {
                    return true;
                }

                return p.IslamicAspect.ReferencePrayer.HasValue
                    && p.IslamicAspect.OffsetMinutes.HasValue
                    && p.LocationId.HasValue;
            })
            .WithMessage("Islamic session scheduling requires LocationId, ReferencePrayer, and OffsetMinutes when StartTimeType is RelativeToPrayer.");

        RuleFor(p => p.SessionTemplateId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _eventSessionTemplateRepository.Exists(id.Value);
            })
            .When(p => p.SessionTemplateId.HasValue)
            .WithMessage("Event session template does not exist.");

        // TenantId is set by the handler from ITenantContext, not by the client
        // No validation needed here
    }
}
