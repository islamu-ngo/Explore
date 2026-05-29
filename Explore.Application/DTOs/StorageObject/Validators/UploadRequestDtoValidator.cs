// ABOUTME: FluentValidation rules for legacy direct presigned upload URL requests.
// ABOUTME: Rejects path-style filenames, control characters, and malformed or wildcard MIME types.

using System.Net.Http.Headers;
using FluentValidation;

namespace Explore.Application.DTOs.StorageObject.Validators;

public class UploadRequestDtoValidator : AbstractValidator<UploadRequestDto>
{
    private const int MaxFileNameLength = 500;
    private const int MaxContentTypeLength = 100;

    public UploadRequestDtoValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(MaxFileNameLength).WithMessage("{PropertyName} must not exceed 500 characters")
            .Must(NotContainControlCharacters).WithMessage("{PropertyName} must not contain control characters")
            .Must(NotContainPathSeparators).WithMessage("{PropertyName} must be a simple file name without path segments")
            .Must(NotBeDotSegment).WithMessage("{PropertyName} must be a simple file name");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(MaxContentTypeLength).WithMessage("{PropertyName} must not exceed 100 characters")
            .Must(NotContainControlCharacters).WithMessage("{PropertyName} must not contain control characters")
            .Must(BeValidContentType).WithMessage("{PropertyName} must be a valid MIME type");
    }

    public static string NormalizeFileName(string? fileName)
        => fileName?.Trim() ?? string.Empty;

    public static string NormalizeContentType(string? contentType)
    {
        var candidate = contentType?.Trim() ?? string.Empty;
        return MediaTypeHeaderValue.TryParse(candidate, out var mediaTypeHeader)
            ? mediaTypeHeader.ToString()
            : candidate;
    }

    private static bool BeValidContentType(string? contentType)
    {
        var candidate = contentType?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(candidate) &&
            MediaTypeHeaderValue.TryParse(candidate, out var mediaTypeHeader) &&
            !string.IsNullOrWhiteSpace(mediaTypeHeader.MediaType) &&
            mediaTypeHeader.MediaType.Contains('/', StringComparison.Ordinal) &&
            !mediaTypeHeader.MediaType.Contains('*', StringComparison.Ordinal);
    }

    private static bool NotContainControlCharacters(string? value)
        => value is null || !value.Any(char.IsControl);

    private static bool NotContainPathSeparators(string? value)
        => value is null ||
            (!value.Contains('/', StringComparison.Ordinal) &&
             !value.Contains('\\', StringComparison.Ordinal));

    private static bool NotBeDotSegment(string? value)
    {
        var candidate = value?.Trim();
        return candidate is not "." and not "..";
    }
}
