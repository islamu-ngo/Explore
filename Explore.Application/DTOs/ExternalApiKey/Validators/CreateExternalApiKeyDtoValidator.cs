// ABOUTME: Validates external API key creation requests before handlers persist credentials.
// ABOUTME: Enforces safe names, organization ownership requirements, and owner-scoped uniqueness.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Domain.Enums;
using FluentValidation;

namespace Explore.Application.DTOs.ExternalApiKey.Validators;

internal class CreateExternalApiKeyDtoValidator : AbstractValidator<CreateExternalApiKeyDto>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public CreateExternalApiKeyDtoValidator(
        IExternalApiKeyRepository externalApiKeyRepository,
        IOrganizationRepository organizationRepository,
        Guid currentUserId)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _organizationRepository = organizationRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API key name is required.")
            .MaximumLength(200).WithMessage("API key name cannot exceed 200 characters.")
            .MustAsync((dto, name, cancellationToken) => NameIsUniqueAsync(dto, currentUserId, name, cancellationToken))
            .WithMessage("An API key with the same name already exists for this owner.");

        RuleFor(x => x.Scopes)
            .NotEmpty().WithMessage("At least one scope is required.")
            .Must(scopes => scopes.All(scope => !string.IsNullOrWhiteSpace(scope)))
            .WithMessage("Scopes cannot contain empty values.");

        When(x => x.OwnerType == ExternalApiKeyOwnerType.Organization, () =>
        {
            RuleFor(x => x.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required for organization-owned API keys.")
                .MustAsync(OrganizationExistsAsync)
                .WithMessage("Organization does not exist.");
        });
    }

    private async Task<bool> NameIsUniqueAsync(CreateExternalApiKeyDto dto, Guid currentUserId, string name, CancellationToken cancellationToken)
    {
        var ownerId = dto.OwnerType == ExternalApiKeyOwnerType.Organization
            ? dto.OrganizationId
            : currentUserId;

        if (!ownerId.HasValue)
        {
            return false;
        }

        return !await _externalApiKeyRepository.ExistsByOwnerAndName(dto.OwnerType, ownerId.Value, name);
    }

    private async Task<bool> OrganizationExistsAsync(Guid? organizationId, CancellationToken cancellationToken)
    {
        return organizationId.HasValue && await _organizationRepository.Exists(organizationId.Value);
    }
}
