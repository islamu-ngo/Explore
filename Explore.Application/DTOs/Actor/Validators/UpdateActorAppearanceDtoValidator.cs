// ABOUTME: Validates the UpdateActorAppearanceDto for targeted appearance updates.
// ABOUTME: Checks max-length, hex-color format, valid effect values, and FK existence.

using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Actor.Validators;

public class UpdateActorAppearanceDtoValidator : AbstractValidator<UpdateActorAppearanceDto>
{
    private static readonly string[] ValidEffects = ["SoftOverlay", "StrongOverlay", "Blur", "None"];

    public UpdateActorAppearanceDtoValidator(IStorageObjectRepository storageObjectRepository)
    {
        RuleFor(x => x.BackgroundColor)
            .MaximumLength(50).WithMessage("Background color cannot exceed 50 characters")
            .Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")
            .When(x => !string.IsNullOrEmpty(x.BackgroundColor))
            .WithMessage("Background color must be a valid hex color (e.g., #1a2b3c)");

        RuleFor(x => x.BackgroundEffect)
            .MaximumLength(50).WithMessage("Background effect cannot exceed 50 characters")
            .Must(effect => ValidEffects.Contains(effect))
            .When(x => !string.IsNullOrEmpty(x.BackgroundEffect))
            .WithMessage($"Background effect must be one of: {string.Join(", ", ValidEffects)}");

        RuleFor(x => x.BannerColor)
            .MaximumLength(50).WithMessage("Banner color cannot exceed 50 characters")
            .Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")
            .When(x => !string.IsNullOrEmpty(x.BannerColor))
            .WithMessage("Banner color must be a valid hex color (e.g., #1a2b3c)");

        RuleFor(x => x.BannerPictureId)
            .MustAsync(async (bannerPictureId, cancellation) =>
            {
                if (!bannerPictureId.HasValue)
                    return true;

                return await storageObjectRepository.Exists(bannerPictureId.Value);
            })
            .When(x => x.BannerPictureId.HasValue)
            .WithMessage("Invalid banner picture");

        RuleFor(x => x.BackgroundImageId)
            .MustAsync(async (backgroundImageId, cancellation) =>
            {
                if (!backgroundImageId.HasValue)
                    return true;

                return await storageObjectRepository.Exists(backgroundImageId.Value);
            })
            .When(x => x.BackgroundImageId.HasValue)
            .WithMessage("Invalid background image");
    }
}
