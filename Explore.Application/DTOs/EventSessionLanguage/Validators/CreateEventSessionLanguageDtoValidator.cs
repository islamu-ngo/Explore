using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventSessionLanguage.Validators
{
    public class CreateEventSessionLanguageDtoValidator : AbstractValidator<CreateEventSessionLanguageDto>
    {
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly ILanguageRepository _languageRepository;

        public CreateEventSessionLanguageDtoValidator(
            IEventSessionRepository eventSessionRepository,
            ILanguageRepository languageRepository)
        {
            _eventSessionRepository = eventSessionRepository;
            _languageRepository = languageRepository;

            RuleFor(x => x.EventSessionId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(EventSessionExists)
                .WithMessage("{PropertyName} not found");

            RuleFor(x => x.LanguageId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(LanguageExists)
                .WithMessage("{PropertyName} not found");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("{PropertyName} is required");
        }

        private async Task<bool> EventSessionExists(Guid eventSessionId, CancellationToken cancellationToken)
        {
            return await _eventSessionRepository.Exists(eventSessionId);
        }

        private async Task<bool> LanguageExists(int languageId, CancellationToken cancellationToken)
        {
            return await _languageRepository.Exists(languageId);
        }
    }
}
