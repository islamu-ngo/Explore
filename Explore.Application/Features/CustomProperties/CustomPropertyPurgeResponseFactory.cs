// ABOUTME: Shared helpers for explicit audited custom-property purge command responses.
// ABOUTME: Centralizes dependency summaries so shared/event/session purge handlers stay consistent.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Domain;

namespace Explore.Application.Features.CustomProperties;

internal static class CustomPropertyPurgeResponseFactory
{
    public const string PurgeAction = "CustomPropertyDefinitionPurged";

    public static CustomPropertyPurgeResultDto ToResult(
        CustomPropertyPurgeDependencySummary summary,
        bool purged,
        Guid? auditLogId,
        string reason)
        => new(
            summary.DefinitionId,
            summary.TenantId,
            summary.Scope,
            purged,
            auditLogId,
            reason,
            summary.OptionCount,
            summary.ValueCount,
            summary.ProjectionCount,
            summary.AuditLogCount,
            summary.SyncProvenanceCount);

    public static AuditLog CreateAudit(
        CustomPropertyPurgeDependencySummary summary,
        CustomPropertyPurgeResultDto result,
        Guid? actorId)
        => new()
        {
            Id = result.AuditLogId ?? Guid.NewGuid(),
            TenantId = summary.TenantId,
            Tenant = null!,
            EntityType = summary.Scope,
            EntityId = summary.DefinitionId.ToString(),
            Action = PurgeAction,
            OldValues = JsonSerializer.Serialize(new
            {
                summary.OptionCount,
                summary.ValueCount,
                summary.ProjectionCount,
                summary.AuditLogCount,
                summary.SyncProvenanceCount
            }),
            NewValues = JsonSerializer.Serialize(new
            {
                result.Purged,
                result.Reason,
                result.AuditLogId
            }),
            AffectedColumns = JsonSerializer.Serialize(new[] { "definition", "options" }),
            ActorId = actorId,
            Timestamp = DateTime.UtcNow
        };

    public static IReadOnlyList<string> ToBlockingErrors(CustomPropertyPurgeDependencySummary summary)
    {
        var errors = new List<string>();

        if (summary.ValueCount > 0)
        {
            errors.Add($"Purge blocked: {summary.ValueCount} historical custom-property value(s) reference this definition.");
        }

        if (summary.ProjectionCount > 0)
        {
            errors.Add($"Purge blocked: {summary.ProjectionCount} projection row(s) reference this definition.");
        }

        if (summary.AuditLogCount > 0)
        {
            errors.Add($"Purge blocked: {summary.AuditLogCount} audit log row(s) reference this definition.");
        }

        if (summary.SyncProvenanceCount > 0)
        {
            errors.Add($"Purge blocked: {summary.SyncProvenanceCount} template sync provenance reference(s) exist for this definition.");
        }

        return errors;
    }
}
