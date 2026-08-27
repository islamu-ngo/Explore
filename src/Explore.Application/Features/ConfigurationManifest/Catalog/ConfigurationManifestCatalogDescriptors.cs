// ABOUTME: Defines scope-tagged descriptors for explicit ConfigurationManifest allowlists.
// ABOUTME: Keeps catalog membership separate from registry scope and future instance authority decisions.

namespace Explore.Application.Features.ConfigurationManifest.Catalog;

using Explore.Domain.Settings;

public enum ConfigurationManifestScope
{
    Instance,
    Tenant
}

public sealed record ConfigurationManifestSettingCatalogEntry(
    ConfigurationManifestScope Scope,
    SettingDefinition Definition,
    int? MaximumStringLength = null);

public sealed record ConfigurationManifestDocumentCatalogEntry(
    ConfigurationManifestScope Scope,
    string DocumentKey,
    int SchemaVersion,
    string? DefaultsVersion,
    Type PayloadType,
    ConfigurationManifestDocumentStorage Storage);

public enum ConfigurationManifestDocumentStorage
{
    TenantSettingsDocument,
    PaidEventPolicy
}
