// ABOUTME: Validates the authenticated tenant/user/DID tuple before current-session storage access.
// ABOUTME: Rejects empty identifiers and malformed or unbounded DIDs before gateway dispatch.

using Explore.Application.Features.Authentication.Atproto.Models;
using FluentValidation;

namespace Explore.Application.Features.Authentication.Atproto.Validators;

public sealed class AtprotoCurrentSessionIdentityValidator : AbstractValidator<AtprotoCurrentSessionIdentity>
{
    public AtprotoCurrentSessionIdentityValidator()
    {
        RuleFor(identity => identity.TenantId).NotEmpty();
        RuleFor(identity => identity.UserId).NotEmpty();
        RuleFor(identity => identity.Did)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(did => did.StartsWith("did:", StringComparison.Ordinal)
                         && did.All(character => !char.IsWhiteSpace(character) && !char.IsControl(character)));
    }
}
