// ABOUTME: FluentValidation rules for local-first upload session reservation payloads.
// ABOUTME: Validates metadata, quota-safe byte counts, and idempotency before handlers mutate counters.

using Explore.Domain;
using FluentValidation;

namespace Explore.Application.DTOs.StorageObject.Validators;

public class CreateStorageUploadSessionDtoValidator : AbstractValidator<CreateStorageUploadSessionDto>
{
    private const int MaxFileNameLength = 500;
    private const int MaxContentTypeLength = 255;
    private const int MaxExtensionLength = 50;
    private const int MaxResourceKindLength = 100;
    private const int MaxIdempotencyKeyLength = 128;

    public CreateStorageUploadSessionDtoValidator()
    {
        RuleFor(x => x.ExpectedSizeBytes)
            .GreaterThan(0).WithMessage("{PropertyName} must be greater than zero");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(MaxContentTypeLength).WithMessage("{PropertyName} must not exceed 255 characters")
            .Must(NotContainControlCharacters).WithMessage("{PropertyName} must not contain control characters")
            .Must(ContainMediaTypeSeparator).WithMessage("{PropertyName} must be a valid media type");

        RuleFor(x => x.OriginalFileName)
            .MaximumLength(MaxFileNameLength).WithMessage("{PropertyName} must not exceed 500 characters")
            .Must(NotContainPathSeparators).WithMessage("{PropertyName} must not contain path separators");

        RuleFor(x => x.SafeDisplayName)
            .MaximumLength(MaxFileNameLength).WithMessage("{PropertyName} must not exceed 500 characters")
            .Must(NotContainPathSeparators).WithMessage("{PropertyName} must not contain path separators");

        RuleFor(x => x.Extension)
            .MaximumLength(MaxExtensionLength).WithMessage("{PropertyName} must not exceed 50 characters")
            .Must(NotContainPathSeparators).WithMessage("{PropertyName} must not contain path separators");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .Must(value => StorageObjectPurposes.All.Contains(value))
            .WithMessage("{PropertyName} must be a supported storage purpose");

        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .Must(value => StorageObjectVisibilities.All.Contains(value))
            .WithMessage("{PropertyName} must be a supported storage visibility");

        RuleFor(x => x.OwningResourceKind)
            .MaximumLength(MaxResourceKindLength).WithMessage("{PropertyName} must not exceed 100 characters")
            .NotEmpty().When(x => x.OwningResourceId.HasValue)
            .WithMessage("{PropertyName} is required when OwningResourceId is provided");

        RuleFor(x => x.OwningResourceId)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.OwningResourceKind))
            .WithMessage("{PropertyName} is required when OwningResourceKind is provided");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(MaxIdempotencyKeyLength).WithMessage("{PropertyName} must not exceed 128 characters")
            .Matches("^[A-Za-z0-9._:-]+$").WithMessage("{PropertyName} contains unsupported characters");
    }

    private static bool ContainMediaTypeSeparator(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.Contains('/', StringComparison.Ordinal);

    private static bool NotContainControlCharacters(string? value)
        => value is null || !value.Any(char.IsControl);

    private static bool NotContainPathSeparators(string? value)
        => value is null || (!value.Contains('/', StringComparison.Ordinal) && !value.Contains('\\', StringComparison.Ordinal));
}
