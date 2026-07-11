// ABOUTME: Applies full replacement writes to tenant branding typed settings documents.
// ABOUTME: Enforces validation, optimistic concurrency, and interim branding lock semantics without scalar fallback.

namespace Explore.Application.Features.TenantSettingsDocuments.Handlers.Commands;

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.TenantSettingsDocuments.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
using FluentValidation;
using MediatR;

public sealed class ReplaceTenantBrandingSettingsDocumentCommandHandler(
    ITenantSettingsDocumentRepository tenantSettingsDocumentRepository,
    ITypedSettingsDocumentResolver typedSettingsDocumentResolver,
    ITenantBrandingSettingsDocumentLockService lockService)
    : IRequestHandler<ReplaceTenantBrandingSettingsDocumentCommand, BaseCommandResponse<Guid>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<BaseCommandResponse<Guid>> Handle(
        ReplaceTenantBrandingSettingsDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var validator = new ReplaceTenantBrandingSettingsDocumentDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Document, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant branding settings replacement failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        var document = await tenantSettingsDocumentRepository.GetTrackedByTenantAndDocumentKey(
            request.TenantId,
            SettingsDocumentKeys.Tenant.Branding,
            cancellationToken);

        if (document is null)
        {
            response.Success = false;
            response.Message = "Tenant branding settings document not found.";
            return response;
        }

        if (document.ConcurrencyStamp != request.Document.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "Tenant branding settings changed since they were loaded. Refresh the settings and try again.",
                "tenant_settings_document",
                document.Id.ToString());
        }

        var currentPayload = DeserializePayload(document.PayloadJson);
        var requestedPayload = NormalizePayload(
            request.Document.Payload.DisplayName,
            request.Document.Payload.LogoUrl,
            request.Document.Payload.FaviconUrl,
            request.Document.Payload.CustomCssUrl);
        var lockState = await lockService.GetLockStateAsync(cancellationToken);
        var errors = lockService.ValidateAllowedChanges(currentPayload, requestedPayload, lockState);
        if (errors.Count > 0)
        {
            response.Success = false;
            response.Message = "Tenant branding settings replacement failed.";
            response.Errors = errors.ToList();
            return response;
        }

        var payloadJson = JsonSerializer.Serialize(requestedPayload, SerializerOptions);
        document.UpdatePayload(document.SchemaVersion, document.DefaultsVersion, payloadJson);
        await tenantSettingsDocumentRepository.Update(document);
        typedSettingsDocumentResolver.InvalidateTenantDocumentCache(request.TenantId, SettingsDocumentKeys.Tenant.Branding);

        response.Success = true;
        response.Id = document.Id;
        response.Message = "Tenant branding settings replaced successfully.";
        return response;
    }

    private static BrandingSettings DeserializePayload(string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<BrandingSettings>(payloadJson, SerializerOptions) ?? new BrandingSettings();
        }
        catch (JsonException)
        {
            return new BrandingSettings();
        }
    }

    private static BrandingSettings NormalizePayload(
        string? displayName,
        string? logoUrl,
        string? faviconUrl,
        string? customCssUrl)
        => new()
        {
            DisplayName = Normalize(displayName),
            LogoUrl = Normalize(logoUrl),
            FaviconUrl = Normalize(faviconUrl),
            CustomCssUrl = Normalize(customCssUrl)
        };

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
