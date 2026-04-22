using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Explore.API.Services;

/// <summary>
/// Background service that exports the OpenAPI specification to a JSON file at startup.
/// This enables NSwag client generation in Blazor.Client to use the latest API schema.
///
/// The swagger.json file is exported to the project directory and can be referenced
/// by Blazor.Client's OpenApiReference for client generation.
/// </summary>
public class OpenApiExportService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OpenApiExportService> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _lifetime;

    public OpenApiExportService(
        IServiceProvider serviceProvider,
        ILogger<OpenApiExportService> logger,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration,
        IHostApplicationLifetime lifetime)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("OpenAPI export skipped - not in Development environment");
            return;
        }

        // Wait for Kestrel to finish binding all listeners before fetching from self.
        using var startCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        startCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var reg = _lifetime.ApplicationStarted.Register(() => tcs.TrySetResult());
            await tcs.Task.WaitAsync(startCts.Token);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timed out waiting for application startup; skipping OpenAPI export");
            return;
        }

        try
        {
            await ExportOpenApiSpecAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export OpenAPI specification");
        }
    }

    private async Task ExportOpenApiSpecAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var baseUrl = _configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault()
                      ?? "https://localhost:7039";

        var openApiSchemaUri = $"{baseUrl}/openapi/event-api.json";

        _logger.LogInformation("Fetching OpenAPI spec from {Url}", openApiSchemaUri);

        const int maxRetries = 5;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
                    ConnectTimeout = TimeSpan.FromSeconds(5),
                    SslOptions = { RemoteCertificateValidationCallback = (_, _, _, _) => true }
                };
                using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

                var response = await httpClient.GetAsync(openApiSchemaUri, stoppingToken);
                response.EnsureSuccessStatusCode();

                var swaggerJson = await response.Content.ReadAsStringAsync(stoppingToken);

                var jsonDoc = JsonDocument.Parse(swaggerJson);
                var prettyJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var projectDir = _hostEnvironment.ContentRootPath;
                var outputPath = Path.Combine(projectDir, "swagger.json");

                await File.WriteAllTextAsync(outputPath, prettyJson, stoppingToken);

                _logger.LogInformation("OpenAPI spec exported to {Path}", outputPath);

                return;
            }
            catch (HttpRequestException ex) when (i < maxRetries - 1)
            {
                _logger.LogWarning("Attempt {Attempt}/{MaxRetries} failed: {Message}. Retrying...",
                    i + 1, maxRetries, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        _logger.LogError("Failed to fetch OpenAPI spec after {MaxRetries} attempts", maxRetries);
    }
}
