using System;
using System.Collections.Generic;
using System.Linq;
using Explore.Domain;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

/// <summary>
/// Validator for CreateEventWithSessionsDto.
/// Validates both event fields and embedded sessions.
/// </summary>
public class CreateEventWithSessionsDtoValidator : AbstractValidator<CreateEventWithSessionsDto>
{
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly ILanguageRepository _languageRepository;

    public CreateEventWithSessionsDtoValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IOrganizationRepository organizationRepository,
        IGroupRepository groupRepository,
        IStorageObjectRepository storageObjectRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        ILanguageRepository languageRepository)
    {
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _organizationRepository = organizationRepository;
        _groupRepository = groupRepository;
        _storageObjectRepository = storageObjectRepository;
        _locationRepository = locationRepository;
        _registrationModeRepository = registrationModeRepository;
        _languageRepository = languageRepository;

        // ===== EVENT VALIDATION RULES =====

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
            }).WithMessage("Event type does not exist.");

        RuleFor(p => p.AudienceGenderId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceGenderRepository.Exists(id);
                return exists;
            }).WithMessage("Audience gender does not exist.");

        RuleFor(p => p.AudienceAgeId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, cancellation) =>
            {
                var exists = await _audienceAgeRepository.Exists(id);
                return exists;
            }).WithMessage("Audience age does not exist.");

        RuleFor(p => p.OrganizationId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _organizationRepository.Exists(id.Value);
            })
            .When(p => p.OrganizationId.HasValue)
            .WithMessage("Organization does not exist.");

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

        RuleFor(p => p.FeaturedImageId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _storageObjectRepository.Exists(id.Value);
            })
            .When(p => p.FeaturedImageId.HasValue)
            .WithMessage("Featured image does not exist.");

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

        // ===== SESSIONS VALIDATION RULES =====

        RuleFor(p => p.Sessions)
            .NotNull().WithMessage("Sessions are required.")
            .NotEmpty().WithMessage("At least one session is required.");

        RuleForEach(p => p.Sessions)
            .ChildRules(session =>
            {
                session.RuleFor(s => s.Title)
                    .MaximumLength(500)
                    .When(s => !string.IsNullOrEmpty(s.Title))
                    .WithMessage("Session title must not exceed 500 characters.");

                session.RuleFor(s => s.Description)
                    .MaximumLength(5000)
                    .When(s => !string.IsNullOrEmpty(s.Description))
                    .WithMessage("Session description must not exceed 5000 characters.");

                session.RuleFor(s => s.StartTime)
                    .NotEmpty().WithMessage("Session start time is required.");

                session.RuleFor(s => s.EndTime)
                    .NotEmpty().WithMessage("Session end time is required.")
                    .GreaterThan(s => s.StartTime)
                    .WithMessage("Session end time must be after start time.");

                session.RuleFor(s => s.MaxAudienceAttendees)
                    .GreaterThan(0)
                    .When(s => s.MaxAudienceAttendees.HasValue)
                    .WithMessage("Maximum audience attendees must be greater than 0.");

                session.RuleFor(s => s.Price)
                    .GreaterThanOrEqualTo(0)
                    .When(s => s.Price.HasValue)
                    .WithMessage("Session price must be greater than or equal to 0.");

                session.RuleFor(s => s.CurrencyCode)
                    .MaximumLength(3)
                    .When(s => !string.IsNullOrWhiteSpace(s.CurrencyCode))
                    .WithMessage("Session currency code must be a valid 3-letter code.");
            });

        RuleFor(p => p.Sessions)
            .Must(sessions =>
            {
                foreach (var session in sessions)
                {
                    if (session.IslamicAspect == null)
                    {
                        continue;
                    }

                    if (session.IslamicAspect.StartTimeType == SessionStartTimeType.Fixed)
                    {
                        continue;
                    }

                    if (!session.LocationId.HasValue
                        || !session.IslamicAspect.ReferencePrayer.HasValue
                        || !session.IslamicAspect.OffsetMinutes.HasValue)
                    {
                        return false;
                    }
                }

                return true;
            })
            .WithMessage("Islamic session scheduling requires LocationId, ReferencePrayer, and OffsetMinutes when StartTimeType is RelativeToPrayer.");

        // Validate LocationId exists for each session that has one
        RuleFor(p => p.Sessions)
            .MustAsync(async (sessions, cancellation) =>
            {
                foreach (var session in sessions.Where(s => s.LocationId.HasValue))
                {
                    var exists = await _locationRepository.Exists(session.LocationId!.Value);
                    if (!exists) return false;
                }
                return true;
            })
            .WithMessage("One or more session locations do not exist.");

        // Validate RegistrationModeId exists for each session that has one
        RuleFor(p => p.Sessions)
            .MustAsync(async (sessions, cancellation) =>
            {
                foreach (var session in sessions.Where(s => s.RegistrationModeId.HasValue))
                {
                    var exists = await _registrationModeRepository.Exists(session.RegistrationModeId!.Value);
                    if (!exists) return false;
                }
                return true;
            })
            .WithMessage("One or more session registration modes do not exist.");

        // Validate LanguageIds exist for each session
        RuleFor(p => p.Sessions)
            .MustAsync(async (sessions, cancellation) =>
            {
                foreach (var session in sessions)
                {
                    foreach (var languageId in session.LanguageIds)
                    {
                        var exists = await _languageRepository.Exists(languageId);
                        if (!exists) return false;
                    }
                }
                return true;
            })
            .WithMessage("One or more session languages do not exist.");
    }
}
