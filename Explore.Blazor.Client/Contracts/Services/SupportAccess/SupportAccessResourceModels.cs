// ABOUTME: Client-side HAL resource models for support-access console workflows.
// ABOUTME: Preserve API affordance links next to DTO data so UI actions are link-gated.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.SupportAccess;

public sealed record SupportAccessLink(string Rel, string? Href, string? Method, string? Title);

public sealed record SupportAccessSessionResource(
    SupportAccessSessionDto Session,
    IReadOnlyDictionary<string, SupportAccessLink> Links)
{
    public bool CanStop => HasLink("stop", HttpMethod.Post.Method);

    public bool CanForceStop => HasLink("force-stop", HttpMethod.Post.Method);

    public bool CanViewAudit => HasLink("audit-events", HttpMethod.Get.Method);

    public bool HasLink(string rel, string? method = null) =>
        SupportAccessLinkLookup.HasLink(Links, rel, method);
}

public sealed record SupportAccessAuditEventResource(
    SupportAccessAuditEventDto Event,
    IReadOnlyDictionary<string, SupportAccessLink> Links)
{
    public bool HasLink(string rel, string? method = null) =>
        SupportAccessLinkLookup.HasLink(Links, rel, method);
}

public sealed record SupportAccessSessionCollection(
    IReadOnlyList<SupportAccessSessionResource> Items,
    IReadOnlyDictionary<string, SupportAccessLink> Links,
    int TotalCount,
    int PageSize,
    string? ErrorMessage = null)
{
    public bool CanStart => HasLink("start", HttpMethod.Post.Method);

    public bool HasLink(string rel, string? method = null) =>
        SupportAccessLinkLookup.HasLink(Links, rel, method);

    public static SupportAccessSessionCollection Empty() => new(
        [],
        SupportAccessLinkLookup.Empty,
        0,
        0);

    public static SupportAccessSessionCollection Failed(string errorMessage) => new(
        [],
        SupportAccessLinkLookup.Empty,
        0,
        0,
        errorMessage);
}

public sealed record SupportAccessAuditEventCollection(
    IReadOnlyList<SupportAccessAuditEventResource> Items,
    IReadOnlyDictionary<string, SupportAccessLink> Links,
    int TotalCount,
    int PageSize,
    string? ErrorMessage = null)
{
    public bool HasLink(string rel, string? method = null) =>
        SupportAccessLinkLookup.HasLink(Links, rel, method);

    public static SupportAccessAuditEventCollection Empty() => new(
        [],
        SupportAccessLinkLookup.Empty,
        0,
        0);

    public static SupportAccessAuditEventCollection Failed(string errorMessage) => new(
        [],
        SupportAccessLinkLookup.Empty,
        0,
        0,
        errorMessage);
}

internal static class SupportAccessLinkLookup
{
    public static readonly IReadOnlyDictionary<string, SupportAccessLink> Empty =
        new Dictionary<string, SupportAccessLink>(StringComparer.OrdinalIgnoreCase);

    public static bool HasLink(
        IReadOnlyDictionary<string, SupportAccessLink> links,
        string rel,
        string? method = null)
    {
        return links.TryGetValue(rel, out var link)
            && (method is null || string.Equals(link.Method, method, StringComparison.OrdinalIgnoreCase));
    }
}
