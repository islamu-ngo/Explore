// ABOUTME: BFF-only registration provider embed host for approved external registration descriptors.
// ABOUTME: Keeps provider URLs server-derived while the browser loads external content only inside a sandboxed iframe.

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Microsoft.Net.Http.Headers;

namespace Explore.Blazor.Extensions;

public static class BffRegistrationProviderEmbedEndpoints
{
    private const string Route = "/bff/registration-provider-embed/tenants/{tenantId:guid}/events/{eventId:guid}/workflows/{workflowId:guid}/requirements/{requirementId:guid}/channels/{channelId:guid}/bindings/{bindingId:guid}";
    private const string HtmlContentType = "text/html; charset=utf-8";

    public static WebApplication MapRegistrationProviderEmbedEndpoints(this WebApplication app)
    {
        app.MapGet(Route, HandleEmbedHostAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleEmbedHostAsync(
        Guid tenantId,
        Guid eventId,
        Guid workflowId,
        Guid requirementId,
        Guid channelId,
        Guid bindingId,
        HttpContext ctx,
        IEventApiClient apiClient,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(ctx.Response.Headers);

        if (ctx.Request.Query.Count != 0)
        {
            return Results.BadRequest();
        }

        RegistrationProviderLaunchDescriptor descriptor;
        try
        {
            var resource = await apiClient.GetRegistrationProviderLaunchDescriptorAsync(
                tenantId,
                eventId,
                workflowId,
                requirementId,
                channelId,
                bindingId,
                cancellationToken: cancellationToken);
            descriptor = RegistrationProviderLaunchDescriptor.From(resource.AdditionalProperties);
        }
        catch (ApiException exception) when (exception.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            return Results.StatusCode(exception.StatusCode);
        }

        if (!descriptor.Matches(tenantId, eventId, workflowId, requirementId, channelId, bindingId) ||
            !descriptor.Available ||
            !string.Equals(descriptor.Mode, "embed", StringComparison.Ordinal) ||
            !TryGetApprovedHttpsUri(descriptor.Url, out var embedUri))
        {
            return Results.NotFound();
        }

        ctx.Response.Headers[HeaderNames.ContentSecurityPolicy] = BuildContentSecurityPolicy(embedUri);
        ctx.Response.Headers[HeaderNames.XFrameOptions] = "SAMEORIGIN";
        ctx.Items[MiddlewareExtensions.PreserveExplicitSecurityHeadersItemKey] = true;
        return Results.Content(BuildHtml(embedUri, descriptor.Title), HtmlContentType, Encoding.UTF8);
    }

    private static string BuildContentSecurityPolicy(Uri embedUri) =>
        "default-src 'none'; " +
        $"frame-src {embedUri.GetLeftPart(UriPartial.Authority)}; " +
        "frame-ancestors 'self'; " +
        "base-uri 'none'; " +
        "form-action 'none'; " +
        "object-src 'none'; " +
        "script-src 'none'; " +
        "style-src 'none'";

    private static string BuildHtml(Uri embedUri, string title)
    {
        var encodedUrl = HtmlEncoder.Default.Encode(embedUri.ToString());
        var encodedTitle = HtmlEncoder.Default.Encode(string.IsNullOrWhiteSpace(title) ? "Registration form" : title);
        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>" + encodedTitle +
            "</title></head><body><iframe src=\"" + encodedUrl + "\" title=\"" + encodedTitle +
            "\" sandbox=\"allow-forms allow-same-origin allow-scripts\" referrerpolicy=\"no-referrer\"></iframe>" +
            "<p><a href=\"" + encodedUrl + "\" target=\"_blank\" rel=\"noopener noreferrer\">Open " + encodedTitle +
            "</a></p></body></html>";
    }

    private static void SetNoStoreHeaders(IHeaderDictionary headers)
    {
        headers[HeaderNames.CacheControl] = "private, no-store";
        headers[HeaderNames.Pragma] = "no-cache";
        headers[HeaderNames.Expires] = "0";
    }

    private static bool TryGetApprovedHttpsUri(string? url, out Uri uri)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out uri!) &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(uri.UserInfo) &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            !IsBlockedLiteralHost(uri.Host);
    }

    private static bool IsBlockedLiteralHost(string host)
    {
        var literal = host.StartsWith('[') && host.EndsWith(']') ? host[1..^1] : host;
        if (!IPAddress.TryParse(literal, out var address))
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.IPv6Loopback) ||
            address.IsIPv6LinkLocal ||
            address.IsIPv6SiteLocal ||
            address.IsIPv6Multicast ||
            IsUniqueLocalIpv6(address) ||
            IsBlockedIpv4(address);
    }

    private static bool IsBlockedIpv4(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 0 ||
            bytes[0] == 10 ||
            bytes[0] == 127 ||
            bytes[0] == 169 && bytes[1] == 254 ||
            bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
            bytes[0] == 192 && bytes[1] == 168 ||
            bytes[0] >= 224;
    }

    private static bool IsUniqueLocalIpv6(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        return (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
    }

    private sealed record RegistrationProviderLaunchDescriptor(
        Guid TenantId,
        Guid EventId,
        Guid WorkflowId,
        Guid RequirementId,
        Guid ChannelId,
        Guid BindingId,
        string Mode,
        bool Available,
        string? Url,
        string Title)
    {
        public static RegistrationProviderLaunchDescriptor From(IDictionary<string, object> properties) => new(
            GetGuid(properties, "tenantId"),
            GetGuid(properties, "eventId"),
            GetGuid(properties, "workflowId"),
            GetGuid(properties, "requirementId"),
            GetGuid(properties, "channelId"),
            GetGuid(properties, "bindingId"),
            GetString(properties, "mode"),
            GetBool(properties, "available"),
            GetNullableString(properties, "url"),
            GetString(properties, "title"));

        public bool Matches(Guid tenantId, Guid eventId, Guid workflowId, Guid requirementId, Guid channelId, Guid bindingId) =>
            TenantId == tenantId &&
            EventId == eventId &&
            WorkflowId == workflowId &&
            RequirementId == requirementId &&
            ChannelId == channelId &&
            BindingId == bindingId;

        private static Guid GetGuid(IDictionary<string, object> properties, string name) =>
            TryGet<JsonElement>(properties, name, out var element) && element.ValueKind == JsonValueKind.String && element.TryGetGuid(out var guid)
                ? guid
                : TryGet<Guid>(properties, name, out guid) ? guid : Guid.Empty;

        private static string GetString(IDictionary<string, object> properties, string name) =>
            TryGet<JsonElement>(properties, name, out var element) && element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? string.Empty
                : TryGet<string>(properties, name, out var value) ? value : string.Empty;

        private static string? GetNullableString(IDictionary<string, object> properties, string name)
        {
            var value = GetString(properties, name);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static bool GetBool(IDictionary<string, object> properties, string name) =>
            TryGet<JsonElement>(properties, name, out var element) && element.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? element.GetBoolean()
                : TryGet<bool>(properties, name, out var value) && value;

        private static bool TryGet<T>(IDictionary<string, object> properties, string name, out T value)
        {
            if (properties.TryGetValue(name, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
