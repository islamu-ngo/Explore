// ABOUTME: Mailpit Testcontainers fixture for E2E SMTP capture assertions.
// ABOUTME: Provides SMTP endpoint wiring and safe message inspection through Mailpit's HTTP API.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class MailpitContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string MailpitImage = "axllent/mailpit:v1.30.0";
    private const ushort SmtpContainerPort = 1025;
    private const ushort HttpContainerPort = 8025;
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);

    private IContainer? _container;

    public string SmtpHost => _container?.Hostname
        ?? throw new InvalidOperationException("Mailpit container not started");

    public int SmtpPort => _container?.GetMappedPublicPort(SmtpContainerPort)
        ?? throw new InvalidOperationException("Mailpit container not started");

    public string ApiBaseUrl => _container is null
        ? throw new InvalidOperationException("Mailpit container not started")
        : $"http://{_container.Hostname}:{_container.GetMappedPublicPort(HttpContainerPort)}";

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder(MailpitImage)
            .WithPortBinding(SmtpContainerPort, true)
            .WithPortBinding(HttpContainerPort, true)
            .Build();

        using var startupCts = new CancellationTokenSource(StartupTimeout);
        await _container.StartAsync(startupCts.Token);
        await WaitForApiAsync(startupCts.Token);
    }

    public async Task ClearMessagesAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateHttpClient();
        using var response = await client.DeleteAsync("/api/v1/messages", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<MailpitMessageSummary>> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateHttpClient();
        var response = await client.GetFromJsonAsync<MailpitMessagesResponse>("/api/v1/messages", cancellationToken);
        return response?.Messages ?? [];
    }

    public async Task<string> GetMessageTextAsync(string id, CancellationToken cancellationToken = default)
    {
        var message = await GetMessageAsync(id, cancellationToken);
        return message.Text;
    }

    public async Task<string> GetMessageHtmlAsync(string id, CancellationToken cancellationToken = default)
    {
        var message = await GetMessageAsync(id, cancellationToken);
        return message.Html;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private HttpClient CreateHttpClient() => new()
    {
        BaseAddress = new Uri(ApiBaseUrl)
    };

    private async Task<MailpitMessage> GetMessageAsync(string id, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();
        var messageId = string.IsNullOrWhiteSpace(id) ? "latest" : Uri.EscapeDataString(id);
        var message = await client.GetFromJsonAsync<MailpitMessage>($"/api/v1/message/{messageId}", cancellationToken);
        return message ?? throw new InvalidOperationException($"Mailpit message '{messageId}' was not returned by the API.");
    }

    private async Task WaitForApiAsync(CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var response = await client.GetAsync("/api/v1/messages", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Mailpit is still binding its HTTP listener; retry until the startup token expires.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        throw new TimeoutException("Mailpit HTTP API did not become ready before the startup timeout elapsed.");
    }

    private sealed class MailpitMessagesResponse
    {
        public IReadOnlyList<MailpitMessageSummary> Messages { get; init; } = [];

        [JsonPropertyName("messages_count")]
        public int MessagesCount { get; init; }
    }

    public sealed class MailpitMessageSummary
    {
        [JsonPropertyName("ID")]
        public string Id { get; init; } = string.Empty;

        public string Subject { get; init; } = string.Empty;

        public MailpitAddress From { get; init; } = new();

        public IReadOnlyList<MailpitAddress> To { get; init; } = [];
    }

    private sealed class MailpitMessage
    {
        [JsonPropertyName("Text")]
        public string Text { get; init; } = string.Empty;

        [JsonPropertyName("HTML")]
        public string Html { get; init; } = string.Empty;
    }

    public sealed class MailpitAddress
    {
        public string Address { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;
    }
}
