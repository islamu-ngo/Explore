using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventSessionLanguage.Validators;

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

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("{PropertyName} is required");

        RuleFor(x => x.EventSessionId)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MustAsync(EventSessionExists)
            .WithMessage("{PropertyName} not found");

        RuleFor(x => x.LanguageId)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MustAsync(LanguageExists)
            .WithMessage("{PropertyName} not found");
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
