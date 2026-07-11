// ABOUTME: FluentValidation rules for storage metadata creation.
// ABOUTME: Validates provider-neutral storage fields without trusting browser-provided filenames or states.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.DTOs.StorageObject.Validators;

public class CreateStorageObjectDtoValidator : AbstractValidator<CreateStorageObjectDto>
{
    private readonly IFileTypeRepository _fileTypeRepository;
    private readonly IActorRepository _actorRepository;

    public CreateStorageObjectDtoValidator(
        IFileTypeRepository fileTypeRepository,
        IActorRepository actorRepository)
    {
        _fileTypeRepository = fileTypeRepository;
        _actorRepository = actorRepository;

        RuleFor(x => x.FileTypeId)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MustAsync(FileTypeExists)
            .WithMessage("{PropertyName} not found");

        RuleFor(x => x.Uri)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(1000).WithMessage("{PropertyName} must not exceed 1000 characters")
            .Must(StorageObjectMetadataValidation.NotContainControlCharacters)
            .WithMessage("{PropertyName} must not contain control characters");

        RuleFor(x => x.ObjectKey)
            .MaximumLength(1024).WithMessage("{PropertyName} must not exceed 1024 characters")
            .Must(StorageObjectMetadataValidation.BeValidObjectKey)
            .WithMessage("{PropertyName} must be a relative provider object key without traversal segments");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .Must(value => StorageProviders.All.Contains(value))
            .WithMessage("{PropertyName} must be a supported storage provider");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters")
            .Must(StorageObjectMetadataValidation.NotContainControlCharacters)
            .WithMessage("{PropertyName} must not contain control characters")
            .Must(StorageObjectMetadataValidation.NotContainPathSeparators)
            .WithMessage("{PropertyName} must not contain path separators")
            .Must(StorageObjectMetadataValidation.NotBeDotSegment)
            .WithMessage("{PropertyName} must be a simple file name")
            .Must(StorageObjectMetadataValidation.NotBeReservedFileName)
            .WithMessage("{PropertyName} must not be a reserved file name");

        RuleFor(x => x.SafeDisplayName)
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters")
            .Must(StorageObjectMetadataValidation.NotContainControlCharacters)
            .WithMessage("{PropertyName} must not contain control characters")
            .Must(StorageObjectMetadataValidation.NotContainPathSeparators)
            .WithMessage("{PropertyName} must not contain path separators")
            .Must(StorageObjectMetadataValidation.NotBeDotSegment)
            .WithMessage("{PropertyName} must be a simple file name")
            .Must(StorageObjectMetadataValidation.NotBeReservedFileName)
            .WithMessage("{PropertyName} must not be a reserved file name");

        RuleFor(x => x.Extension)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters")
            .Must(StorageObjectMetadataValidation.NotContainControlCharacters)
            .WithMessage("{PropertyName} must not contain control characters")
            .Must(StorageObjectMetadataValidation.NotContainPathSeparators)
            .WithMessage("{PropertyName} must not contain path separators")
            .Must(StorageObjectMetadataValidation.NotBeDotSegment)
            .WithMessage("{PropertyName} must be a simple extension")
            .Must(StorageObjectMetadataValidation.BeValidExtension)
            .WithMessage("{PropertyName} contains unsupported characters");

        RuleFor(x => x.ContentType)
            .MaximumLength(255).WithMessage("{PropertyName} must not exceed 255 characters")
            .Must(StorageObjectMetadataValidation.NotContainControlCharacters)
            .WithMessage("{PropertyName} must not contain control characters")
            .Must(StorageObjectMetadataValidation.BeValidOptionalContentType)
            .WithMessage("{PropertyName} must be a valid MIME type");

        RuleFor(x => x.Sha256Checksum)
            .Must(StorageObjectMetadataValidation.BeValidSha256HexDigest)
            .When(x => !string.IsNullOrWhiteSpace(x.Sha256Checksum))
            .WithMessage("{PropertyName} must be a SHA-256 hex digest");

        RuleFor(x => x.Size)
            .GreaterThanOrEqualTo(0).WithMessage("{PropertyName} must be zero or greater");

        RuleFor(x => x.Visibility)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .Must(value => StorageObjectVisibilities.All.Contains(value))
            .WithMessage("{PropertyName} must be a supported storage visibility");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .Must(value => StorageObjectPurposes.All.Contains(value))
            .WithMessage("{PropertyName} must be a supported storage purpose");

        RuleFor(x => x.LifecycleState)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .Must(value => StorageObjectLifecycleStates.All.Contains(value))
            .WithMessage("{PropertyName} must be a supported lifecycle state");

        RuleFor(x => x.OwningResourceKind)
            .MaximumLength(100).WithMessage("{PropertyName} must not exceed 100 characters")
            .Must(StorageObjectMetadataValidation.NotContainControlCharacters)
            .WithMessage("{PropertyName} must not contain control characters")
            .Matches("^[A-Za-z0-9._:-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.OwningResourceKind))
            .WithMessage("{PropertyName} contains unsupported characters")
            .NotEmpty().When(x => x.OwningResourceId.HasValue)
            .WithMessage("{PropertyName} is required when OwningResourceId is provided");

        RuleFor(x => x.OwningResourceId)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.OwningResourceKind))
            .WithMessage("{PropertyName} is required when OwningResourceKind is provided");

        RuleFor(x => x.ActorId)
            .MustAsync(ActorExists)
            .When(x => x.ActorId.HasValue)
            .WithMessage("{PropertyName} not found");
    }

    private async Task<bool> FileTypeExists(int fileTypeId, CancellationToken cancellationToken)
    {
        return await _fileTypeRepository.Exists(fileTypeId);
    }

    private async Task<bool> ActorExists(Guid? actorId, CancellationToken cancellationToken)
    {
        if (!actorId.HasValue) return true;
        return await _actorRepository.Exists(actorId.Value);
    }
}
