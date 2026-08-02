// ABOUTME: Verifies the embedded authority's local-file, single-writer, and bounded-contention contract.
// ABOUTME: Prevents URI, network, replica, and unsafe timeout configurations from reaching SQLite.

using Explore.Secrets.Database;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.Secrets.UnitTests.Database;

public sealed class EmbeddedPrivacyErasureAuthorityOptionsTests
{
    [Test]
    public void MissingConfigurationUsesProductionDefaults()
    {
        EmbeddedPrivacyErasureAuthorityOptions options =
            EmbeddedPrivacyErasureAuthorityOptions.Bind(BuildConfiguration([]));

        options.Path.Should().Be(EmbeddedPrivacyErasureAuthorityOptions.DefaultPath);
        options.WriterReplicaCount.Should().Be(1);
        options.BusyTimeoutSeconds.Should()
            .Be(EmbeddedPrivacyErasureAuthorityOptions.DefaultBusyTimeoutSeconds);
    }

    [Test]
    public void ConnectionStringUsesPrivatePersistedFileComposition()
    {
        string path = Path.Combine(Path.GetTempPath(), "privacy-erasure-authority.db");
        EmbeddedPrivacyErasureAuthorityOptions options =
            EmbeddedPrivacyErasureAuthorityOptions.Bind(BuildConfiguration(new()
            {
                ["PrivacyErasureAuthorityEmbedded:Path"] = path,
                ["PrivacyErasureAuthorityEmbedded:BusyTimeoutSeconds"] = "17",
            }));

        var connection = new SqliteConnectionStringBuilder(options.BuildConnectionString());

        connection.DataSource.Should().Be(Path.GetFullPath(path));
        connection.Mode.Should().Be(SqliteOpenMode.ReadWriteCreate);
        connection.Cache.Should().Be(SqliteCacheMode.Private);
        connection.DefaultTimeout.Should().Be(17);
    }

    [Test]
    [Arguments("relative.db")]
    [Arguments("file:/tmp/authority.db")]
    [Arguments("https://example.test/authority.db")]
    [Arguments("\\\\server\\authority.db")]
    public void NonLocalOrNonAbsolutePathFailsClosed(string path)
    {
        Action act = () => EmbeddedPrivacyErasureAuthorityOptions.Bind(
            BuildConfiguration(new()
            {
                ["PrivacyErasureAuthorityEmbedded:Path"] = path,
            }));

        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    [Arguments("0")]
    [Arguments("2")]
    public void WriterReplicaCountOtherThanOneFailsClosed(string replicas)
    {
        Action act = () => EmbeddedPrivacyErasureAuthorityOptions.Bind(
            BuildConfiguration(new()
            {
                ["PrivacyErasureAuthorityEmbedded:WriterReplicaCount"] = replicas,
            }));

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*WriterReplicaCount must be exactly 1*");
    }

    [Test]
    [Arguments("0")]
    [Arguments("301")]
    [Arguments("not-an-integer")]
    public void BusyTimeoutOutsideBoundsFailsClosed(string timeout)
    {
        Action act = () => EmbeddedPrivacyErasureAuthorityOptions.Bind(
            BuildConfiguration(new()
            {
                ["PrivacyErasureAuthorityEmbedded:BusyTimeoutSeconds"] = timeout,
            }));

        act.Should().Throw<OptionsValidationException>();
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
