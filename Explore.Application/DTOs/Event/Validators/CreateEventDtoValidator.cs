using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;

    public CreateEventDtoValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IOrganizationRepository organizationRepository,
        IGroupRepository groupRepository,
        IStorageObjectRepository storageObjectRepository)
    {
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _organizationRepository = organizationRepository;
        _groupRepository = groupRepository;
        _storageObjectRepository = storageObjectRepository;

        RuleFor(p => p.Title)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");

        RuleFor(p => p.Subtitle)
            .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");

        RuleFor(p => p.Description)
            .MaximumLength(5000)
            .When(p => !string.IsNullOrEmpty(p.Description))
            .WithMessage("{PropertyName} must not exceed 5000 characters.");

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

        // OrganizationId is optional - if provided, validate it exists
        RuleFor(p => p.OrganizationId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _organizationRepository.Exists(id.Value);
            })
            .When(p => p.OrganizationId.HasValue)
            .WithMessage("Organization does not exist.");

        // GroupId is optional - if provided, validate it exists
        RuleFor(p => p.GroupId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _groupRepository.Exists(id.Value);
            })
            .When(p => p.GroupId.HasValue)
            .WithMessage("Group does not exist.");

        RuleFor(p => p)
            .Must(p => !(p.OrganizationId.HasValue && p.GroupId.HasValue))
            .WithMessage("OrganizationId and GroupId cannot both be provided.");

        RuleFor(p => p.Price)
            .GreaterThanOrEqualTo(0)
            .When(p => p.Price.HasValue)
            .WithMessage("{PropertyName} must be greater than or equal to 0.");

        RuleFor(p => p.CurrencyCode)
            .MaximumLength(3)
            .When(p => !string.IsNullOrEmpty(p.CurrencyCode))
            .WithMessage("{PropertyName} must be a valid 3-letter currency code.");

        // FeaturedImageId is optional - only validate if provided
        RuleFor(p => p.FeaturedImageId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var storageObjectExists = await _storageObjectRepository.Exists(id.Value);
                return storageObjectExists;
            })
            .When(p => p.FeaturedImageId.HasValue)
            .WithMessage("{PropertyName} does not exist.");

        // EventStatusId defaults to 1 (Draft) - no validation needed for existence
        // VisibilityTypeId defaults to 1 (Public) - no validation needed for existence
        // EventFormatId defaults to 1 (In-Person) - no validation needed for existence

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

        // TenantId is set by the handler from context, not by the client
        // No validation needed here
    }
}
