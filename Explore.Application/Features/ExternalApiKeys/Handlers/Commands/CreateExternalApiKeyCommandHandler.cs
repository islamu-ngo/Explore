// ABOUTME: Issues persisted external API keys for the current user or a managed organization.
// ABOUTME: Generates one-time raw secrets in the handler while storing only hash and public key id.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey.Validators;
using Explore.Application.Features.ExternalApiKeys.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.ExternalApiKeys.Handlers.Commands;

public class CreateExternalApiKeyCommandHandler : IRequestHandler<CreateExternalApiKeyCommand, CreateExternalApiKeyCommandResponse>
{
    private readonly IExternalApiKeyRepository _externalApiKeyRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly BusinessMetrics _metrics;
    private readonly ILogger<CreateExternalApiKeyCommandHandler> _logger;

    public CreateExternalApiKeyCommandHandler(
        IExternalApiKeyRepository externalApiKeyRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IUserContext userContext,
        ITenantContext tenantContext,
        BusinessMetrics metrics,
        ILogger<CreateExternalApiKeyCommandHandler> logger)
    {
        _externalApiKeyRepository = externalApiKeyRepository;
        _organizationRepository = organizationRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<CreateExternalApiKeyCommandResponse> Handle(CreateExternalApiKeyCommand request, CancellationToken cancellationToken)
    {
        var response = new CreateExternalApiKeyCommandResponse();
        var currentUserId = _userContext.GetRequiredUserId();

        var validator = new CreateExternalApiKeyDtoValidator(
            _externalApiKeyRepository,
            _organizationRepository,
            currentUserId);

        var validationResult = await validator.ValidateAsync(request.ExternalApiKeyDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "External API key creation failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        var ownerId = currentUserId;
        if (request.ExternalApiKeyDto.OwnerType == ExternalApiKeyOwnerType.Organization)
        {
            ownerId = request.ExternalApiKeyDto.OrganizationId!.Value;
            var hasPermission = await _organizationMemberRepository.HasPermissionInOrganization(
                ownerId,
                currentUserId,
                PermissionCodes.OrganizationManage);

            if (!hasPermission)
            {
                response.Success = false;
                response.Message = "You do not have permission to manage API keys for this organization.";
                response.Errors = ["Your organization role does not include organization management permission."];
                return response;
            }
        }

        var keyId = ApiKeyHashing.CreateKeyId();
        var secret = ApiKeyHashing.CreateSecret();
        var rawApiKey = ApiKeyHashing.FormatPersistedApiKey(keyId, secret);

        var externalApiKey = new ExternalApiKey
        {
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            Name = request.ExternalApiKeyDto.Name.Trim(),
            KeyId = keyId,
            SecretHash = ApiKeyHashing.ComputeHash(secret),
            Scopes = NormalizeScopes(request.ExternalApiKeyDto.Scopes),
            OwnerType = request.ExternalApiKeyDto.OwnerType,
            OwnerId = ownerId,
            Status = ExternalApiKeyStatus.Active,
            ExpiresAt = request.ExternalApiKeyDto.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId
        };

        externalApiKey = await _externalApiKeyRepository.Create(externalApiKey);

        _metrics.RecordExternalApiKeyCreated(
            externalApiKey.TenantId.ToString(),
            externalApiKey.OwnerType.ToString());

        _logger.LogInformation(
            "External API key {KeyId} created for tenant {TenantId} with owner type {OwnerType} and owner {OwnerId}.",
            externalApiKey.KeyId,
            externalApiKey.TenantId,
            externalApiKey.OwnerType,
            externalApiKey.OwnerId);

        response.Success = true;
        response.Id = externalApiKey.Id;
        response.KeyId = externalApiKey.KeyId;
        response.ApiKey = rawApiKey;
        response.Message = "External API key created successfully. Save the secret now because it will not be shown again.";
        return response;
    }

    private static string NormalizeScopes(IEnumerable<string> scopes)
    {
        return string.Join(' ', scopes
            .Select(scope => scope.Trim())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(scope => scope, StringComparer.OrdinalIgnoreCase));
    }
}
