// ABOUTME: Transaction-neutral boundary for creating one tenant and its final branding document.
// ABOUTME: Leaves locking, transaction ownership, cache invalidation, and notifications to callers.

using Explore.Domain;
using Explore.Domain.Settings.Documents;

namespace Explore.Application.Contracts.Services;

public sealed record TenantCreationRequest(
    Guid TenantId,
    Guid BrandingDocumentId,
    string FullName,
    string Slug,
    int TenantStatusId,
    Guid? ActorUserId,
    DateTime OccurredAt,
    string BrandingDocumentKey,
    int BrandingSchemaVersion,
    string BrandingDefaultsVersion,
    string BrandingPayloadJson);

public sealed record TenantCreationOutcome(
    Tenant Tenant,
    TenantSettingsDocument BrandingDocument);

public interface ITenantCreationService
{
    Task<TenantCreationOutcome> CreateInCurrentTransactionAsync(
        TenantCreationRequest request,
        CancellationToken cancellationToken);
}
