// ABOUTME: Verifies generated fair-return waitlist operations preserve line scope and header-only authority.
// ABOUTME: Pins bounded immutable HAL state, idempotent writes, and absence of paid-priority or PII fields.

using System.Text.Json;

namespace Explore.GeneratedContracts.Tests;

public sealed class
    FairReturnWaitlistGeneratedContractTests
{
    private const string RootPath =
        "/api/events/{eventId}/registration-orders/" +
        "{registrationOrderId}/lines/" +
        "{registrationOrderLineId}/waitlist";
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();

    [Test]
    public async Task CanonicalSchemaPublishesExactWaitlistLifecycle()
    {
        using JsonDocument document =
            JsonDocument.Parse(
                await File.ReadAllTextAsync(
                    Path.Combine(
                        RepositoryRoot,
                        "schemas",
                        "openapi_islamu-event.json")));
        JsonElement paths =
            document.RootElement.GetProperty(
                "paths");
        await Assert.That(paths.TryGetProperty(
                RootPath,
                out JsonElement root))
            .IsTrue();
        foreach (string operation
                 in new[] { "get", "post", "delete" })
        {
            await Assert.That(root.TryGetProperty(
                    operation,
                    out _))
                .IsTrue();
        }
        await Assert.That(paths.TryGetProperty(
                RootPath +
                "/offers/{offerId}/accept",
                out JsonElement accept))
            .IsTrue();
        await Assert.That(
                accept.TryGetProperty("post", out _))
            .IsTrue();
        await Assert.That(paths.TryGetProperty(
                RootPath +
                "/supply/{supplyId}",
                out JsonElement withdraw))
            .IsTrue();
        await Assert.That(
                withdraw.TryGetProperty(
                    "delete",
                    out _))
            .IsTrue();
    }

    [Test]
    public async Task CanonicalSchemaKeepsStateBoundedAndPriorityFree()
    {
        using JsonDocument document =
            JsonDocument.Parse(
                await File.ReadAllTextAsync(
                    Path.Combine(
                        RepositoryRoot,
                        "schemas",
                        "openapi_islamu-event.json")));
        JsonElement schema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("FairReturnWaitlistDto");
        string[] properties = schema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await Assert.That(properties)
            .IsEquivalentTo([
                "id",
                "offerExpiresAt",
                "position",
                "reasonCode",
                "statusCode",
            ]);
        string raw = schema.GetRawText();
        foreach (string forbidden in new[]
                 {
                     "priority",
                     "paid",
                     "amount",
                     "currency",
                     "tenant",
                     "participant",
                     "seller",
                     "user",
                     "email",
                     "phone",
                     "name",
                     "address",
                     "answer",
                     "consent",
                     "payment",
                     "refund",
                     "capability",
                 })
        {
            await Assert.That(raw)
                .DoesNotContain(
                    forbidden,
                    StringComparison.OrdinalIgnoreCase);
        }
    }

    [Test]
    public async Task GeneratedClientCarriesHeaderOnlyAuthorityAndImmutableDto()
    {
        string source =
            await File.ReadAllTextAsync(
                Path.Combine(
                    RepositoryRoot,
                    "src",
                    "Explore.Blazor.Client",
                    "Clients",
                    "EventApiClient.g.cs"));
        await Assert.That(source).Contains(
            "GetFairReturnWaitlistAsync(System.Guid eventId, System.Guid registrationOrderId, System.Guid registrationOrderLineId, string? x_Registration_Order_Capability");
        await Assert.That(source).Contains(
            "JoinFairReturnWaitlistAsync(System.Guid eventId, System.Guid registrationOrderId, System.Guid registrationOrderLineId, string idempotency_Key, string? x_Registration_Order_Capability");
        await Assert.That(source).Contains(
            "LeaveFairReturnWaitlistAsync(System.Guid eventId, System.Guid registrationOrderId, System.Guid registrationOrderLineId, string idempotency_Key, string? x_Registration_Order_Capability");
        await Assert.That(source).Contains(
            "AcceptFairReturnOfferAsync(System.Guid eventId, System.Guid registrationOrderId, System.Guid registrationOrderLineId, System.Guid offerId, string idempotency_Key, string? x_Registration_Order_Capability");
        await Assert.That(source).Contains(
            "WithdrawFairReturnSupplyAsync(System.Guid eventId, System.Guid registrationOrderId, System.Guid registrationOrderLineId, System.Guid supplyId, string idempotency_Key, string? x_Registration_Order_Capability");
        string dto = ExtractType(
            source,
            "public partial record class " +
            "FairReturnWaitlistDto");
        await Assert.That(dto).Contains(
            "public System.Guid Id { get; init; }");
        await Assert.That(dto).Contains(
            "public int Position { get; init; }");
        await Assert.That(dto).Contains(
            "public string ReasonCode { get; init; }");
        await Assert.That(dto)
            .DoesNotContain("Priority");
        await Assert.That(dto)
            .DoesNotContain("Participant");
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
