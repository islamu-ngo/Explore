// ABOUTME: Validates editable policy changes for persisted external API keys.
// ABOUTME: Enforces owner-scoped name uniqueness without allowing ownership changes.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Features.ExternalApiKeys;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using FluentValidation;

namespace Explore.Application.DTOs.ExternalApiKey.Validators;

internal class UpdateExternalApiKeyPolicyDtoValidator : AbstractValidator<UpdateExternalApiKeyPolicyDto>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;

    public UpdateExternalApiKeyPolicyDtoValidator(IExternalApiKeyRepository externalApiKeyRepository, Explore.Domain.ExternalApiKey existingApiKey)
    {
        _externalApiKeyRepository = externalApiKeyRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("API key ID is required.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("API key name is required.")
            .MaximumLength(ExternalApiKeyInputValidation.NameMaxLength).WithMessage("API key name cannot exceed 200 characters.")
            .Must(ExternalApiKeyInputValidation.DoesNotContainControlCharacters).WithMessage("API key name must not contain control characters.")
            .MustAsync((dto, name, cancellationToken) => NameIsUniqueAsync(
                existingApiKey,
                ExternalApiKeyInputValidation.NormalizeRequiredText(name),
                cancellationToken))
            .WithMessage("An API key with the same name already exists for this owner.");

        RuleFor(x => x.Scopes)
            .NotEmpty().WithMessage("At least one scope is required.")
            .Must(scopes => scopes.All(scope => !string.IsNullOrWhiteSpace(scope)))
            .WithMessage("Scopes cannot contain empty values.")
            .Must(scopes => ExternalApiKeyScopes.AreAllValid(scopes))
            .WithMessage((dto, _) => $"Invalid scopes: {string.Join(", ", ExternalApiKeyScopes.GetInvalid(dto.Scopes))}.")
            .Must((dto, scopes) => ExternalApiKeyScopeCeiling.AreWithinCeiling(existingApiKey.OwnerType, scopes))
            .WithMessage((dto, _) => $"Scopes exceed ceiling for {existingApiKey.OwnerType}: {string.Join(", ", ExternalApiKeyScopeCeiling.GetExceeding(existingApiKey.OwnerType, dto.Scopes))}.");
    }

    private async Task<bool> NameIsUniqueAsync(Explore.Domain.ExternalApiKey existingApiKey, string name, CancellationToken cancellationToken)
    {
        if (string.Equals(existingApiKey.Name, name, StringComparison.Ordinal))
        {
            return true;
        }

        if (existingApiKey.OwnerType == ExternalApiKeyOwnerType.InstanceAdmin)
        {
            return !await _externalApiKeyRepository.ExistsByOwnerAndNameIgnoringTenantFilter(
                existingApiKey.OwnerType,
                existingApiKey.OwnerId,
                name,
                cancellationToken);
        }

        return !await _externalApiKeyRepository.ExistsByOwnerAndName(
            existingApiKey.OwnerType,
            existingApiKey.OwnerId,
            name,
            cancellationToken);
    }
}
