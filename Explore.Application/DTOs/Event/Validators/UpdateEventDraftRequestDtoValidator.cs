// ABOUTME: Validates scalar event-draft update requests without accepting lifecycle or program projection fields.
// ABOUTME: Keeps public draft update validation narrower than the legacy internal UpdateEventDto validator.

using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

public sealed class UpdateEventDraftRequestDtoValidator : AbstractValidator<UpdateEventDraftRequestDto>
{
    public UpdateEventDraftRequestDtoValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IVisibilityTypeRepository visibilityTypeRepository,
        IEventFormatRepository eventFormatRepository,
        IStorageObjectRepository storageObjectRepository,
        IEventSeriesRepository eventSeriesRepository,
        IEventRegistrationPolicyRepository eventRegistrationPolicyRepository)
    {
        RuleFor(request => request.Title)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(request => request.Subtitle)
            .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");

        RuleFor(request => request.Description)
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(request => request.Slug)
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(request => request.EventTypeId)
            .MustAsync(async (id, _) => !id.HasValue || await eventTypeRepository.Exists(id.Value))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(request => request.AudienceGenderId)
            .MustAsync(async (id, _) => !id.HasValue || await audienceGenderRepository.Exists(id.Value))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(request => request.AudienceAgeId)
            .MustAsync(async (id, _) => !id.HasValue || await audienceAgeRepository.Exists(id.Value))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(request => request.VisibilityTypeId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, _) => await visibilityTypeRepository.Exists(id))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(request => request.EventFormatId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, _) => await eventFormatRepository.Exists(id))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(request => request.Price)
            .GreaterThanOrEqualTo(0).When(request => request.Price.HasValue)
            .WithMessage("{PropertyName} must be greater than or equal to 0.");

        RuleFor(request => request.CurrencyCode)
            .MaximumLength(3).When(request => !string.IsNullOrEmpty(request.CurrencyCode))
            .WithMessage("{PropertyName} must be a valid 3-letter currency code.");

        RuleFor(request => request.FeaturedImageId)
            .MustAsync(async (id, _) => !id.HasValue || await storageObjectRepository.Exists(id.Value))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(request => request.BackgroundImageId)
            .MustAsync(async (id, _) => !id.HasValue || await storageObjectRepository.Exists(id.Value))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(request => request.ExternalRegistrationUrl)
            .MaximumLength(500).When(request => !string.IsNullOrEmpty(request.ExternalRegistrationUrl))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(request => request.Timezone)
            .MaximumLength(500).When(request => !string.IsNullOrEmpty(request.Timezone))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(request => request.EventTimeZoneId)
            .MaximumLength(500).When(request => !string.IsNullOrEmpty(request.EventTimeZoneId))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(request => request.EventUrl)
            .MaximumLength(500).When(request => !string.IsNullOrEmpty(request.EventUrl))
            .WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(request => request.EventSeriesId)
            .MustAsync(async (id, _) => !id.HasValue || await eventSeriesRepository.Exists(id.Value))
            .WithMessage("Event series does not exist.");

        RuleFor(request => request.RegistrationPolicyId)
            .MustAsync(async (id, _) => !id.HasValue || await eventRegistrationPolicyRepository.Exists(id.Value))
            .WithMessage("Registration policy does not exist.");

        RuleFor(request => request.SeriesOrder)
            .GreaterThanOrEqualTo(0)
            .When(request => request.SeriesOrder.HasValue)
            .WithMessage("{PropertyName} must be non-negative.");
    }
}
