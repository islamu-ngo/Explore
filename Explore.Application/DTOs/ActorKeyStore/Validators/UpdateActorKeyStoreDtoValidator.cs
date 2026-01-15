using FluentValidation;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ActorKeyStore;
using System;

namespace Explore.Application.DTOs.ActorKeyStore.Validators
{
    public class UpdateActorKeyStoreDtoValidator : AbstractValidator<UpdateActorKeyStoreDto>
    {
        private readonly IActorRepository _actorRepository;

        public UpdateActorKeyStoreDtoValidator(IActorRepository actorRepository)
        {
            _actorRepository = actorRepository;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Actor Key Store ID is required");

            RuleFor(x => x.ActorId)
                .NotEmpty().WithMessage("Actor ID is required")
                .MustAsync(ActorExists)
                .WithMessage("Actor does not exist");

            // TenantId is set by the handler from context, not by the client
            // No validation needed here

            RuleFor(x => x.KeyPurpose)
                .NotEmpty().WithMessage("Key purpose is required")
                //.MustBeOneOf("signing", "rotation", "encryption")
                .WithMessage("Key purpose must be signing, rotation, or encryption");

            RuleFor(x => x.PrivateKeyEncrypted)
                .NotEmpty().WithMessage("Private key (encrypted) is required");

            RuleFor(x => x.PublicKey)
                .NotEmpty().WithMessage("Public key is required")
                .MaximumLength(500).WithMessage("Public key cannot exceed 500 characters");
        }

        private async Task<bool> ActorExists(Guid actorId, CancellationToken cancellationToken)
        {
            return await _actorRepository.Exists(actorId);
        }
    }
}
