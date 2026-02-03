// ABOUTME: Unit tests for RotationAwareHttpClientFactory.
// Tests client creation, credential rotation, atomic swap, and graceful disposal.

using Explore.Secrets.Configuration;
using Explore.Secrets.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Services;

public class RotationAwareHttpClientFactoryTests : IDisposable
{
    private readonly ILogger<RotationAwareHttpClientFactory> _logger;
    private RotationAwareHttpClientFactory? _factory;

    public RotationAwareHttpClientFactoryTests()
    {
        _logger = Substitute.For<ILogger<RotationAwareHttpClientFactory>>();
    }

    private RotationAwareHttpClientFactory CreateFactory(
        HttpClientCredentialOptions? credentials = null,
        RotationOptions? rotation = null)
    {
        credentials ??= new HttpClientCredentialOptions();
        rotation ??= new RotationOptions { Enabled = true, GracePeriod = TimeSpan.FromSeconds(1) };

        var credentialMonitor = CreateOptionsMonitor(credentials);
        var rotationMonitor = CreateOptionsMonitor(rotation);

        _factory = new RotationAwareHttpClientFactory(credentialMonitor, rotationMonitor, _logger);
        return _factory;
    }

    private static IOptionsMonitor<T> CreateOptionsMonitor<T>(T value)
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        return monitor;
    }

    // ==================== Constructor Tests ====================

    [Test]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange & Act
        var factory = CreateFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.ActiveClientCount.Should().Be(0);
    }

    [Test]
    public void Constructor_WithNullCredentialOptions_ShouldThrow()
    {
        // Arrange
        var rotationMonitor = CreateOptionsMonitor(new RotationOptions());

        // Act
        var act = () => new RotationAwareHttpClientFactory(null!, rotationMonitor, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialOptions");
    }

    [Test]
    public void Constructor_WithNullRotationOptions_ShouldThrow()
    {
        // Arrange
        var credentialMonitor = CreateOptionsMonitor(new HttpClientCredentialOptions());

        // Act
        var act = () => new RotationAwareHttpClientFactory(credentialMonitor, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("rotationOptions");
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Arrange
        var credentialMonitor = CreateOptionsMonitor(new HttpClientCredentialOptions());
        var rotationMonitor = CreateOptionsMonitor(new RotationOptions());

        // Act
        var act = () => new RotationAwareHttpClientFactory(credentialMonitor, rotationMonitor, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ==================== CreateClient Tests ====================

    [Test]
    public void CreateClient_WithNewName_ShouldCreateClient()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var client = factory.CreateClient("test-client");

        // Assert
        client.Should().NotBeNull();
        factory.ActiveClientCount.Should().Be(1);
        factory.HasClient("test-client").Should().BeTrue();
    }

    [Test]
    public void CreateClient_WithSameName_ShouldReturnSameClient()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var client1 = factory.CreateClient("test-client");
        var client2 = factory.CreateClient("test-client");

        // Assert
        client1.Should().BeSameAs(client2);
        factory.ActiveClientCount.Should().Be(1);
    }

    [Test]
    public void CreateClient_WithDifferentNames_ShouldCreateDifferentClients()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var client1 = factory.CreateClient("client-1");
        var client2 = factory.CreateClient("client-2");

        // Assert
        client1.Should().NotBeSameAs(client2);
        factory.ActiveClientCount.Should().Be(2);
    }

    [Test]
    public void CreateClient_WithCredentials_ShouldApplyCredentials()
    {
        // Arrange
        var credentials = new HttpClientCredentialOptions
        {
            Clients = new Dictionary<string, HttpClientCredential>
            {
                ["api-client"] = new HttpClientCredential
                {
                    BaseAddress = "https://api.example.com",
                    BearerToken = "test-token-123",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            }
        };
        var factory = CreateFactory(credentials);

        // Act
        var client = factory.CreateClient("api-client");

        // Assert
        client.BaseAddress.Should().Be(new Uri("https://api.example.com"));
        client.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        client.DefaultRequestHeaders.Authorization.Parameter.Should().Be("test-token-123");
        client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Test]
    public void CreateClient_WithApiKey_ShouldAddApiKeyHeader()
    {
        // Arrange
        var credentials = new HttpClientCredentialOptions
        {
            Clients = new Dictionary<string, HttpClientCredential>
            {
                ["api-client"] = new HttpClientCredential
                {
                    ApiKey = "my-api-key-123"
                }
            }
        };
        var factory = CreateFactory(credentials);

        // Act
        var client = factory.CreateClient("api-client");

        // Assert
        client.DefaultRequestHeaders.TryGetValues("X-API-Key", out var values).Should().BeTrue();
        values.Should().Contain("my-api-key-123");
    }

    [Test]
    public void CreateClient_WithCustomHeaders_ShouldAddHeaders()
    {
        // Arrange
        var credentials = new HttpClientCredentialOptions
        {
            Clients = new Dictionary<string, HttpClientCredential>
            {
                ["api-client"] = new HttpClientCredential
                {
                    Headers = new Dictionary<string, string>
                    {
                        ["X-Custom-Header"] = "custom-value",
                        ["X-Another-Header"] = "another-value"
                    }
                }
            }
        };
        var factory = CreateFactory(credentials);

        // Act
        var client = factory.CreateClient("api-client");

        // Assert
        client.DefaultRequestHeaders.TryGetValues("X-Custom-Header", out var values1).Should().BeTrue();
        values1.Should().Contain("custom-value");
        client.DefaultRequestHeaders.TryGetValues("X-Another-Header", out var values2).Should().BeTrue();
        values2.Should().Contain("another-value");
    }

    [Test]
    public void CreateClient_AfterDispose_ShouldThrow()
    {
        // Arrange
        var factory = CreateFactory();
        factory.Dispose();

        // Act
        var act = () => factory.CreateClient("test-client");

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    // ==================== ForceRotate Tests ====================

    [Test]
    public async Task ForceRotateAsync_WithExistingClient_ShouldRotate()
    {
        // Arrange
        var factory = CreateFactory();
        var originalClient = factory.CreateClient("test-client");
        factory.ActiveClientCount.Should().Be(1);

        // Act
        await factory.ForceRotateAsync("test-client");

        // Allow time for rotation
        await Task.Delay(100);

        // Assert
        var newClient = factory.CreateClient("test-client");
        newClient.Should().NotBeSameAs(originalClient);
    }

    [Test]
    public async Task ForceRotateAsync_WithNonExistentClient_ShouldThrow()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var act = () => factory.ForceRotateAsync("non-existent");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("name");
    }

    [Test]
    public async Task ForceRotateAsync_AfterDispose_ShouldThrow()
    {
        // Arrange
        var factory = CreateFactory();
        factory.CreateClient("test-client");
        factory.Dispose();

        // Act
        var act = () => factory.ForceRotateAsync("test-client");

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Test]
    public async Task ForceRotateAllAsync_ShouldRotateAllClients()
    {
        // Arrange
        var factory = CreateFactory();
        var client1 = factory.CreateClient("client-1");
        var client2 = factory.CreateClient("client-2");

        // Act
        await factory.ForceRotateAllAsync();
        await Task.Delay(100);

        // Assert
        var newClient1 = factory.CreateClient("client-1");
        var newClient2 = factory.CreateClient("client-2");
        newClient1.Should().NotBeSameAs(client1);
        newClient2.Should().NotBeSameAs(client2);
    }

    // ==================== HasClient Tests ====================

    [Test]
    public void HasClient_WithExistingClient_ShouldReturnTrue()
    {
        // Arrange
        var factory = CreateFactory();
        factory.CreateClient("test-client");

        // Act
        var result = factory.HasClient("test-client");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasClient_WithNonExistentClient_ShouldReturnFalse()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var result = factory.HasClient("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    // ==================== Dispose Tests ====================

    [Test]
    public void Dispose_ShouldClearClients()
    {
        // Arrange
        var factory = CreateFactory();
        factory.CreateClient("client-1");
        factory.CreateClient("client-2");
        factory.ActiveClientCount.Should().Be(2);

        // Act
        factory.Dispose();

        // Assert
        factory.ActiveClientCount.Should().Be(0);
    }

    [Test]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var factory = CreateFactory();
        factory.CreateClient("test-client");

        // Act
        var act = () =>
        {
            factory.Dispose();
            factory.Dispose();
            factory.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }
}
