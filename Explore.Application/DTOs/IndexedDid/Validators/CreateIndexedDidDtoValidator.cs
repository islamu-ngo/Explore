using FluentValidation;
using Explore.Application.Contracts.Persistence;

namespace Explore.Application.DTOs.IndexedDid.Validators
{
    public class CreateIndexedDidDtoValidator : AbstractValidator<CreateIndexedDidDto>
    {
        public CreateIndexedDidDtoValidator()
        {
            RuleFor(x => x.Did)
                .NotEmpty().WithMessage("Did is required")
                .MaximumLength(255).WithMessage("Did must not exceed 255 characters")
                .Must(BeValidDidFormat).WithMessage("Did must be in valid format (did:plc:xxx or did:web:xxx)");

            RuleFor(x => x.PdsHost)
                .NotEmpty().WithMessage("PDS host is required")
                .MaximumLength(500).WithMessage("PDS host must not exceed 500 characters");

            RuleFor(x => x.Handle)
                .MaximumLength(255).WithMessage("Handle must not exceed 255 characters");

            RuleFor(x => x.SigningKey)
                .MaximumLength(500).WithMessage("Signing key must not exceed 500 characters");

            RuleFor(x => x.LastIndexedAt)
                .NotEmpty().WithMessage("Last indexed at is required");
        }

        private bool BeValidDidFormat(string did)
        {
            if (string.IsNullOrEmpty(did)) return false;
            return did.StartsWith("did:plc:") || did.StartsWith("did:web:");
        }
    }
}
