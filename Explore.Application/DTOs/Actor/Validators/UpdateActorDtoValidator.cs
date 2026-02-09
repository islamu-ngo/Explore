using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Actor.Validators;

public class UpdateActorDtoValidator : AbstractValidator<UpdateActorDto>
{
    private readonly IActorTypeRepository _actorTypeRepository;
    private readonly IDidCustodyTypeRepository _didCustodyTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IActorRepository _actorRepository;

    public UpdateActorDtoValidator(
        IActorTypeRepository actorTypeRepository,
        IDidCustodyTypeRepository didCustodyTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        IActorRepository actorRepository)
    {
        _actorTypeRepository = actorTypeRepository;
        _didCustodyTypeRepository = didCustodyTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _actorRepository = actorRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Actor ID is required");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(500).WithMessage("Display name cannot exceed 500 characters");

        RuleFor(x => x.ActorTypeId)
            .NotEmpty().WithMessage("Actor type is required")
            .MustAsync(async (actorTypeId, cancellation) =>
            {
                return await _actorTypeRepository.Exists(actorTypeId);
            }).WithMessage("Invalid actor type");

        RuleFor(x => x.DidCustodyTypeId)
            .MustAsync(async (didCustodyTypeId, cancellation) =>
            {
                if (!didCustodyTypeId.HasValue)
                    return true;

                return await _didCustodyTypeRepository.Exists(didCustodyTypeId.Value);
            })
            .When(x => x.DidCustodyTypeId.HasValue)
            .WithMessage("Invalid DID custody type");

        RuleFor(x => x.ProfilePictureId)
            .MustAsync(async (profilePictureId, cancellation) =>
            {
                if (!profilePictureId.HasValue)
                    return true;

                return await _storageObjectRepository.Exists(profilePictureId.Value);
            })
            .When(x => x.ProfilePictureId.HasValue)
            .WithMessage("Invalid profile picture");

        RuleFor(x => x.Did)
            .MaximumLength(500).WithMessage("DID cannot exceed 500 characters")
            .MustAsync(async (dto, did, cancellation) =>
            {
                if (string.IsNullOrWhiteSpace(did))
                    return true;

                var existingActor = await _actorRepository.GetActorByDid(did);
                return existingActor == null || existingActor.Id == dto.Id;
            })
            .When(x => !string.IsNullOrWhiteSpace(x.Did))
            .WithMessage("DID already exists for another actor");

        RuleFor(x => x.Handle)
            .MaximumLength(500).WithMessage("Handle cannot exceed 500 characters");

        RuleFor(x => x.PdsHost)
            .MaximumLength(500).WithMessage("PDS host cannot exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.ProfilePictureCid)
            .MaximumLength(500).WithMessage("Profile picture CID cannot exceed 500 characters");

        RuleFor(x => x.ProfilePictureUri)
            .MaximumLength(500).WithMessage("Profile picture URI cannot exceed 500 characters");
    }
}
