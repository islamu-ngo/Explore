// ABOUTME: Unit tests for the local filesystem storage provider.
// ABOUTME: Verifies server-generated keys, root containment, stream reads, delete idempotency, and health checks.

using System.Text;
using Explore.Application.Models.Storage;
using Explore.Domain;
using Explore.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class LocalFileStorageProviderTests
{
    [Test]
    public async Task WriteAsync_GeneratesTenantScopedObjectKeyAndChecksum()
    {
        var provider = CreateProvider(out var root);

        try
        {
            var tenantId = Guid.CreateVersion7();
            await using var content = new MemoryStream(Encoding.UTF8.GetBytes("storage payload"));

            var result = await provider.WriteAsync(
                new FileStorageWriteInput(
                    tenantId,
                    content,
                    "text/plain",
                    "payload.txt",
                    ".txt",
                    ExpectedSizeBytes: 15,
                    MaxSizeBytes: 1024),
                CancellationToken.None);

            await Assert.That(result.Provider).IsEqualTo(StorageProviders.Local);
            await Assert.That(result.ObjectKey).StartsWith($"tenants/{tenantId:N}/");
            await Assert.That(result.ObjectKey).EndsWith(".txt");
            await Assert.That(result.SizeBytes).IsEqualTo(15);
            await Assert.That(result.Sha256Checksum).IsEqualTo("5e1c7766758f09dc15399c4c444a9c5734cf49bda9797f31c2f42af3be2fbbaa");
            await Assert.That(File.Exists(provider.ResolveObjectPath(result.ObjectKey))).IsTrue();
        }
        finally
        {
            DeleteRootIfExists(root);
        }
    }

    [Test]
    public async Task OpenReadAsync_ReturnsStoredBytesAndContentType()
    {
        var provider = CreateProvider(out var root);

        try
        {
            await using var content = new MemoryStream(Encoding.UTF8.GetBytes("read me"));
            var writeResult = await provider.WriteAsync(
                new FileStorageWriteInput(Guid.CreateVersion7(), content, "text/plain", "read.txt", ".txt", 7, 1024),
                CancellationToken.None);

            var readResult = await provider.OpenReadAsync(
                new FileStorageReadInput(writeResult.ObjectKey, "text/plain"),
                CancellationToken.None);

            await using (readResult.Content)
            {
                using var reader = new StreamReader(readResult.Content, Encoding.UTF8);
                await Assert.That(await reader.ReadToEndAsync()).IsEqualTo("read me");
            }

            await Assert.That(readResult.ContentType).IsEqualTo("text/plain");
            await Assert.That(readResult.Length).IsEqualTo(7);
        }
        finally
        {
            DeleteRootIfExists(root);
        }
    }

    [Test]
    public async Task DeleteAsync_WhenCalledTwice_IsIdempotent()
    {
        var provider = CreateProvider(out var root);

        try
        {
            await using var content = new MemoryStream(Encoding.UTF8.GetBytes("delete me"));
            var writeResult = await provider.WriteAsync(
                new FileStorageWriteInput(Guid.CreateVersion7(), content, "text/plain", "delete.txt", ".txt", 9, 1024),
                CancellationToken.None);

            var firstDelete = await provider.DeleteAsync(new FileStorageDeleteInput(writeResult.ObjectKey), CancellationToken.None);
            var secondDelete = await provider.DeleteAsync(new FileStorageDeleteInput(writeResult.ObjectKey), CancellationToken.None);

            await Assert.That(firstDelete.Deleted).IsTrue();
            await Assert.That(secondDelete.Deleted).IsFalse();
        }
        finally
        {
            DeleteRootIfExists(root);
        }
    }

    [Test]
    public async Task ResolveObjectPath_WithTraversalKey_ThrowsArgumentException()
    {
        var provider = CreateProvider(out var root);

        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                _ = provider.ResolveObjectPath("tenants/../../secret.txt");
                return Task.CompletedTask;
            });
        }
        finally
        {
            DeleteRootIfExists(root);
        }
    }

    [Test]
    public async Task TestAsync_WithWritableRoot_ReturnsAvailable()
    {
        var provider = CreateProvider(out var root);

        try
        {
            var status = await provider.TestAsync(CancellationToken.None);

            await Assert.That(status.Provider).IsEqualTo(StorageProviders.Local);
            await Assert.That(status.IsAvailable).IsTrue();
            await Assert.That(status.SupportsServerSideStreaming).IsTrue();
            await Assert.That(status.SupportsBrowserDirectUpload).IsFalse();
        }
        finally
        {
            DeleteRootIfExists(root);
        }
    }

    private static LocalFileStorageProvider CreateProvider(out string root)
    {
        root = Path.Combine(Path.GetTempPath(), $"islamu-local-storage-tests-{Guid.NewGuid():N}");
        return new LocalFileStorageProvider(
            Options.Create(new LocalFileStorageOptions
            {
                RootPath = root
            }),
            NullLogger<LocalFileStorageProvider>.Instance);
    }

    private static void DeleteRootIfExists(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
