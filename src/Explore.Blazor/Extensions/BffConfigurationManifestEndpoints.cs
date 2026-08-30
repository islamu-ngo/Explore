// ABOUTME: Proxies whole-instance configuration-manifest downloads through the authenticated BFF.
// ABOUTME: Revalidates HAL authority and buffers only exact, bounded, canonical API file responses.

using System.Net.Http.Headers;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.ControlPlane;
using Explore.Blazor.Client.Routing.ControlPlane;
using Explore.Blazor.Services.Preferences;

namespace Explore.Blazor.Extensions;

public static class BffConfigurationManifestEndpoints
{
    private const string MediaType =
        "application/vnd.islamu.configuration-manifest.v1alpha2+json";
    private const string OverridesFileName = "configuration-manifest-overrides.json";
    private const string PortableFileName = "configuration-manifest-portable.json";
    private const int MaximumBytes = 4 * 1024 * 1024;

    public static WebApplication MapConfigurationManifestEndpoints(
        this WebApplication app)
    {
        app.MapGet(
                ConfigurationManifestExportRoutes.BffExport,
                HandleDownloadAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleDownloadAsync(
        ConfigurationManifestExportView? view,
        HttpContext context,
        IEventApiClient apiClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache";
        context.Response.Headers.Pragma = "no-cache";

        ConfigurationManifestExportView requestedView =
            view ?? ConfigurationManifestExportView.Overrides;
        string relation = RelationFor(requestedView);
        string expectedFileName = FileNameFor(requestedView);
        var logger = loggerFactory.CreateLogger("ConfigurationManifestExport");

        try
        {
            HalResourceOfControlPlaneOverviewDto overview =
                await apiClient.GetControlPlaneOverviewAsync(
                    cancellationToken: cancellationToken);
            if (!HasGetCapability(overview, relation))
            {
                return Results.Problem(
                    title: "Configuration manifest export unavailable",
                    detail: "The current control-plane capabilities do not permit this export.",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            using FileResponse response =
                await apiClient.ExportConfigurationManifestAsync(
                    requestedView,
                    cancellationToken: cancellationToken);

            if (!HasExpectedContentType(response.Headers)
                || !HasExpectedFileName(response.Headers, expectedFileName))
            {
                logger.LogWarning(
                    "Rejected a configuration-manifest export response with invalid file metadata.");
                return InvalidDownstreamResponse();
            }

            byte[]? content = await ReadBoundedAsync(
                response.Stream,
                cancellationToken);
            if (content is null)
            {
                logger.LogWarning(
                    "Rejected a configuration-manifest export response exceeding the BFF byte limit.");
                return InvalidDownstreamResponse();
            }

            return Results.File(
                content,
                MediaType,
                expectedFileName,
                enableRangeProcessing: false);
        }
        catch (ApiException exception)
        {
            logger.LogWarning(
                "Configuration-manifest export forwarding failed. Status={StatusCode}",
                exception.StatusCode);
            return BffForwardingResults.Problem(
                exception,
                "The configuration manifest could not be downloaded.",
                "Configuration manifest export failed");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Configuration-manifest export forwarding failed. FailureType={FailureType}",
                exception.GetType().Name);
            return InvalidDownstreamResponse();
        }
    }

    private static bool HasGetCapability(
        HalResourceOfControlPlaneOverviewDto overview,
        string relation) =>
        overview._links?.TryGetValue(relation, out HalLink? link) == true
        && string.Equals(link.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);

    private static string RelationFor(ConfigurationManifestExportView view) =>
        view switch
        {
            ConfigurationManifestExportView.Overrides =>
                ControlPlaneLinkRelations.ExportConfigurationOverrides,
            ConfigurationManifestExportView.Portable =>
                ControlPlaneLinkRelations.ExportConfigurationPortable,
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unsupported export view.")
        };

    private static string FileNameFor(ConfigurationManifestExportView view) =>
        view switch
        {
            ConfigurationManifestExportView.Overrides => OverridesFileName,
            ConfigurationManifestExportView.Portable => PortableFileName,
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unsupported export view.")
        };

    private static bool HasExpectedContentType(
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        string? value = HeaderValue(headers, "Content-Type");
        return MediaTypeHeaderValue.TryParse(value, out MediaTypeHeaderValue? parsed)
            && string.Equals(parsed.MediaType, MediaType, StringComparison.Ordinal);
    }

    private static bool HasExpectedFileName(
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        string expectedFileName)
    {
        string? value = HeaderValue(headers, "Content-Disposition");
        if (!ContentDispositionHeaderValue.TryParse(
                value,
                out ContentDispositionHeaderValue? parsed))
        {
            return false;
        }

        string? fileName = parsed.FileNameStar ?? parsed.FileName;
        return string.Equals(
            fileName?.Trim('"'),
            expectedFileName,
            StringComparison.Ordinal);
    }

    private static string? HeaderValue(
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        string name) =>
        headers.FirstOrDefault(pair =>
                string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            .Value?
            .SingleOrDefault();

    private static async Task<byte[]?> ReadBoundedAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        byte[] buffer = GC.AllocateUninitializedArray<byte>(MaximumBytes + 1);
        var total = 0;
        while (total < buffer.Length)
        {
            int read = await source.ReadAsync(
                buffer.AsMemory(total, buffer.Length - total),
                cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total > MaximumBytes ? null : buffer[..total];
    }

    private static IResult InvalidDownstreamResponse() =>
        Results.Problem(
            title: "Configuration manifest export failed",
            detail: "The configuration service returned an invalid download response.",
            statusCode: StatusCodes.Status502BadGateway);
}
