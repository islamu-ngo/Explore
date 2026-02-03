// ABOUTME: FluentValidation validator for CreateUpdateIslamicAspectDto.
// ABOUTME: Validates foreign keys against lookup repositories.

namespace Explore.Application.DTOs.EventAspects.Validators;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using FluentValidation;

/// <summary>
/// Validator for Islamic aspect create/update operations.
/// </summary>
public class CreateUpdateIslamicAspectDtoValidator : AbstractValidator<CreateUpdateIslamicAspectDto>
{
    private readonly IMadhabRepository _madhabRepository;
    private readonly ILanguageRepository _languageRepository;

    public CreateUpdateIslamicAspectDtoValidator(
        IMadhabRepository madhabRepository,
        ILanguageRepository languageRepository)
    {
        _madhabRepository = madhabRepository;
        _languageRepository = languageRepository;

        // Madhab validation (optional)
        RuleFor(x => x.MadhabId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _madhabRepository.Exists(id.Value);
            })
            .When(x => x.MadhabId.HasValue)
            .WithMessage("Madhab not found.");

        // Prayer time offset validation
        RuleFor(x => x.PrayerTimeOffset)
            .InclusiveBetween(-180, 180)
            .When(x => x.PrayerTimeOffset.HasValue)
            .WithMessage("Prayer time offset must be between -180 and 180 minutes.");

        // Prayer time offset requires reference prayer
        RuleFor(x => x.PrayerTimeOffset)
            .Null()
            .When(x => !x.ReferencePrayer.HasValue)
            .WithMessage("Prayer time offset requires a reference prayer to be set.");

        // Gender mode validation
        RuleFor(x => x.GenderMode)
            .IsInEnum()
            .WithMessage("Invalid gender segregation mode.");

        // Primary language validation (optional)
        RuleFor(x => x.PrimaryLanguageId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                return await _languageRepository.Exists(id.Value);
            })
            .When(x => x.PrimaryLanguageId.HasValue)
            .WithMessage("Primary language not found.");
    }
}
