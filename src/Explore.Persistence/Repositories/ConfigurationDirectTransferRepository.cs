// ABOUTME: Persists target-qualified direct-transfer sessions and encrypted resumable chunks.
// ABOUTME: Verifies duplicate ranges and assembled plaintext before returning an artifact to Application.

namespace Explore.Persistence.Repositories;

using System.Security.Cryptography;
using Explore.Application.Features.ConfigurationManifest.Importing;
using Explore.Application.Features.ConfigurationManifest.Managed;
using Explore.Domain;
using Explore.Persistence.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

public sealed class ConfigurationDirectTransferRepository(ExploreDbContext dbContext)
    : IConfigurationDirectTransferRepository
{
    public async Task AddAsync(
        ConfigurationDirectTransferSession session,
        CancellationToken cancellationToken) =>
        await dbContext.Set<ConfigurationDirectTransferSession>()
            .AddAsync(session, cancellationToken);

    public Task<ConfigurationDirectTransferSession?> GetForUpdateAsync(
        Guid sessionId,
        string targetAuthorityKey,
        CancellationToken cancellationToken) =>
        dbContext.Set<ConfigurationDirectTransferSession>()
            .SingleOrDefaultAsync(
                session => session.Id == sessionId
                    && session.TargetAuthorityKey == targetAuthorityKey,
                cancellationToken);

    public Task UpdateAsync(
        ConfigurationDirectTransferSession session,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        dbContext.Update(session);
        return Task.CompletedTask;
    }
}

public sealed class ConfigurationDirectTransferChunkStore(
    ExploreDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider)
    : IConfigurationDirectTransferChunkStore
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "ISLAMU.Event.ConfigurationDirectTransferChunk.v1");

    public async Task<bool> AddAsync(
        Guid sessionId,
        int offset,
        ReadOnlyMemory<byte> bytes,
        string digest,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        ConfigurationDirectTransferChunk? existing =
            await dbContext.Set<ConfigurationDirectTransferChunk>()
                .SingleOrDefaultAsync(
                    chunk => chunk.SessionId == sessionId && chunk.Offset == offset,
                    cancellationToken);
        if (existing is not null)
        {
            return existing.ByteLength == bytes.Length
                && string.Equals(existing.Digest, digest, StringComparison.Ordinal);
        }

        byte[] plaintext = bytes.ToArray();
        byte[] protectedPayload;
        try
        {
            protectedPayload = _protector.Protect(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
        await dbContext.Set<ConfigurationDirectTransferChunk>().AddAsync(
            ConfigurationDirectTransferChunk.Create(
                Guid.CreateVersion7(),
                sessionId,
                offset,
                bytes.Length,
                digest,
                protectedPayload,
                expiresAt),
            cancellationToken);
        return true;
    }

    public async Task<ReadOnlyMemory<byte>> AssembleAsync(
        Guid sessionId,
        int expectedByteLength,
        CancellationToken cancellationToken)
    {
        ConfigurationDirectTransferChunk[] stored =
            await dbContext.Set<ConfigurationDirectTransferChunk>()
                .AsNoTracking()
                .Where(chunk => chunk.SessionId == sessionId)
                .OrderBy(chunk => chunk.Offset)
                .ToArrayAsync(cancellationToken);
        byte[] assembled = GC.AllocateUninitializedArray<byte>(expectedByteLength);
        var nextOffset = 0;
        foreach (ConfigurationDirectTransferChunk chunk in stored)
        {
            byte[] plaintext = _protector.Unprotect(chunk.ProtectedPayload);
            try
            {
                if (chunk.Offset != nextOffset
                    || plaintext.Length != chunk.ByteLength
                    || nextOffset + plaintext.Length > assembled.Length
                    || !string.Equals(
                        ConfigurationImportDigest.ComputeBytes(plaintext),
                        chunk.Digest,
                        StringComparison.Ordinal))
                {
                    throw new ConfigurationImportSessionException(
                        ConfigurationImportFailureCodes.ArtifactIntegrityInvalid);
                }
                plaintext.CopyTo(assembled, nextOffset);
                nextOffset += plaintext.Length;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
        if (nextOffset != assembled.Length)
        {
            CryptographicOperations.ZeroMemory(assembled);
            throw new ConfigurationImportSessionException(
                ConfigurationImportFailureCodes.ArtifactIntegrityInvalid);
        }
        return assembled;
    }
}
