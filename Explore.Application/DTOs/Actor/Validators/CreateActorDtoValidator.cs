using FluentValidation;
using Explore.Application.Contracts.Persistence;

namespace Explore.Application.DTOs.Actor.Validators;

public class CreateActorDtoValidator : AbstractValidator<CreateActorDto>
{
    private readonly IActorTypeRepository _actorTypeRepository;
    private readonly IDidCustodyTypeRepository _didCustodyTypeRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public CreateActorDtoValidator(
        IActorTypeRepository actorTypeRepository,
        IDidCustodyTypeRepository didCustodyTypeRepository,
        IStorageObjectRepository storageObjectRepository,
        ITenantRepository tenantRepository,
        IActorRepository actorRepository,
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository)
    {
        _actorTypeRepository = actorTypeRepository;
        _didCustodyTypeRepository = didCustodyTypeRepository;
        _storageObjectRepository = storageObjectRepository;
        _tenantRepository = tenantRepository;
        _actorRepository = actorRepository;
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;

        // ===== XOR Validation: Actor must be User OR Organization (exactly one) =====
        RuleFor(x => x)
            .Must(x => (x.UserId.HasValue && !x.OrganizationId.HasValue) || 
                       (!x.UserId.HasValue && x.OrganizationId.HasValue))
            .WithMessage("Actor must have exactly one of UserId or OrganizationId set (not both, not neither).");

        // UserId validation - must exist if provided
        RuleFor(x => x.UserId)
            .MustAsync(async (userId, cancellation) =>
            {
                if (!userId.HasValue) return true;
                return await _userRepository.Exists(userId.Value);
            })
            .When(x => x.UserId.HasValue)
            .WithMessage("User does not exist.");

        // OrganizationId validation - must exist if provided
        RuleFor(x => x.OrganizationId)
            .MustAsync(async (orgId, cancellation) =>
            {
                if (!orgId.HasValue) return true;
                return await _organizationRepository.Exists(orgId.Value);
            })
            .When(x => x.OrganizationId.HasValue)
            .WithMessage("Organization does not exist.");

        // Check that User doesn't already have an Actor
        RuleFor(x => x.UserId)
            .MustAsync(async (userId, cancellation) =>
            {
                if (!userId.HasValue) return true;
                var existingActor = await _actorRepository.GetActorByUserId(userId.Value);
                return existingActor == null;
            })
            .When(x => x.UserId.HasValue)
            .WithMessage("This user already has an associated actor.");

        // Check that Organization doesn't already have an Actor
        RuleFor(x => x.OrganizationId)
            .MustAsync(async (orgId, cancellation) =>
            {
                if (!orgId.HasValue) return true;
                var existingActor = await _actorRepository.GetActorByOrganizationId(orgId.Value);
                return existingActor == null;
            })
            .When(x => x.OrganizationId.HasValue)
            .WithMessage("This organization already has an associated actor.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(500).WithMessage("Display name cannot exceed 500 characters");

        RuleFor(x => x.ActorTypeId)
            .NotEmpty().WithMessage("Actor type is required")
            .MustAsync(async (actorTypeId, cancellation) =>
            {
                return await _actorTypeRepository.Exists(actorTypeId);
            }).WithMessage("Invalid actor type");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant is required")
            .MustAsync(async (tenantId, cancellation) =>
            {
                return await _tenantRepository.Exists(tenantId);
            }).WithMessage("Invalid tenant");

        RuleFor(x => x.DidCustodyTypeId)
            .MustAsync(async (didCustodyTypeId, cancellation) =>
            {
                if (!didCustodyTypeId.HasValue) return true;
                return await _didCustodyTypeRepository.Exists(didCustodyTypeId.Value);
            })
            .When(x => x.DidCustodyTypeId.HasValue)
            .WithMessage("Invalid DID custody type");

        RuleFor(x => x.ProfilePictureId)
            .MustAsync(async (profilePictureId, cancellation) =>
            {
                if (!profilePictureId.HasValue) return true;
                return await _storageObjectRepository.Exists(profilePictureId.Value);
            })
            .When(x => x.ProfilePictureId.HasValue)
            .WithMessage("Invalid profile picture");

        RuleFor(x => x.Did)
            .MaximumLength(500).WithMessage("DID cannot exceed 500 characters")
            .MustAsync(async (did, cancellation) =>
            {
                if (string.IsNullOrWhiteSpace(did)) return true;
                return !await _actorRepository.DidExists(did);
            })
            .When(x => !string.IsNullOrWhiteSpace(x.Did))
            .WithMessage("DID already exists");

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
