// ABOUTME: Unit tests for legacy S3-compatible object-storage service diagnostics.
// ABOUTME: Verifies connection probes fail closed without leaking endpoints or provider exception payloads.

using Amazon.S3;
using Amazon.S3.Model;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Infrastructure.Services;
using Explore.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class ObjectStorageServiceTests
{
    [Test]
    public async Task TestConnectionAsync_WhenProbeFails_LogsFailureTypeWithoutEndpointOrProviderPayload()
    {
        var config = CreateConfig();
        var configResolver = Substitute.For<IS3ConfigResolver>();
        var clientFactory = Substitute.For<IS3ClientFactory>();
        var s3Client = Substitute.For<IAmazonS3>();
        var logger = new TestListLogger<ObjectStorageService>();
        configResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(config);
        clientFactory.CreateDataClient(config).Returns(s3Client);
        s3Client.ListBucketsAsync(Arg.Any<CancellationToken>())
            .Returns<Task<ListBucketsResponse>>(_ => throw new InvalidOperationException(
                $"provider leaked endpoint {config.Endpoint} bucket {config.BucketName} secret {config.SecretAccessKey}"));
        var service = new ObjectStorageService(configResolver, clientFactory, logger);

        var result = await service.TestConnectionAsync(CancellationToken.None);

        await Assert.That(result).IsFalse();

        var log = logger.Entries.Single(entry => entry.Level == LogLevel.Warning);
        await Assert.That(log.Exception).IsNull();
        await Assert.That(log.Message).Contains("FailureType=provider_unavailable");
        await Assert.That(log.Message).DoesNotContain("provider leaked endpoint");
        await Assert.That(log.Message).DoesNotContain(config.Endpoint);
        await Assert.That(log.Message).DoesNotContain(config.BucketName);
        await Assert.That(log.Message).DoesNotContain(config.SecretAccessKey);
    }

    private static S3Configuration CreateConfig()
        => new()
        {
            Endpoint = "https://s3.secret.example.test/private?token=storage-secret",
            BucketName = "tenant-private-bucket",
            AccessKeyId = "test-access-key",
            SecretAccessKey = "test-secret-key",
            Region = "us-east-1",
            ForcePathStyle = true
        };
}
