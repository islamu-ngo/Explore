// ABOUTME: Verifies generated purchase-governance client methods and contract mutability policy.
// ABOUTME: Pins machine-consumed headers, safe request authority, HAL shape, and immutable response values.

using System.Text.Json;

namespace Explore.GeneratedContracts.Tests;

public sealed class TicketPurchaseGeneratedContractTests
{
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();

    [Test]
    public async Task CanonicalSchemaPublishesBothPurchaseOperationsAndSafeInput()
    {
        using JsonDocument document = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    RepositoryRoot,
                    "schemas",
                    "openapi_islamu-event.json")));
        JsonElement root = document.RootElement;
        JsonElement paths = root.GetProperty("paths");
        JsonElement schemas = root.GetProperty("components")
            .GetProperty("schemas");

        await Assert.That(paths.TryGetProperty(
                "/api/events/{eventId}/registration-orders/{orderId}/purchase-authority",
                out _))
            .IsTrue();
        await Assert.That(paths.TryGetProperty(
                "/api/events/{eventId}/registration-orders/guest/{orderId}/purchase-authority",
                out _))
            .IsTrue();
        JsonElement request = schemas
            .GetProperty("ReserveTicketPurchaseRequest")
            .GetProperty("properties");
        await Assert.That(request.TryGetProperty(
                "accessMode",
                out _))
            .IsTrue();
        await Assert.That(request.TryGetProperty(
                "tenantId",
                out _))
            .IsFalse();
        await Assert.That(request.TryGetProperty(
                "accountUserId",
                out _))
            .IsFalse();
        await Assert.That(request.TryGetProperty(
                "quantity",
                out _))
            .IsFalse();
        await Assert.That(request.TryGetProperty(
                "policyVersionId",
                out _))
            .IsFalse();
        JsonElement hal = schemas
            .GetProperty(
                "HalResourceOfTicketPurchaseGovernanceResource")
            .GetProperty("properties");
        await Assert.That(hal.TryGetProperty(
                "orderId",
                out _))
            .IsTrue();
        await Assert.That(hal.TryGetProperty(
                "_links",
                out _))
            .IsTrue();
    }

    [Test]
    public async Task GeneratedClientCarriesHeadersAndImmutableResponseShape()
    {
        string source = await File.ReadAllTextAsync(
            Path.Combine(
                RepositoryRoot,
                "src",
                "Explore.Blazor.Client",
                "Clients",
                "EventApiClient.g.cs"));

        await Assert.That(source).Contains(
            "ReserveAuthenticatedPurchaseAuthorityAsync(System.Guid eventId, System.Guid orderId, string idempotency_Key");
        await Assert.That(source).Contains(
            "ReserveGuestPurchaseAuthorityAsync(System.Guid eventId, System.Guid orderId, string idempotency_Key, string? x_Registration_Order_Capability");
        string request = ExtractType(
            source,
            "public partial class ReserveTicketPurchaseRequest");
        await Assert.That(request).DoesNotContain("TenantId");
        await Assert.That(request).DoesNotContain("AccountUserId");
        await Assert.That(request).DoesNotContain("Quantity");
        await Assert.That(request).DoesNotContain(
            "PolicyVersionId");

        string response = ExtractType(
            source,
            "public partial record class TicketPurchaseGovernanceResource");
        await Assert.That(response).Contains(
            "public System.Guid OrderId { get; init; }");
        await Assert.That(response).Contains(
            "public bool SupportsHardCrossOrderCeiling { get; init; }");
        await Assert.That(response).Contains(
            "public string EnforcementScopeCode { get; init; }");
        string hal = ExtractType(
            source,
            "public partial class HalResourceOfTicketPurchaseGovernanceResource");
        await Assert.That(hal).Contains(
            "public System.Guid OrderId { get; set; }");
        await Assert.That(hal).Contains(
            "public bool SupportsHardCrossOrderCeiling { get; set; }");
        await Assert.That(hal).Contains(
            "public System.Collections.Generic.IDictionary<string, HalLink>? _links");
    }

    private static string ExtractType(
        string source,
        string declaration)
    {
        int start = source.IndexOf(
            declaration,
            StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        int next = source.IndexOf(
            "[System.CodeDom.Compiler.GeneratedCode",
            start + declaration.Length,
            StringComparison.Ordinal);
        return next < 0
            ? source[start..]
            : source[start..next];
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(
            AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "Explore.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root was not found.");
    }
}
