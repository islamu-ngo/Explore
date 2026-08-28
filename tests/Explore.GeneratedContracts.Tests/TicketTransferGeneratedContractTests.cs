// ABOUTME: Verifies generated ticket-transfer operations preserve exact routes and header-only capabilities.
// ABOUTME: Pins bounded HAL state, authorized actions, and one-time claim/credential response envelopes.

using System.Text.Json;

namespace Explore.GeneratedContracts.Tests;

public sealed class TicketTransferGeneratedContractTests
{
    private const string RootPath =
        "/api/events/{eventId}/admission-tickets/" +
        "{admissionTicketId}/transfers";
    private const string ItemPath =
        RootPath + "/{transferId}";
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();

    [Test]
    public async Task CanonicalSchemaPublishesExactTransferLifecycle()
    {
        using JsonDocument document = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    RepositoryRoot,
                    "schemas",
                    "openapi_islamu-event.json")));
        JsonElement paths =
            document.RootElement.GetProperty("paths");

        await Assert.That(paths.TryGetProperty(
                RootPath,
                out JsonElement root))
            .IsTrue();
        await Assert.That(
                root.TryGetProperty("post", out _))
            .IsTrue();
        await Assert.That(paths.TryGetProperty(
                ItemPath,
                out JsonElement item))
            .IsTrue();
        await Assert.That(
                item.TryGetProperty("get", out _))
            .IsTrue();
        await Assert.That(
                item.TryGetProperty("delete", out _))
            .IsTrue();
        foreach (string suffix in new[]
                 {
                     "/accept",
                     "/correction",
                     "/reissue",
                 })
        {
            await Assert.That(paths.TryGetProperty(
                    ItemPath + suffix,
                    out JsonElement action))
                .IsTrue();
            await Assert.That(
                    action.TryGetProperty("post", out _))
                .IsTrue();
        }

        JsonElement read =
            item.GetProperty("get");
        bool capabilityHeader = read
            .GetProperty("parameters")
            .EnumerateArray()
            .Any(parameter =>
                parameter.GetProperty("name").GetString() ==
                "X-Ticket-Transfer-Capability"
                && parameter.GetProperty("in").GetString() ==
                "header");
        await Assert.That(capabilityHeader).IsTrue();
        await Assert.That(
                document.RootElement.GetRawText())
            .DoesNotContain(
                "capability={",
                StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task CanonicalSchemaKeepsTransferStateBounded()
    {
        using JsonDocument document = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    RepositoryRoot,
                    "schemas",
                    "openapi_islamu-event.json")));
        JsonElement schema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("TicketTransferDto");
        string[] properties = schema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await Assert.That(properties).IsEquivalentTo(
            new[]
            {
                "admissionTicketId",
                "credentialGeneration",
                "expiresAt",
                "id",
                "statusCode",
                "supportCode",
                "transferHop",
            });
        string raw = schema.GetRawText();
        foreach (string forbidden in new[]
                 {
                     "participant",
                     "subject",
                     "email",
                     "phone",
                     "name",
                     "answer",
                     "consent",
                     "approval",
                     "payment",
                     "refund",
                     "digest",
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
    public async Task GeneratedClientCarriesTransferOperationsAndSecrets()
    {
        string source = await File.ReadAllTextAsync(
            Path.Combine(
                RepositoryRoot,
                "src",
                "Explore.Blazor.Client",
                "Clients",
                "EventApiClient.g.cs"));

        await Assert.That(source).Contains(
            "GetTicketTransferAsync(System.Guid eventId, System.Guid admissionTicketId, System.Guid transferId, string? x_Ticket_Transfer_Capability");
        await Assert.That(source).Contains(
            "OfferTicketTransferAsync(System.Guid eventId, System.Guid admissionTicketId");
        await Assert.That(source).Contains(
            "AcceptTicketTransferAsync(System.Guid eventId, System.Guid admissionTicketId, System.Guid transferId, AcceptTicketTransferRequest body, string? x_Ticket_Transfer_Capability");
        await Assert.That(source).Contains(
            "CancelTicketTransferAsync(System.Guid eventId, System.Guid admissionTicketId, System.Guid transferId");
        await Assert.That(source).Contains(
            "CorrectTicketTransferAsync(System.Guid eventId, System.Guid admissionTicketId, System.Guid transferId");
        await Assert.That(source).Contains(
            "ReissueTransferredTicketAsync(System.Guid eventId, System.Guid admissionTicketId, System.Guid transferId");

        string dto = ExtractType(
            source,
            "public partial record class TicketTransferDto");
        await Assert.That(dto).Contains(
            "public System.Guid AdmissionTicketId { get; init; }");
        await Assert.That(dto).Contains(
            "public string StatusCode { get; init; }");
        await Assert.That(dto).Contains(
            "public string SupportCode { get; init; }");
        await Assert.That(dto).Contains(
            "public int TransferHop { get; init; }");
        await Assert.That(dto).Contains(
            "public int CredentialGeneration { get; init; }");
        await Assert.That(dto).DoesNotContain(
            "ParticipantId");
        await Assert.That(dto).DoesNotContain(
            "CapabilityDigest");

        string offer = ExtractType(
            source,
            "public partial record class TicketTransferOfferResponse");
        await Assert.That(offer).Contains(
            "public string ClaimCapability { get; init; }");
        string credential = ExtractType(
            source,
            "public partial record class TicketTransferCredentialResponse");
        await Assert.That(credential).Contains(
            "public string Credential { get; init; }");
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
