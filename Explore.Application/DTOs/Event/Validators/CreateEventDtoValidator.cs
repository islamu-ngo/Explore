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
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.");

            RuleFor(p => p.Description)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

            RuleFor(p => p.AudienceGenderId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MustAsync(async (id, cancellation) =>
                {
                    var audienceGenderExists = await _audienceGenderRepository.Exists(id);
                    return audienceGenderExists;
                }).WithMessage("{PropertyName} does not exist.");

            RuleFor(p => p.AudienceAgeId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MustAsync(async (id, cancellation) =>
                {
                    var audienceAgeExists = await _audienceAgeRepository.Exists(id);
                    return audienceAgeExists;
                }).WithMessage("{PropertyName} does not exist.");

            RuleFor(p => p.OrganizationId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MustAsync(async (id, cancellation) =>
                {
                    var organizationExists = await _organizationRepository.Exists(id);
                    return organizationExists;
                }).WithMessage("{PropertyName} does not exist.");

            RuleFor(p => p.AudienceAttendees)
                .GreaterThan(0).When(p => p.AudienceAttendees.HasValue)
                .WithMessage("{PropertyName} must be greater than 0.");

            RuleFor(p => p.Price)
                .GreaterThanOrEqualTo(0).WithMessage("{PropertyName} must be greater")
                .NotNull().WithMessage("{PropertyName} is required.");

            RuleFor(p => p.FeaturedImageId)
                .MustAsync(async (id, cancellation) =>
                {
                    if (!id.HasValue)
                        return true;
                    var storageObjectExists = await _storageObjectRepository.Exists(id.Value);
                    return storageObjectExists;
                }).WithMessage("{PropertyName} does not exist.");

            //TODO quand je change le Program pour mettre non nullable revient changer ici!
            //RuleFor(p => p.IsRegistrationRequired)
            //    .NotNull().WithMessage("{PropertyName} is required.");

            RuleFor(p => p.Country)
                .MaximumLength(50).When(p => !string.IsNullOrEmpty(p.Country))
                .WithMessage("{PropertyName} must not exceed 50 characters.");

            RuleFor(p => p.City)
                .MaximumLength(50).When(p => !string.IsNullOrEmpty(p.City))
                .WithMessage("{PropertyName} must not exceed 50 characters.");

            RuleFor(p => p.PostCode)
                .GreaterThan(0).When(p => p.PostCode.HasValue)
                .WithMessage("{PropertyName} must be greater than 0.");

            RuleFor(p => p.Address)
                .MaximumLength(100).When(p => !string.IsNullOrEmpty(p.Address))
                .WithMessage("{PropertyName} must not exceed 100 characters.");

            // Url can be very long, so we allow up to 4000 characters
            RuleFor(p => p.ProgramUrl)
                .MaximumLength(4000).When(p => !string.IsNullOrEmpty(p.ProgramUrl))
                .WithMessage("{PropertyName} must not exceed 4000 characters.");

            RuleFor(p => p.EventTypeId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MustAsync(async (id, cancellation) =>
                {
                    var eventTypeExists = await _eventTypeRepository.Exists(id);
                    return eventTypeExists;
                }).WithMessage("{PropertyName} does not exist.");
        }
    }
}
