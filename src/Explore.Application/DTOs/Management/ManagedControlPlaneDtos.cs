// ABOUTME: Defines bounded public Event management contracts for discovery, registration, status, health, and version.
// ABOUTME: Carries no Event business data and exposes credential material only in the one-time registration callback.

using System.ComponentModel.DataAnnotations;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Management;

public sealed record ManagementCapabilitiesDto(
    bool ManagedModeEnabled,
    string ManagementApiVersion,
    string EventVersion,
    DeploymentMode DeploymentMode,
    Guid? EventInstanceId,
    string RegistrationState,
    IReadOnlyList<string> Capabilities,
    ManagementTenantProvisioningCapacityDto? TenantProvisioningCapacity);

public sealed record ManagedEventInstanceStatusDto(
    Guid ManagedInstanceId,
    Guid EventInstanceId,
    DeploymentMode DeploymentMode,
    string EventVersion,
    string ManagementApiVersion,
    string RegistrationState,
    DateTime? RegisteredAt,
    DateTime EventToControlPlaneCredentialExpiresAt,
    DateTime ControlPlaneToEventCredentialExpiresAt);

public sealed record ManagementVersionDto(string EventVersion, string ManagementApiVersion);

public sealed record ManagementHealthDto(string Status, DateTime ObservedAt);

public sealed class ManagementUpgradePreflightRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string TargetEventVersion { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string TargetManagementApiVersion { get; init; }
}

public sealed class ManagementUpgradePostflightRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string ExpectedEventVersion { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string ExpectedManagementApiVersion { get; init; }
}

public sealed record ManagementUpgradeBlockerDto(string Code, string Message);

public sealed record ManagementUpgradePreflightDto(
    Guid? ManagedInstanceId,
    Guid? EventInstanceId,
    DeploymentMode DeploymentMode,
    string CurrentEventVersion,
    string TargetEventVersion,
    string CurrentManagementApiVersion,
    string TargetManagementApiVersion,
    string RegistrationState,
    string HealthStatus,
    bool Ready,
    IReadOnlyList<ManagementUpgradeBlockerDto> Blockers,
    DateTime ObservedAt);

public sealed record ManagementUpgradePostflightDto(
    Guid? ManagedInstanceId,
    Guid? EventInstanceId,
    DeploymentMode DeploymentMode,
    string CurrentEventVersion,
    string ExpectedEventVersion,
    string CurrentManagementApiVersion,
    string ExpectedManagementApiVersion,
    string RegistrationState,
    string HealthStatus,
    bool Verified,
    IReadOnlyList<ManagementUpgradeBlockerDto> Blockers,
    DateTime ObservedAt);

public sealed record ManagedCredentialDto(
    string KeyId,
    string Secret,
    IReadOnlyList<string> Scopes,
    DateTime ExpiresAt);

public sealed record CompleteManagedInstanceRegistrationRequest(
    Guid RegistrationAttemptId,
    Guid ManagedInstanceId,
    Guid EventInstanceId,
    string RegistrationToken,
    string RequestHash,
    string ManagementApiVersion,
    string EventVersion,
    DeploymentMode DeploymentMode,
    ManagedCredentialDto EventToControlPlaneCredential,
    ManagedCredentialDto ControlPlaneToEventCredential);

public sealed record CompleteManagedInstanceRegistrationResponse(
    Guid ManagedInstanceId,
    Guid RegistrationAttemptId,
    string RegistrationState,
    string ManagementEndpoint,
    bool Replay);

public sealed record TriggerManagedRegistrationResult(
    bool Success,
    string State,
    string? FailureCode,
    Guid? RegistrationAttemptId);

public sealed record RotateManagedControlPlaneCredentialRequest(
    string KeyId,
    string SecretHash,
    DateTime ExpiresAt);
