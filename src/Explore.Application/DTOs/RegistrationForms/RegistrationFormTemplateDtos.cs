// ABOUTME: Defines bounded registration-form template catalog API contracts.
// ABOUTME: Carries template ownership, source version provenance, and instantiation input without graph duplication.

namespace Explore.Application.DTOs.RegistrationForms;

public sealed record RegistrationFormTemplateDto(
    Guid Id,
    Guid? TenantId,
    bool IsPlatformOwned,
    string Name,
    string Description,
    string Category,
    string? PackKey,
    Guid SourceEventId,
    Guid SourceRegistrationFormId,
    Guid SourceRegistrationFormVersionId,
    Guid ConcurrencyStamp);

public sealed record RegistrationFormTemplateInputDto(
    string Name,
    string Description,
    string Category,
    string? PackKey,
    Guid SourceEventId,
    Guid SourceRegistrationFormId,
    Guid SourceRegistrationFormVersionId,
    bool IsPlatformOwned);

public sealed record InstantiateRegistrationFormTemplateInputDto(
    Guid EventId,
    Guid WorkflowId,
    string Namespace,
    string Key,
    string Name,
    Guid ExpectedWorkflowConcurrencyStamp);
