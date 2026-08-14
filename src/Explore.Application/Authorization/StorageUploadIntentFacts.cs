// ABOUTME: Trusted Application-owned facts for pre-create storage upload authorization.
// ABOUTME: Binds upload intent to authenticated subject, tenant, and loaded owning-resource evidence.

using Explore.Domain;

namespace Explore.Application.Authorization;

public sealed record StorageUploadIntentFacts(
    Guid SubjectUserId,
    Guid TenantId,
    string OwningResourceKind,
    Guid OwningResourceId,
    Guid? OwningOrganizationId) : IAuthorizationFacts
{
    public bool IsOrganizationTenantUpload =>
        string.Equals(OwningResourceKind, StorageOwningResourceKinds.OrganizationTenant, StringComparison.Ordinal);
}
