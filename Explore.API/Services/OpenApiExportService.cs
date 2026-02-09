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

    public OpenApiExportService(
        IServiceProvider serviceProvider,
        ILogger<OpenApiExportService> logger,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Only export in Development environment
        if (!_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("OpenAPI export skipped - not in Development environment");
            return;
        }

        // Wait a bit for the API to fully start
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

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

        // Get the HttpClient to fetch swagger from our own endpoint
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient();

        // Get the base URL from configuration or use default
        var baseUrl = _configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault()
                      ?? "https://localhost:7039";

        // Use native OpenAPI endpoint. Swashbuckle has version incompatibility with .NET 10's
        // Microsoft.OpenApi 2.x library. The native endpoint works, and HalSchemaTransformer
        // can be enabled once .NET 10 Preview 4+ is available with AddSchemaTransformer API.
        var openApiSchemaUri = $"{baseUrl}/openapi/explore-api.json";

        _logger.LogInformation("Fetching OpenAPI spec from {Url}", openApiSchemaUri);

        // Retry a few times as the swagger endpoint might not be ready immediately
        const int maxRetries = 5;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // Skip SSL validation for localhost
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                using var httpClient = new HttpClient(handler);

                var response = await httpClient.GetAsync(openApiSchemaUri, stoppingToken);
                response.EnsureSuccessStatusCode();

                var swaggerJson = await response.Content.ReadAsStringAsync(stoppingToken);

                // Pretty-print the JSON
                var jsonDoc = JsonDocument.Parse(swaggerJson);
                var prettyJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Determine output path - project directory
                var projectDir = _hostEnvironment.ContentRootPath;
                var outputPath = Path.Combine(projectDir, "swagger.json");

                await File.WriteAllTextAsync(outputPath, prettyJson, stoppingToken);

                _logger.LogInformation("? OpenAPI spec exported to {Path}", outputPath);
                _logger.LogInformation("   Blazor.Client can now regenerate the API client");

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
