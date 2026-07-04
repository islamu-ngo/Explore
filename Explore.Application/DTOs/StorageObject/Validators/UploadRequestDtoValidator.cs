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
            .Must(StorageObjectMetadataValidation.NotContainControlCharacters).WithMessage("{PropertyName} must not contain control characters")
            .Must(StorageObjectMetadataValidation.NotContainPathSeparators).WithMessage("{PropertyName} must be a simple file name without path segments")
            .Must(StorageObjectMetadataValidation.NotBeDotSegment).WithMessage("{PropertyName} must be a simple file name")
            .Must(StorageObjectMetadataValidation.NotBeReservedFileName).WithMessage("{PropertyName} must not be a reserved file name");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(MaxContentTypeLength).WithMessage("{PropertyName} must not exceed 100 characters")
            .Must(StorageObjectMetadataValidation.NotContainControlCharacters).WithMessage("{PropertyName} must not contain control characters")
            .Must(StorageObjectMetadataValidation.BeValidRequiredContentType).WithMessage("{PropertyName} must be a valid MIME type");
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

}
