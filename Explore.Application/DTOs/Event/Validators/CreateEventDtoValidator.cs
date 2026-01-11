using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators
{
    public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
    {
        private readonly IAudienceAgeRepository _audienceAgeRepository;
        private readonly IAudienceGenderRepository _audienceGenderRepository;
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IStorageObjectRepository _storageObjectRepository;

        public CreateEventDtoValidator(
            IAudienceAgeRepository audienceAgeRepository,
            IAudienceGenderRepository audienceGenderRepository,
            IEventTypeRepository eventTypeRepository,
            IOrganizationRepository organizationRepository,
            IStorageObjectRepository storageObjectRepository)
        {
            _audienceAgeRepository = audienceAgeRepository;
            _audienceGenderRepository = audienceGenderRepository;
            _eventTypeRepository = eventTypeRepository;
            _organizationRepository = organizationRepository;
            _storageObjectRepository = storageObjectRepository;

            RuleFor(p => p.Title)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");

            RuleFor(p => p.Description)
                .MaximumLength(5000)
                .When(p => !string.IsNullOrEmpty(p.Description))
                .WithMessage("{PropertyName} must not exceed 5000 characters.");

            RuleFor(p => p.Slug)
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

            RuleFor(p => p.EventTypeId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .MustAsync(async (id, cancellation) =>
                {
                    var exists = await _eventTypeRepository.Exists(id);
                    return exists;
                }).WithMessage("{PropertyName} does not exist.");

            RuleFor(p => p.AudienceGenderId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .MustAsync(async (id, cancellation) =>
                {
                    var audienceGenderExists = await _audienceGenderRepository.Exists(id);
                    return audienceGenderExists;
                }).WithMessage("{PropertyName} does not exist.");

            RuleFor(p => p.AudienceAgeId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .MustAsync(async (id, cancellation) =>
                {
                    var audienceAgeExists = await _audienceAgeRepository.Exists(id);
                    return audienceAgeExists;
                }).WithMessage("{PropertyName} does not exist.");

            // OrganizationId is optional - if provided, validate it exists
            RuleFor(p => p.OrganizationId)
                .MustAsync(async (id, cancellation) =>
                {
                    if (!id.HasValue) return true;
                    return await _organizationRepository.Exists(id.Value);
                })
                .When(p => p.OrganizationId.HasValue)
                .WithMessage("Organization does not exist.");

            RuleFor(p => p.Price)
                .GreaterThanOrEqualTo(0)
                .When(p => p.Price.HasValue)
                .WithMessage("{PropertyName} must be greater than or equal to 0.");

            RuleFor(p => p.CurrencyCode)
                .MaximumLength(3)
                .When(p => !string.IsNullOrEmpty(p.CurrencyCode))
                .WithMessage("{PropertyName} must be a valid 3-letter currency code.");

            RuleFor(p => p.FeaturedImageId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
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
                .MaximumLength(500)
                .When(p => !string.IsNullOrEmpty(p.ExternalRegistrationUrl))
                .WithMessage("{PropertyName} must not exceed 500 characters.");

            RuleFor(p => p.Timezone)
                .MaximumLength(100)
                .When(p => !string.IsNullOrEmpty(p.Timezone))
                .WithMessage("{PropertyName} must not exceed 100 characters.");

            RuleFor(p => p.EventUrl)
                .MaximumLength(500)
                .When(p => !string.IsNullOrEmpty(p.EventUrl))
                .WithMessage("{PropertyName} must not exceed 500 characters.");

            RuleFor(p => p.TenantId)
                .NotEmpty().WithMessage("{PropertyName} is required.");
        }
    }
}
