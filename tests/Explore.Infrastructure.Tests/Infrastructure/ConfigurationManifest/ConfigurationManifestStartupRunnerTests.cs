// ABOUTME: Verifies deterministic configuration-manifest discovery and application during host startup.
// ABOUTME: Covers no-op, apply failure, cancellation, and safe diagnostic boundaries without filesystem I/O.

namespace Explore.Infrastructure.Tests.Infrastructure.ConfigurationManifest;

using System.Text.Json;
using Explore.Application.Features.ConfigurationManifest.Contracts;
using Explore.Application.Features.ConfigurationManifest.Application;
using Explore.Application.Features.ConfigurationManifest.Ingestion;
using Explore.Application.Responses;
using Explore.Infrastructure.ConfigurationManifest;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

public sealed class ConfigurationManifestStartupRunnerTests
{
    [Test]
    public async Task RunAsync_NoDiscoveredManifest_DoesNotInvokeApplication()
    {
        var reader = Substitute.For<IConfigurationManifestReader>();
        var applier = Substitute.For<IConfigurationManifestApplier>();
        var cancellationToken = new CancellationTokenSource().Token;
        reader.ReadAsync(
                Arg.Any<ConfigurationManifestReadOptions>(),
                cancellationToken)
            .Returns((ConfigurationManifestReadResult?)null);
        var runner = CreateRunner(
            ConfigurationManifestMode.Off,
            "relative-path-is-ignored.json",
            reader,
            applier);

        await runner.RunAsync(cancellationToken);

        await reader.Received(1).ReadAsync(
            Arg.Is<ConfigurationManifestReadOptions>(request =>
                request.Mode == ConfigurationManifestMode.Off
                && request.ConfiguredPath == "relative-path-is-ignored.json"),
            cancellationToken);
        await applier.DidNotReceiveWithAnyArgs().ApplyAsync(default!, default);
    }

    [Test]
    public async Task RunAsync_DiscoveredManifest_AppliesExactReadResultOnce()
    {
        ConfigurationManifestReadResult source = CreateSource(
            ConfigurationManifestMode.ValidateOnly);
        var reader = Substitute.For<IConfigurationManifestReader>();
        var applier = Substitute.For<IConfigurationManifestApplier>();
        var cancellationToken = new CancellationTokenSource().Token;
        reader.ReadAsync(
                Arg.Any<ConfigurationManifestReadOptions>(),
                cancellationToken)
            .Returns(source);
        applier.ApplyAsync(source, cancellationToken)
            .Returns(BaseCommandResponse.Success(Guid.CreateVersion7(), "validated"));
        var runner = CreateRunner(
            ConfigurationManifestMode.ValidateOnly,
            ConfigurationManifestOptions.ConventionPath,
            reader,
            applier);

        await runner.RunAsync(cancellationToken);

        await applier.Received(1).ApplyAsync(source, cancellationToken);
    }

    [Test]
    public async Task RunAsync_ApplicationFailure_ThrowsSafeStructuredStartupException()
    {
        ConfigurationManifestReadResult source = CreateSource(
            ConfigurationManifestMode.Bootstrap);
        Guid operationId = Guid.CreateVersion7();
        var reader = Substitute.For<IConfigurationManifestReader>();
        var applier = Substitute.For<IConfigurationManifestApplier>();
        reader.ReadAsync(
                Arg.Any<ConfigurationManifestReadOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(source);
        applier.ApplyAsync(source, Arg.Any<CancellationToken>())
            .Returns(BaseCommandResponse.Failure<Guid>(
                "configuration_manifest_preflight_failed",
                "unsafe-sensitive-detail",
                id: operationId));
        var runner = CreateRunner(
            ConfigurationManifestMode.Bootstrap,
            "/private/operator/configuration-manifest.json",
            reader,
            applier);

        ConfigurationManifestStartupException exception =
            await Assert.ThrowsAsync<ConfigurationManifestStartupException>(
                () => runner.RunAsync(CancellationToken.None));

        await Assert.That(exception.OperationId).IsEqualTo(operationId);
        await Assert.That(exception.FailureCode).IsEqualTo(
            "configuration_manifest_preflight_failed");
        await Assert.That(exception.Message).DoesNotContain(
            "unsafe-sensitive-detail");
        await Assert.That(exception.Message).DoesNotContain(
            "/private/operator");
    }

    [Test]
    public async Task RunAsync_ReaderCancellation_PropagatesWithoutApplication()
    {
        var reader = Substitute.For<IConfigurationManifestReader>();
        var applier = Substitute.For<IConfigurationManifestApplier>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        reader.ReadAsync(
                Arg.Any<ConfigurationManifestReadOptions>(),
                cancellation.Token)
            .Returns<Task<ConfigurationManifestReadResult?>>(
                _ => throw new OperationCanceledException(cancellation.Token));
        var runner = CreateRunner(
            ConfigurationManifestMode.Bootstrap,
            null,
            reader,
            applier);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => runner.RunAsync(cancellation.Token));
        await applier.DidNotReceiveWithAnyArgs().ApplyAsync(default!, default);
    }

    [Test]
    public async Task RunAsync_InvalidManifestFile_FailsClosedBeforeApplication()
    {
        var reader = Substitute.For<IConfigurationManifestReader>();
        var applier = Substitute.For<IConfigurationManifestApplier>();
        reader.ReadAsync(
                Arg.Any<ConfigurationManifestReadOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<ConfigurationManifestReadResult?>>(
                _ => throw new ConfigurationManifestIngestionException(
                    ConfigurationManifestIngestionFailureCodes.FileMissing,
                    "The configured configuration manifest file does not exist."));
        var runner = CreateRunner(
            ConfigurationManifestMode.Bootstrap,
            "/private/operator/missing.json",
            reader,
            applier);

        ConfigurationManifestIngestionException exception =
            await Assert.ThrowsAsync<ConfigurationManifestIngestionException>(
                () => runner.RunAsync(CancellationToken.None));

        await Assert.That(exception.FailureCode).IsEqualTo(
            ConfigurationManifestIngestionFailureCodes.FileMissing);
        await Assert.That(exception.Message).DoesNotContain("/private/operator");
        await applier.DidNotReceiveWithAnyArgs().ApplyAsync(default!, default);
    }

    private static ConfigurationManifestStartupRunner CreateRunner(
        ConfigurationManifestMode mode,
        string? path,
        IConfigurationManifestReader reader,
        IConfigurationManifestApplier applier) =>
        new(
            Options.Create(new ConfigurationManifestOptions
            {
                Mode = mode,
                Path = path
            }),
            reader,
            applier,
            NullLogger<ConfigurationManifestStartupRunner>.Instance);

    private static ConfigurationManifestReadResult CreateSource(
        ConfigurationManifestMode mode) =>
        new(
            new ConfigurationManifestV1Alpha1
            {
                Schema = ConfigurationManifestContractMetadata.SchemaId,
                ApiVersion = ConfigurationManifestContractMetadata.ApiVersion,
                Kind = ConfigurationManifestContractMetadata.Kind,
                Metadata = new ConfigurationManifestMetadataV1Alpha1
                {
                    Name = "startup-test"
                },
                Spec = new ConfigurationManifestSpecV1Alpha1
                {
                    Instance = new ConfigurationManifestInstanceV1Alpha1
                    {
                        Settings = new Dictionary<string, JsonElement>(
                            StringComparer.Ordinal),
                        Documents =
                            new Dictionary<string, ConfigurationManifestDocumentV1Alpha1>(
                                StringComparer.Ordinal)
                    },
                    Tenants = []
                }
            },
            mode,
            new string('a', 64),
            ByteLength: 128);
}
