// ABOUTME: Breaks selected-authority Setup secret writes and purpose-specific HMAC commitments.
// ABOUTME: Proves exact-source dispatch, no fallback, canonical evidence, and value-free outcomes.

namespace Explore.Secrets.UnitTests.SetupLive;

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Application.Contracts.SetupLive;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Explore.Secrets.Configuration;
using Explore.Secrets.Abstractions;
using Explore.Secrets.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

public sealed class SetupSecretBindingAuthorityTests
{
    [Test]
    [Arguments(SecretProviderType.Infisical, SecretSourceType.Infisical,
        SetupSecretBindingWriteOutcome.Ready, 1)]
    [Arguments(SecretProviderType.Environment, SecretSourceType.EnvironmentVariable,
        SetupSecretBindingWriteOutcome.Unavailable, 0)]
    [Arguments(SecretProviderType.UserSecrets, SecretSourceType.EnvironmentVariable,
        SetupSecretBindingWriteOutcome.Unavailable, 0)]
    [Arguments(SecretProviderType.Infisical, SecretSourceType.EnvironmentVariable,
        SetupSecretBindingWriteOutcome.Invalid, 0)]
    [Arguments(SecretProviderType.Environment, SecretSourceType.Infisical,
        SetupSecretBindingWriteOutcome.Invalid, 0)]
    [Arguments(SecretProviderType.UserSecrets, SecretSourceType.Infisical,
        SetupSecretBindingWriteOutcome.Invalid, 0)]
    public async Task RegisteredWriterUsesOnlyTheSelectedStoredAuthority(
        SecretProviderType selectedProvider,
        SecretSourceType storedSource,
        SetupSecretBindingWriteOutcome expectedOutcome,
        int expectedWrites)
    {
        SecretBinding binding = Binding(storedSource);
        ISecretBindingRepository repository = Substitute.For<ISecretBindingRepository>();
        repository.GetById(binding.Id).Returns(binding);
        var client = new RecordingInfisicalClient();
        await using ServiceProvider provider = Services(
            selectedProvider,
            repository,
            new FixedInfisicalClientFactory(client))
            .BuildServiceProvider();
        ISetupSecretBindingWriter writer =
            provider.GetRequiredService<ISetupSecretBindingWriter>();
        byte[] secret = RandomNumberGenerator.GetBytes(64);
        try
        {
            var request = new SetupSecretBindingWriteRequest(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                1,
                Guid.CreateVersion7(),
                binding.Id,
                "setup.signing",
                secret);

            SetupSecretBindingWriteOutcome outcome = await writer.WriteAsync(
                request,
                CancellationToken.None);

            await Assert.That(outcome).IsEqualTo(expectedOutcome);
            await Assert.That(client.WriteCount).IsEqualTo(expectedWrites);
            await Assert.That(client.ReadCount).IsEqualTo(0);
            if (expectedWrites == 1)
            {
                await Assert.That(client.LastEnvironment)
                    .IsEqualTo(binding.InfisicalEnvironment);
                await Assert.That(client.LastPath).IsEqualTo(binding.InfisicalPath);
                await Assert.That(client.LastName).IsEqualTo(binding.InfisicalKey);
                await Assert.That(client.LastSecretDigest)
                    .IsEqualTo(Digest(secret));
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    [Test]
    public async Task RegisteredCommitmentAuthorityUsesCanonicalPurposeAndSelectedResolver()
    {
        byte[] keyBytes = RandomNumberGenerator.GetBytes(32);
        byte[] secret = RandomNumberGenerator.GetBytes(64);
        string key = Convert.ToBase64String(keyBytes);
        var resolver = new RecordingCommitmentResolver(key);
        ISecretBindingRepository repository = Substitute.For<ISecretBindingRepository>();
        await using ServiceProvider provider = Services(
            SecretProviderType.Environment,
            repository,
            new FixedInfisicalClientFactory(new RecordingInfisicalClient()),
            resolver)
            .BuildServiceProvider();
        ISetupSecretBindingCommitmentAuthority authority =
            provider.GetRequiredService<ISetupSecretBindingCommitmentAuthority>();
        var request = new SetupSecretBindingCommitmentRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            7,
            Guid.CreateVersion7(),
            "setup.encryption",
            secret);
        try
        {
            SetupSecretBindingCommitment commitment = await authority.CommitAsync(
                request,
                CancellationToken.None);

            await Assert.That(commitment.KeyVersion).IsEqualTo(1);
            await Assert.That(commitment.Commitment)
                .IsEqualTo(ExpectedCommitment(key, request));
            await Assert.That(resolver.CallCount).IsEqualTo(1);
            await Assert.That(resolver.LastSettingKey)
                .IsEqualTo(SetupSecretBindingContractMetadata.CommitmentAuthorityKey);
            await Assert.That(resolver.LastTenantId).IsNull();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    private static ServiceCollection Services(
        SecretProviderType provider,
        ISecretBindingRepository repository,
        IInfisicalClientFactory clientFactory,
        ISecretResolver? resolver = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<SecretProviderOptions>().Configure(options =>
        {
            options.Provider = provider;
            options.Infisical.Environment = "staging";
        });
        services.AddSingleton(repository);
        services.AddSingleton(clientFactory);
        if (resolver is not null)
            services.AddSingleton(resolver);
        services.AddSecretResolution();
        return services;
    }

    private static SecretBinding Binding(SecretSourceType sourceType)
    {
        var binding = new SecretBinding
        {
            Id = Guid.CreateVersion7(),
            SettingKey = "setup.signing",
            Scope = SecretScope.Instance,
            SourceType = sourceType
        };
        if (sourceType == SecretSourceType.Infisical)
        {
            binding.InfisicalEnvironment = "staging";
            binding.InfisicalPath = "/setup";
            binding.InfisicalKey = "SETUP_SIGNING";
        }
        else
        {
            binding.EnvironmentVariableName = "ISLAMU_SETUP_SIGNING";
        }

        return binding;
    }

    private static string ExpectedCommitment(
        string key,
        SetupSecretBindingCommitmentRequest request)
    {
        byte[] prefix = Encoding.UTF8.GetBytes(
            $"setup-secret-binding-commitment-v1\n{request.TenantId:D}\n{request.ActorId:D}\n{request.EnrollmentId:D}\n{request.EnrollmentGeneration}\n{request.OperationKey:D}\n{request.BindingKey}\n");
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        try
        {
            using IncrementalHash hmac = IncrementalHash.CreateHMAC(
                HashAlgorithmName.SHA256,
                keyBytes);
            hmac.AppendData(prefix);
            hmac.AppendData(request.SecretValue.Span);
            return Convert.ToHexString(hmac.GetHashAndReset()).ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(prefix);
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    private static string Digest(ReadOnlySpan<byte> value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private sealed class FixedInfisicalClientFactory(IInfisicalClient client) :
        IInfisicalClientFactory
    {
        public Task<IInfisicalClient?> GetClientAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IInfisicalClient?>(client);
    }

    private sealed class RecordingInfisicalClient : IInfisicalClient
    {
        private int _readCount;
        private int _writeCount;

        public int ReadCount => Volatile.Read(ref _readCount);
        public int WriteCount => Volatile.Read(ref _writeCount);
        public string? LastEnvironment { get; private set; }
        public string? LastPath { get; private set; }
        public string? LastName { get; private set; }
        public string? LastSecretDigest { get; private set; }

        public Task<string?> GetSecretAsync(
            string environment,
            string folderPath,
            string secretName,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _readCount);
            return Task.FromResult<string?>(null);
        }

        public Task<bool> WriteSecretAsync(
            string environment,
            string folderPath,
            string secretName,
            ReadOnlyMemory<byte> secretValue,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _writeCount);
            LastEnvironment = environment;
            LastPath = folderPath;
            LastName = secretName;
            LastSecretDigest = Digest(secretValue.Span);
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingCommitmentResolver(string value) : ISecretResolver
    {
        private int _callCount;
        public int CallCount => Volatile.Read(ref _callCount);
        public string? LastSettingKey { get; private set; }
        public Guid? LastTenantId { get; private set; }

        public Task<SecretResolutionResult> ResolveAsync(
            string settingKey,
            Guid? tenantId,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            LastSettingKey = settingKey;
            LastTenantId = tenantId;
            return Task.FromResult(SecretResolutionResult.Resolved(new ResolvedSecret(
                settingKey,
                value,
                SecretSourceType.EnvironmentVariable,
                SecretScope.Instance,
                ScopeId: null,
                DateTime.UtcNow)));
        }

        public Task<SecretResolutionResult> ResolveQualifiedAsync(
            string settingKey,
            SecretScope scope,
            Guid? scopeId,
            string qualifier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SecretResolutionResult.Invalid);

        public Task<SecretResolutionResult> ResolveTenantBindingAsync(
            Guid tenantId,
            Guid bindingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SecretResolutionResult.Invalid);

        public Task InvalidateAsync(
            string settingKey,
            SecretScope scope,
            Guid? scopeId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
