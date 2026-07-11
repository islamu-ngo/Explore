// ABOUTME: Validates Listmonk credential rotation input before encrypted SecretBinding storage.
// ABOUTME: Allows rotating username, API key, or both without ever returning plaintext values.

namespace Explore.Application.DTOs.Integrations.Validators;

using FluentValidation;

public sealed class RotateListmonkIntegrationCredentialsDtoValidator
    : AbstractValidator<RotateListmonkIntegrationCredentialsDto>
{
    public RotateListmonkIntegrationCredentialsDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.ApiUsername) || !string.IsNullOrWhiteSpace(x.ApiKey))
            .WithMessage("Listmonk API username or API key is required.");

        RuleFor(x => x.ApiUsername)
            .MaximumLength(4096)
            .WithMessage("Listmonk API username must be 4096 characters or fewer.")
            .Must(value => value is null || !value.Any(char.IsControl))
            .WithMessage("Listmonk API username cannot contain control characters.");

        RuleFor(x => x.ApiKey)
            .MaximumLength(4096)
            .WithMessage("Listmonk API key must be 4096 characters or fewer.")
            .Must(value => value is null || !value.Any(char.IsControl))
            .WithMessage("Listmonk API key cannot contain control characters.");
    }
}
