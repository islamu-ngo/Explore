// ABOUTME: Validates bounded server-private ATProto bridge input before any PDS or database work.
// ABOUTME: Rejects malformed DID, PDS, key identifiers, and oversized OAuth session envelopes.

using Explore.Application.Features.Authentication.Atproto.Requests.Commands;
using FluentValidation;

namespace Explore.Application.Features.Authentication.Atproto.Validators;

public sealed class BootstrapAtprotoSessionCommandValidator : AbstractValidator<BootstrapAtprotoSessionCommand>
{
    public const int MaximumSessionPayloadBytes = 128 * 1024;

    public BootstrapAtprotoSessionCommandValidator()
    {
        RuleFor(command => command.ExpectedDid)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(did => did.StartsWith("did:", StringComparison.Ordinal)
                         && did.All(character => !char.IsWhiteSpace(character) && !char.IsControl(character)));
        RuleFor(command => command.ExpectedPdsUri)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeCanonicalHttpsOrigin);
        RuleFor(command => command.OAuthClientKeyId)
            .NotEmpty()
            .MaximumLength(128)
            .Matches("^[A-Za-z0-9._-]+$");
        RuleFor(command => command.OAuthSessionPayload)
            .NotNull()
            .Must(payload => payload.Length is > 0 and <= MaximumSessionPayloadBytes);
    }

    private static bool BeCanonicalHttpsOrigin(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && !string.IsNullOrWhiteSpace(uri.Host)
               && string.IsNullOrEmpty(uri.UserInfo)
               && string.IsNullOrEmpty(uri.Query)
               && string.IsNullOrEmpty(uri.Fragment)
               && uri.AbsolutePath == "/";
    }
}
