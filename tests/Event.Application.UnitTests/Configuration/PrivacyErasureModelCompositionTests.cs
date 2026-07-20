// ABOUTME: Verifies canonical platform privacy-erasure configuration and default-mode secret isolation.
// ABOUTME: Proves retained authority is explicit, fail-closed, and absent from default composition.

using Explore.Application;
using Explore.Application.Configuration;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Event.Application.UnitTests.Configuration;

public sealed class PrivacyErasureModelCompositionTests
{
    [Test]
    public async Task PrivacyErasureDefaultComposition_PoisonAuthorityProviderIsNeverReadOrResolved()
    {
        var provider = new PoisonAuthorityConfigurationProvider();
        using var configuration = new ConfigurationRoot([provider]);
        var services = new ServiceCollection();

        services.ConfigureApplicationServices(configuration);
        PrivacyErasureDurabilityOptions options =
            PrivacyErasureDurabilityOptions.FromConfiguration(configuration);

        await Assert.That(options.Mode)
            .IsEqualTo(PrivacyErasureDurabilityMode.ApplicationDatabase);
        await Assert.That(provider.AuthorityReadCount).IsEqualTo(0);
        await Assert.That(services.Any(IsRetainedAuthorityDescriptor)).IsFalse();
        await Assert.That(services.Last(descriptor =>
                descriptor.ServiceType == typeof(IPrivacyErasureService))
            .ImplementationType)
            .IsEqualTo(typeof(ApplicationDatabasePrivacyErasureWorkflow));
    }

    [Test]
    [Arguments(null)]
    [Arguments("ApplicationDatabase")]
    public async Task ApplicationDatabaseMode_NeverRetainsStrayAuthorityConnection(string? mode)
    {
        var values = new Dictionary<string, string?>
        {
            ["PrivacyErasure:Durability:Mode"] = mode,
            ["ConnectionStrings:PrivacyErasureAuthority"] =
                "Host=unused;Database=unused;Username=unused"
        };

        PrivacyErasureDurabilityOptions options = Resolve(values);

        await Assert.That(options.Mode)
            .IsEqualTo(PrivacyErasureDurabilityMode.ApplicationDatabase);
        await Assert.That(typeof(PrivacyErasureDurabilityOptions).GetProperties()
            .Select(property => property.Name))
            .IsEquivalentTo(["Mode"]);
    }

    [Test]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("Automatic")]
    [Arguments("0")]
    [Arguments("1")]
    public async Task InvalidMode_FailsClosed(string mode)
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() =>
            Task.FromResult(Resolve(new Dictionary<string, string?>
            {
                ["PrivacyErasure:Durability:Mode"] = mode
            })));
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("Host=only")]
    [Arguments("not-a-connection-string")]
    public async Task RetainedMode_InvalidConnection_FailsClosed(string? connectionString)
    {
        await Assert.ThrowsAsync<OptionsValidationException>(() =>
            Task.FromResult(Resolve(new Dictionary<string, string?>
            {
                ["PrivacyErasure:Durability:Mode"] = "RetainedAuthority",
                ["ConnectionStrings:PrivacyErasureAuthority"] = connectionString
            })));
    }

    [Test]
    public async Task RetainedMode_ValidNpgsqlShape_SelectsRetainedWorkflow()
    {
        IConfiguration configuration = Build(new Dictionary<string, string?>
        {
            ["PrivacyErasure:Durability:Mode"] = "retainedauthority",
            ["ConnectionStrings:PrivacyErasureAuthority"] =
                "Host=localhost;Database=privacy_erasure;Username=runtime;Password=unused"
        });
        var services = new ServiceCollection();

        services.ConfigureApplicationServices(configuration);

        await Assert.That(services.Last(descriptor =>
                descriptor.ServiceType == typeof(IPrivacyErasureService))
            .ImplementationType)
            .IsEqualTo(typeof(RetainedAuthorityPrivacyErasureWorkflow));
    }

    [Test]
    public async Task LegacyAndNestedAuthorityKeys_DoNotActivateRetainedMode()
    {
        PrivacyErasureDurabilityOptions options = Resolve(new Dictionary<string, string?>
        {
            ["ConnectionStrings:LocationPrivacyAuthority"] =
                "Host=unused;Database=unused;Username=unused",
            ["LocationPrivacy:ErasureDurability:Mode"] = "RetainedAuthority",
            ["PrivacyErasure:Authority:Mode"] = "RetainedAuthority",
            ["PrivacyErasure:Authority:ConnectionString"] =
                "Host=unused;Database=unused;Username=unused"
        });

        await Assert.That(options.Mode)
            .IsEqualTo(PrivacyErasureDurabilityMode.ApplicationDatabase);
    }

    private static bool IsRetainedAuthorityDescriptor(ServiceDescriptor descriptor)
    {
        string[] names =
        [
            descriptor.ServiceType.FullName ?? string.Empty,
            descriptor.ImplementationType?.FullName ?? string.Empty
        ];
        return names.Any(name =>
            name.Contains("PrivacyErasureAuthorityDbContext", StringComparison.Ordinal)
            || name.Contains("IPrivacyErasureAuthority", StringComparison.Ordinal)
            || name.Contains("IPrivacyErasureReplayService", StringComparison.Ordinal));
    }

    private static PrivacyErasureDurabilityOptions Resolve(
        IDictionary<string, string?> values) =>
        PrivacyErasureDurabilityOptions.FromConfiguration(Build(values));

    private static IConfiguration Build(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private sealed class PoisonAuthorityConfigurationProvider : ConfigurationProvider
    {
        private static readonly string[] ForbiddenPrefixes =
        [
            "PrivacyErasure:Authority",
            "ConnectionStrings:PrivacyErasureAuthority",
            "ConnectionStrings:LocationPrivacyAuthority",
            "LocationPrivacy:ErasureAuthority",
            "LocationPrivacy:ErasureDurability"
        ];

        private int _authorityReadCount;

        public int AuthorityReadCount => Volatile.Read(ref _authorityReadCount);

        public override bool TryGet(string key, out string? value)
        {
            ThrowIfAuthorityKey(key);
            value = null;
            return false;
        }

        public override IEnumerable<string> GetChildKeys(
            IEnumerable<string> earlierKeys,
            string? parentPath)
        {
            if (parentPath is not null)
            {
                ThrowIfAuthorityKey(parentPath);
            }

            foreach (string key in earlierKeys)
            {
                ThrowIfAuthorityKey(
                    string.IsNullOrEmpty(parentPath)
                        ? key
                        : $"{parentPath}:{key}");
            }

            return earlierKeys;
        }

        private void ThrowIfAuthorityKey(string key)
        {
            if (!ForbiddenPrefixes.Any(prefix =>
                    key.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                    || key.StartsWith($"{prefix}:", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Interlocked.Increment(ref _authorityReadCount);
            throw new InvalidOperationException("Default composition read an authority configuration key.");
        }
    }
}
