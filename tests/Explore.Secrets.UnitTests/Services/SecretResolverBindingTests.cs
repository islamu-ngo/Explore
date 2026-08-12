// ABOUTME: Unit tests exact SecretBinding-id resolution for provider credentials.
// ABOUTME: Proves qualified tenant bindings dispatch through their declared source without fallback.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Explore.Secrets.Observability;
using Explore.Secrets.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Explore.Secrets.UnitTests.Services;

public sealed class SecretResolverBindingTests
{
    [Test]
    public async Task ResolveTenantBindingAsync_UsesExactQualifiedTenantBinding()
    {
        Guid tenantId = Guid.CreateVersion7();
        SecretBinding first = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.RegistrationProviders.ApiToken,
            SecretScope.Tenant,
            tenantId,
            "TOKEN_A",
            qualifier: "connection-a");
        first.Id = Guid.CreateVersion7();
        SecretBinding second = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.RegistrationProviders.ApiToken,
            SecretScope.Tenant,
            tenantId,
            "TOKEN_B",
            qualifier: "connection-b");
        second.Id = Guid.CreateVersion7();
        var resolver = Resolver([first, second], new Dictionary<Guid, string>
        {
            [first.Id] = "secret-a",
            [second.Id] = "secret-b"
        });

        ResolvedSecret? resolved = await resolver.ResolveTenantBindingAsync(tenantId, second.Id, CancellationToken.None);

        await Assert.That(resolved).IsNotNull();
        await Assert.That(resolved!.Value).IsEqualTo("secret-b");
        await Assert.That(resolved.Scope).IsEqualTo(SecretScope.Tenant);
        await Assert.That(resolved.ScopeId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task ResolveTenantBindingAsync_DoesNotFallBackToOtherTenantBinding()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid otherTenantId = Guid.CreateVersion7();
        SecretBinding binding = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.RegistrationProviders.WebhookSecret,
            SecretScope.Tenant,
            otherTenantId,
            "WEBHOOK_SECRET",
            qualifier: "binding");
        binding.Id = Guid.CreateVersion7();
        var resolver = Resolver([binding], new Dictionary<Guid, string> { [binding.Id] = "secret" });

        ResolvedSecret? resolved = await resolver.ResolveTenantBindingAsync(tenantId, binding.Id, CancellationToken.None);

        await Assert.That(resolved).IsNull();
    }

    private static SecretResolver Resolver(IReadOnlyList<SecretBinding> bindings, IReadOnlyDictionary<Guid, string> values)
    {
        return new SecretResolver(
            new FakeSecretBindingRepository(bindings),
            [new FakeSecretSource(values)],
            new MemoryCache(new MemoryCacheOptions()),
            new SecretResolverMetrics(new TestMeterFactory()),
            NullLogger<SecretResolver>.Instance);
    }

    private sealed class FakeSecretBindingRepository(IReadOnlyList<SecretBinding> bindings) : ISecretBindingRepository
    {
        public Task<SecretBinding?> GetByTenantAndIdAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(bindings.SingleOrDefault(binding => binding.ScopeId == tenantId && binding.Id == bindingId));
        public Task<SecretBinding?> GetByKeyAndScopeAsync(string settingKey, SecretScope scope, Guid? scopeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(bindings.SingleOrDefault(binding => binding.SettingKey == settingKey && binding.Scope == scope && binding.ScopeId == scopeId && binding.Qualifier == string.Empty));
        public Task<IReadOnlyList<SecretBinding>> GetByScopeAsync(SecretScope scope, Guid? scopeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SecretBinding>>([.. bindings.Where(binding => binding.Scope == scope && binding.ScopeId == scopeId)]);
        public Task<IReadOnlyList<SecretBinding>> GetAllForKeyAsync(string settingKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SecretBinding>>([.. bindings.Where(binding => binding.SettingKey == settingKey)]);
        public Task<bool> ExistsForScopeAsync(string settingKey, SecretScope scope, Guid? scopeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(bindings.Any(binding => binding.SettingKey == settingKey && binding.Scope == scope && binding.ScopeId == scopeId && binding.Qualifier == string.Empty));
        public Task<SecretBinding?> GetById(Guid id) => Task.FromResult(bindings.SingleOrDefault(binding => binding.Id == id));
        public Task<IReadOnlyList<SecretBinding>> GetAll() => Task.FromResult(bindings);
        public Task<(IReadOnlyList<SecretBinding> Items, int TotalCount)> GetAllPaged(int pageNumber, int pageSize) => Task.FromResult((bindings, bindings.Count));
        public Task<bool> Exists(Guid id) => Task.FromResult(bindings.Any(binding => binding.Id == id));
        public Task<SecretBinding> Create(SecretBinding entity) => throw new NotImplementedException();
        public Task Update(SecretBinding entity) => throw new NotImplementedException();
        public Task Delete(SecretBinding entity) => throw new NotImplementedException();
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options.Name ?? "test");
        public void Dispose() { }
    }

    private sealed class FakeSecretSource(IReadOnlyDictionary<Guid, string> values) : ISecretSource
    {
        public SecretSourceType SourceType => SecretSourceType.EnvironmentVariable;
        public Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken cancellationToken = default) =>
            Task.FromResult(values.GetValueOrDefault(binding.Id));
        public Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken = default) =>
            Task.FromResult(values.ContainsKey(binding.Id));
    }
}
