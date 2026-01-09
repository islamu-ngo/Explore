using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventSessionLanguage.Validators
{
    public class UpdateEventSessionLanguageDtoValidator : AbstractValidator<UpdateEventSessionLanguageDto>
    {
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly ILanguageRepository _languageRepository;

        public UpdateEventSessionLanguageDtoValidator(
            IEventSessionRepository eventSessionRepository,
            ILanguageRepository languageRepository)
        {
            _eventSessionRepository = eventSessionRepository;
            _languageRepository = languageRepository;

            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.");

            RuleFor(p => p.EventSessionId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .MustAsync(async (id, cancellation) =>
                {
                    var exists = await _eventSessionRepository.Exists(id);
                    return exists;
                }).WithMessage("EventSession does not exist.");

            RuleFor(p => p.LanguageId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .MustAsync(async (id, cancellation) =>
                {
                    var exists = await _languageRepository.Exists(id);
                    return exists;
                }).WithMessage("Language does not exist.");

            RuleFor(p => p.TenantId)
                .NotEmpty().WithMessage("{PropertyName} is required.");
        }
    }
}
