// ABOUTME: Tenant-owned provider connection metadata for external registration collection.
// ABOUTME: References SecretBinding credentials only; no provider secret value or adapter-specific payload is stored.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class RegistrationProviderConnection : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    private RegistrationProviderConnection() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public int ProviderKindId { get; private set; }
    public int DeploymentKindId { get; private set; }
    public Guid? ApiTokenSecretBindingId { get; private set; }
    public Guid? WebhookSecretBindingId { get; private set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    public static RegistrationProviderConnection Create(Guid tenantId, string name, RegistrationProviderKindEnum providerKind,
        RegistrationProviderDeploymentKindEnum deploymentKind, Guid? apiTokenSecretBindingId, Guid? webhookSecretBindingId, DateTime createdAt) =>
        Create(Guid.CreateVersion7(), tenantId, name, providerKind, deploymentKind, apiTokenSecretBindingId, webhookSecretBindingId, createdAt);

    public static RegistrationProviderConnection Create(Guid id, Guid tenantId, string name, RegistrationProviderKindEnum providerKind,
        RegistrationProviderDeploymentKindEnum deploymentKind, Guid? apiTokenSecretBindingId, Guid? webhookSecretBindingId, DateTime createdAt)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || apiTokenSecretBindingId == Guid.Empty || webhookSecretBindingId == Guid.Empty ||
            !Enum.IsDefined(providerKind) || !Enum.IsDefined(deploymentKind))
        {
            throw new ArgumentException("Provider connection identities and lookup values must be valid.");
        }

        return new RegistrationProviderConnection
        {
            Id = id,
            TenantId = tenantId,
            Name = NormalizeText(name, nameof(name), 120),
            ProviderKindId = (int)providerKind,
            DeploymentKindId = (int)deploymentKind,
            ApiTokenSecretBindingId = apiTokenSecretBindingId,
            WebhookSecretBindingId = webhookSecretBindingId,
            CreatedAt = EnsureUtc(createdAt, nameof(createdAt))
        };
    }

    private static string NormalizeText(string value, string parameterName, int maxLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length is > 0 && normalized.Length <= maxLength
            ? normalized
            : throw new ArgumentException($"Value must be non-blank and at most {maxLength} characters.", parameterName);
    }

    internal static DateTime EnsureUtc(DateTime value, string parameterName) =>
        value != default && value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be a non-default UTC value.", parameterName);
}
