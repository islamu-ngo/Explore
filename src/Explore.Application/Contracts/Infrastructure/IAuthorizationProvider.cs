// ABOUTME: Application-owned typed authorization port and provider-neutral decision model.
// ABOUTME: Keeps capabilities catalog-bound while Local and Cerbos stay infrastructure adapters.

namespace Explore.Application.Contracts.Infrastructure;

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Explore.Application.Authorization;

public interface IAuthorizationProvider
{
    Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthorizationDecision>> AuthorizeBatchAsync(
        IReadOnlyList<AuthorizationRequest> requests,
        CancellationToken cancellationToken = default);
}

public sealed record AuthorizationCapability
{
    internal AuthorizationCapability(string resourceKind, string action)
    {
        ResourceKind = resourceKind;
        Action = action;
    }

    public string ResourceKind { get; }

    public string Action { get; }
}

public static class AuthorizationCapabilityCatalog
{
    private static readonly Lazy<IReadOnlySet<string>> KnownResourceKinds = new(BuildResourceKinds);
    private static readonly Lazy<IReadOnlySet<string>> KnownActions = new(BuildActions);

    public static AuthorizationCapability Require(string resourceKind, string action)
    {
        if (string.IsNullOrWhiteSpace(resourceKind) || !KnownResourceKinds.Value.Contains(resourceKind))
            throw new ArgumentException("Authorization resource kind is not in the catalog.", nameof(resourceKind));

        if (string.IsNullOrWhiteSpace(action) || !KnownActions.Value.Contains(action))
            throw new ArgumentException("Authorization action is not in the catalog.", nameof(action));

        return new AuthorizationCapability(resourceKind, action);
    }

    private static IReadOnlySet<string> BuildResourceKinds() =>
        typeof(ResourceKinds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => (string)field.GetValue(null)!)
            .ToHashSet(StringComparer.Ordinal);

    private static IReadOnlySet<string> BuildActions()
    {
        var actionTypes = new[] { typeof(AuthorizationActions) }
            .Concat(typeof(AuthorizationActions).GetNestedTypes(BindingFlags.Public));

        return actionTypes
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.FieldType == typeof(string))
            .Select(field => (string)field.GetValue(null)!)
            .ToHashSet(StringComparer.Ordinal);
    }
}

public sealed record AuthorizationSubject(Guid? UserId = null, bool IsMachine = false)
{
    public static readonly AuthorizationSubject Ambient = new();
}

public sealed record AuthorizationTenant(Guid? TenantId = null, Guid? OrganizationId = null)
{
    public static readonly AuthorizationTenant Ambient = new();
}

public enum AuthorizationDecisionOutcome
{
    Deny = 0,
    Allow = 1
}

public static class AuthorizationDecisionReasonCodes
{
    public const string Allowed = "allowed";
    public const string Denied = "denied";
    public const string InvalidRequest = "invalid_request";
    public const string MissingSubject = "missing_subject";
    public const string ProviderUnavailable = "provider_unavailable";
    public const string ProviderError = "provider_error";
}

public sealed record AuthorizationProviderMetadata(string ProviderId, string? ObservedRevision = null)
{
    public static readonly AuthorizationProviderMetadata Runtime = new("runtime");
    public static readonly AuthorizationProviderMetadata Local = new("local");
    public static readonly AuthorizationProviderMetadata Cerbos = new("cerbos");
}

public sealed record AuthorizationDecision(
    AuthorizationDecisionOutcome Outcome,
    string ReasonCode,
    AuthorizationProviderMetadata Provider)
{
    public bool IsAllowed => Outcome == AuthorizationDecisionOutcome.Allow;

    public static AuthorizationDecision Allow(
        AuthorizationProviderMetadata provider,
        string reasonCode = AuthorizationDecisionReasonCodes.Allowed) =>
        new(AuthorizationDecisionOutcome.Allow, reasonCode, provider);

    public static AuthorizationDecision Deny(
        AuthorizationProviderMetadata provider,
        string reasonCode = AuthorizationDecisionReasonCodes.Denied) =>
        new(AuthorizationDecisionOutcome.Deny, reasonCode, provider);
}

public record AuthorizationRequest(
    AuthorizationCapability Capability,
    string ResourceId,
    IReadOnlyDictionary<string, object>? ResourceAttributes = null,
    AuthorizationScope? Scope = null,
    IAuthorizationFacts? Facts = null,
    AuthorizationSubject? Subject = null,
    AuthorizationTenant? Tenant = null)
{
    public AuthorizationRequest(
        string resourceKind,
        string resourceId,
        string action,
        IReadOnlyDictionary<string, object>? ResourceAttributes = null,
        AuthorizationScope? Scope = null,
        IAuthorizationFacts? Facts = null,
        AuthorizationSubject? Subject = null,
        AuthorizationTenant? Tenant = null)
        : this(
            AuthorizationCapabilityCatalog.Require(resourceKind, action),
            resourceId,
            ResourceAttributes,
            Scope,
            Facts,
            Subject,
            Tenant)
    {
    }

    public string ResourceKind => Capability.ResourceKind;

    public string Action => Capability.Action;

    public string ToDeduplicationKey()
    {
        var builder = new StringBuilder();

        AppendSegment(builder, ResourceKind);
        AppendSegment(builder, ResourceId);
        AppendSegment(builder, Action);
        AppendScope(builder, Scope);
        AppendAttributes(builder, ResourceAttributes);

        return builder.ToString();
    }

    private static void AppendScope(StringBuilder builder, AuthorizationScope? scope)
    {
        AppendSegment(builder, scope?.TenantId ?? string.Empty);
        AppendSegment(builder, scope?.OrganizationId ?? string.Empty);
    }

    private static void AppendAttributes(StringBuilder builder, IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            AppendSegment(builder, string.Empty);
            return;
        }

        AppendSegment(builder, attributes.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var pair in attributes.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            AppendSegment(builder, pair.Key);
            AppendSegment(builder, NormalizeAttributeValue(pair.Value));
        }
    }

    private static string NormalizeAttributeValue(object? value)
    {
        if (value is null)
            return "<null>";

        var rendered = value switch
        {
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        return $"{value.GetType().FullName}:{rendered}";
    }

    private static void AppendSegment(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
        builder.Append('|');
    }
}

public sealed record AuthorizationCheck : AuthorizationRequest
{
    public AuthorizationCheck(
        AuthorizationCapability capability,
        string resourceId,
        IReadOnlyDictionary<string, object>? resourceAttributes = null,
        AuthorizationScope? scope = null,
        IAuthorizationFacts? facts = null,
        AuthorizationSubject? subject = null,
        AuthorizationTenant? tenant = null)
        : base(capability, resourceId, resourceAttributes, scope, facts, subject, tenant)
    {
    }

    public AuthorizationCheck(
        string resourceKind,
        string resourceId,
        string action,
        IReadOnlyDictionary<string, object>? ResourceAttributes = null,
        AuthorizationScope? Scope = null,
        IAuthorizationFacts? Facts = null,
        AuthorizationSubject? Subject = null,
        AuthorizationTenant? Tenant = null)
        : base(resourceKind, resourceId, action, ResourceAttributes, Scope, Facts, Subject, Tenant)
    {
    }
}
