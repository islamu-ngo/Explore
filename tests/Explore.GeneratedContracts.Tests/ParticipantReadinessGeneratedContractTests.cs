// ABOUTME: Verifies generated readiness operations preserve exact scope and PII-minimal state.
// ABOUTME: Pins optional capability input, mutation methods, HAL links, and bounded response fields.

using System.Text.Json;

namespace Explore.GeneratedContracts.Tests;

public sealed class ParticipantReadinessGeneratedContractTests
{
    private const string ReadinessPath =
        "/api/events/{eventId}/participant-readiness/" +
        "registration-orders/{orderId}/participants/" +
        "{participantId}/assignments/{assignmentId}";
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();

    [Test]
    public async Task CanonicalSchemaPublishesExactReadAndActions()
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
                ReadinessPath,
                out JsonElement readiness))
            .IsTrue();
        await Assert.That(readiness.TryGetProperty(
                "get",
                out JsonElement read))
            .IsTrue();
        await Assert.That(paths.TryGetProperty(
                $"{ReadinessPath}/complete",
                out _))
            .IsTrue();
        await Assert.That(paths.TryGetProperty(
                $"{ReadinessPath}/approve",
                out _))
            .IsTrue();
        await Assert.That(paths.TryGetProperty(
                $"{ReadinessPath}/revoke",
                out _))
            .IsTrue();
        bool optionalCapability = read
            .GetProperty("parameters")
            .EnumerateArray()
            .Any(parameter =>
                parameter.GetProperty("name").GetString() ==
                "X-Registration-Order-Capability"
                && parameter.GetProperty("in").GetString() ==
                "header"
                && (!parameter.TryGetProperty(
                        "required",
                        out JsonElement required)
                    || !required.GetBoolean()));
        await Assert.That(optionalCapability).IsTrue();

        JsonElement properties = schemas
            .GetProperty("ParticipantReadinessDto")
            .GetProperty("properties");
        string[] expected =
        [
            "registrationTicketAssignmentId",
            "statusCode",
            "supportCode",
            "activeAdmissionAvailable",
        ];
        await Assert.That(properties
                .EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal)
                .ToArray())
            .IsEquivalentTo(expected);
        JsonElement hal = schemas
            .GetProperty(
                "HalResourceOfParticipantReadinessDto")
            .GetProperty("properties");
        await Assert.That(hal.TryGetProperty(
                "_links",
                out _))
            .IsTrue();
        await Assert.That(hal.TryGetProperty(
                "statusCode",
                out _))
            .IsTrue();
    }

    [Test]
    public async Task GeneratedClientCarriesOnlyBoundedReadinessContract()
    {
        string source = await File.ReadAllTextAsync(
            Path.Combine(
                RepositoryRoot,
                "src",
                "Explore.Blazor.Client",
                "Clients",
                "EventApiClient.g.cs"));

        await Assert.That(source).Contains(
            "GetParticipantReadinessAsync(System.Guid eventId, System.Guid orderId, System.Guid participantId, System.Guid assignmentId, string? x_Registration_Order_Capability");
        await Assert.That(source).Contains(
            "CompleteParticipantReadinessAsync(System.Guid eventId, System.Guid orderId, System.Guid participantId, System.Guid assignmentId");
        await Assert.That(source).Contains(
            "ApproveParticipantReadinessAsync(System.Guid eventId, System.Guid orderId, System.Guid participantId, System.Guid assignmentId");
        await Assert.That(source).Contains(
            "RevokeParticipantReadinessAsync(System.Guid eventId, System.Guid orderId, System.Guid participantId, System.Guid assignmentId");

        string dto = ExtractType(
            source,
            "public partial record class ParticipantReadinessDto");
        await Assert.That(dto).Contains(
            "public System.Guid RegistrationTicketAssignmentId { get; init; }");
        await Assert.That(dto).Contains(
            "public string StatusCode { get; init; }");
        await Assert.That(dto).Contains(
            "public string SupportCode { get; init; }");
        await Assert.That(dto).Contains(
            "public bool ActiveAdmissionAvailable { get; init; }");
        await Assert.That(dto).DoesNotContain(
            "public string Email");
        await Assert.That(dto).DoesNotContain(
            "public string Phone");
        await Assert.That(dto).DoesNotContain(
            "public string Name");
        await Assert.That(dto).DoesNotContain(
            "public string Answer");
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
