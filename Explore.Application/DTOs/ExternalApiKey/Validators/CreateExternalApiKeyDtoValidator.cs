// ABOUTME: Validates external API key creation requests before handlers persist credentials.
// ABOUTME: Enforces safe names, entity existence for Organization/Group types, and owner-scoped uniqueness.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Domain.Enums;
using FluentValidation;

namespace Explore.Application.DTOs.ExternalApiKey.Validators;

internal class CreateExternalApiKeyDtoValidator : AbstractValidator<CreateExternalApiKeyDto>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGroupRepository _groupRepository;

    public CreateExternalApiKeyDtoValidator(
        IExternalApiKeyRepository externalApiKeyRepository,
        IOrganizationRepository organizationRepository,
        IGroupRepository groupRepository,
        Guid currentUserId,
        Guid? tenantId)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _organizationRepository = organizationRepository;
        _groupRepository = groupRepository;

        RuleFor(x => x.OwnerType)
            .IsInEnum().WithMessage("Invalid owner type.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API key name is required.")
            .MaximumLength(200).WithMessage("API key name cannot exceed 200 characters.")
            .MustAsync((dto, name, cancellationToken) => NameIsUniqueAsync(dto, currentUserId, tenantId, name, cancellationToken))
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

        When(x => x.OwnerType == ExternalApiKeyOwnerType.Group, () =>
        {
            RuleFor(x => x.GroupId)
                .NotEmpty().WithMessage("Group ID is required for group-owned API keys.")
                .MustAsync(GroupExistsAsync)
                .WithMessage("Group does not exist.");
        });
    }

    private async Task<bool> NameIsUniqueAsync(
        CreateExternalApiKeyDto dto, Guid currentUserId, Guid? tenantId, string name, CancellationToken cancellationToken)
    {
        var ownerId = ResolveOwnerId(dto, currentUserId, tenantId);

        if (!ownerId.HasValue)
        {
            return false;
        }

        return !await _externalApiKeyRepository.ExistsByOwnerAndName(dto.OwnerType, ownerId.Value, name);
    }

    private static Guid? ResolveOwnerId(CreateExternalApiKeyDto dto, Guid currentUserId, Guid? tenantId)
    {
        return dto.OwnerType switch
        {
            ExternalApiKeyOwnerType.Organization => dto.OrganizationId,
            ExternalApiKeyOwnerType.Group => dto.GroupId,
            ExternalApiKeyOwnerType.Tenant => tenantId,
            _ => currentUserId // User and InstanceAdmin both use current user's ID
        };
    }

    private async Task<bool> OrganizationExistsAsync(Guid? organizationId, CancellationToken cancellationToken)
    {
        return organizationId.HasValue && await _organizationRepository.Exists(organizationId.Value);
    }

    private async Task<bool> GroupExistsAsync(Guid? groupId, CancellationToken cancellationToken)
    {
        return groupId.HasValue && await _groupRepository.Exists(groupId.Value);
    }
}
