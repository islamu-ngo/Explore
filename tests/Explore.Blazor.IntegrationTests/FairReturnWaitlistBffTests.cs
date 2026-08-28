// ABOUTME: Defines RED browser trust-boundary contracts for fair-return waitlist lifecycle operations.
// ABOUTME: Pins generated-client forwarding, cookie authority, antiforgery, capability isolation, and no-store.

using System.Reflection;

namespace Explore.Blazor.IntegrationTests;

public sealed class FairReturnWaitlistBffTests
{
    private const string EndpointTypeName =
        "Explore.Blazor.Extensions." +
        "BffFairReturnWaitlistEndpoints";
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();
    private static readonly string EndpointPath =
        Path.Combine(
            RepositoryRoot,
            "src",
            "Explore.Blazor",
            "Extensions",
            "BffFairReturnWaitlistEndpoints.cs");

    [Test]
    public async Task EndpointFamilyIsMappedExplicitly()
    {
        Type? endpoints = typeof(Program)
            .Assembly.GetType(EndpointTypeName);

        await Assert.That(endpoints).IsNotNull();
        await Assert.That(endpoints!
                .GetMethod(
                    "MapFairReturnWaitlistEndpoints",
                    BindingFlags.Public
                    | BindingFlags.Static))
            .IsNotNull();
        string root = await ReadSourceAsync(
            "src/Explore.Blazor/Extensions/" +
            "BffEndpointExtensions.cs");
        await Assert.That(root).Contains(
            "MapFairReturnWaitlistEndpoints");
    }

    [Test]
    public async Task RoutesStayLineScopedAndUseGeneratedClientOnly()
    {
        await Assert.That(
                File.Exists(EndpointPath))
            .IsTrue();
        string source =
            await File.ReadAllTextAsync(
                EndpointPath);
        foreach (string segment in new[]
                 {
                     "/bff/events/{eventId:guid}/",
                     "registration-orders/",
                     "{registrationOrderId:guid}/lines/",
                     "{registrationOrderLineId:guid}/waitlist",
                 })
        {
            await Assert.That(source)
                .Contains(segment);
        }
        foreach (string operation in new[]
                 {
                     "GetFairReturnWaitlistAsync",
                     "JoinFairReturnWaitlistAsync",
                     "LeaveFairReturnWaitlistAsync",
                     "AcceptFairReturnOfferAsync",
                     "WithdrawFairReturnSupplyAsync",
                 })
        {
            await Assert.That(source)
                .Contains(operation);
        }
        await Assert.That(source)
            .DoesNotContain("HttpClient");
        await Assert.That(source)
            .DoesNotContain("/api/");
    }

    [Test]
    public async Task CapabilityRemainsHeaderOnlyAndNeverReturnsToBrowser()
    {
        string source =
            await File.ReadAllTextAsync(
                EndpointPath);
        await Assert.That(source).Contains(
            "X-Registration-Order-Capability");
        await Assert.That(source).Contains(
            "[FromHeader");
        await Assert.That(source)
            .DoesNotContain("[FromQuery");
        await Assert.That(source)
            .DoesNotContain("CapabilityToken");
        await Assert.That(source)
            .DoesNotContain("Bearer");
        await Assert.That(source)
            .DoesNotContain("AccessToken");
    }

    [Test]
    public async Task EveryMutationRequiresCookieAuthAndAntiforgery()
    {
        string source =
            await File.ReadAllTextAsync(
                EndpointPath);
        foreach (string route in new[]
                 {
                     "MapPost",
                     "MapDelete",
                 })
        {
            await Assert.That(source)
                .Contains(route);
        }
        await Assert.That(source).Contains(
            ".RequireAuthorization()");
        await Assert.That(source).Contains(
            ".ValidateAntiforgeryBeforeRateLimiting()");
        await Assert.That(source).Contains(
            "Idempotency-Key");
    }

    [Test]
    public async Task ResponsesArePrivateAndUpstreamFailuresAreBounded()
    {
        string source =
            await File.ReadAllTextAsync(
                EndpointPath);
        await Assert.That(source).Contains(
            "Headers.CacheControl");
        await Assert.That(source).Contains(
            "no-store");
        await Assert.That(source).Contains(
            "StatusCodes.Status502BadGateway");
        await Assert.That(source).Contains(
            "StatusCodes.Status429TooManyRequests");
        foreach (string forbidden in new[]
                 {
                     "exception.Response",
                     "exception.Message",
                     "ProviderPayload",
                     "PaymentInstrument",
                 })
        {
            await Assert.That(source)
                .DoesNotContain(forbidden);
        }
    }

    private static int Count(
        string source,
        string value)
    {
        int count = 0;
        int offset = 0;
        while ((offset = source.IndexOf(
                   value,
                   offset,
                   StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
    }

    private static async Task<string>
        ReadSourceAsync(string relativePath) =>
        await File.ReadAllTextAsync(
            Path.Combine(
                RepositoryRoot,
                relativePath));

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
