using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.StorageObject.Validators
{
    public class UpdateStorageObjectDtoValidator : AbstractValidator<UpdateStorageObjectDto>
    {
        private readonly IFileTypeRepository _fileTypeRepository;
        private readonly IActorRepository _actorRepository;

        public UpdateStorageObjectDtoValidator(
            IFileTypeRepository fileTypeRepository,
            IActorRepository actorRepository)
        {
            _fileTypeRepository = fileTypeRepository;
            _actorRepository = actorRepository;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("{PropertyName} is required");

            RuleFor(x => x.FileTypeId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(FileTypeExists)
                .WithMessage("{PropertyName} not found");

            RuleFor(x => x.Uri)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters");

            RuleFor(x => x.Extension)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters");

            RuleFor(x => x.Size)
                .GreaterThan(0).WithMessage("{PropertyName} must be greater than 0");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("{PropertyName} is required");

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
}
