using System;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

public class UpdateEventDtoValidator : AbstractValidator<UpdateEventDto>
{
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IEventRegistrationPolicyRepository _eventRegistrationPolicyRepository;

    public UpdateEventDtoValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IActorRepository actorRepository,
        IStorageObjectRepository storageObjectRepository,
        IEventSeriesRepository eventSeriesRepository,
        IEventRegistrationPolicyRepository eventRegistrationPolicyRepository)
    {
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _actorRepository = actorRepository;
        _storageObjectRepository = storageObjectRepository;
        _eventSeriesRepository = eventSeriesRepository;
        _eventRegistrationPolicyRepository = eventRegistrationPolicyRepository;

        RuleFor(p => p.Id)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull();

        RuleFor(p => p.Title)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Subtitle)
            .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");

        RuleFor(p => p.Description)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Slug)
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.EventTypeId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var exists = await _eventTypeRepository.Exists(id.Value);
                return exists;
            })
            .When(p => p.EventTypeId.HasValue)
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.AudienceGenderId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var audienceGenderExists = await _audienceGenderRepository.Exists(id.Value);
                return audienceGenderExists;
            })
            .When(p => p.AudienceGenderId.HasValue)
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.AudienceAgeId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var audienceAgeExists = await _audienceAgeRepository.Exists(id.Value);
                return audienceAgeExists;
            })
            .When(p => p.AudienceAgeId.HasValue)
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.ActorId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MustAsync(async (id, cancellation) =>
            {
                var actorExists = await _actorRepository.Exists(id);
                return actorExists;
            }).WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.Price)
            .GreaterThanOrEqualTo(0).When(p => p.Price.HasValue)
            .WithMessage("{PropertyName} must be greater than or equal to 0.");

        RuleFor(p => p.CurrencyCode)
            .MaximumLength(3).When(p => !string.IsNullOrEmpty(p.CurrencyCode))
            .WithMessage("{PropertyName} must be a valid 3-letter currency code.");

        RuleFor(p => p.FeaturedImageId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MustAsync(async (id, cancellation) =>
            {
                var storageObjectExists = await _storageObjectRepository.Exists(id);
                return storageObjectExists;
            }).WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.EventStatusId)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(p => p.VisibilityTypeId)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(p => p.EventFormatId)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(p => p.ExternalRegistrationUrl)
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.ExternalRegistrationUrl))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Timezone)
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.Timezone))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.EventUrl)
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.EventUrl))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        // EventSeriesId is optional - if provided, validate it exists
        RuleFor(p => p.EventSeriesId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _eventSeriesRepository.Exists(id.Value);
            })
            .When(p => p.EventSeriesId.HasValue)
            .WithMessage("Event series does not exist.");

        // RegistrationPolicyId is optional - if provided, validate it exists
        RuleFor(p => p.RegistrationPolicyId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _eventRegistrationPolicyRepository.Exists(id.Value);
            })
            .When(p => p.RegistrationPolicyId.HasValue)
            .WithMessage("Registration policy does not exist.");

        RuleFor(p => p.SeriesOrder)
            .GreaterThanOrEqualTo(0)
            .When(p => p.SeriesOrder.HasValue)
            .WithMessage("{PropertyName} must be non-negative.");
    }
}
