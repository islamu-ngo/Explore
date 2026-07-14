// ABOUTME: Provides a bounded EF design-time migration identifier override for migration-chain recovery.
// ABOUTME: Defaults to EF generation unless the explicit recovery timestamp environment variable is present.

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Persistence;

public sealed class ExplorePersistenceDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection) =>
        serviceCollection.AddSingleton<IMigrationsIdGenerator, RecoverableMigrationsIdGenerator>();
}

#pragma warning disable EF1001
internal sealed class RecoverableMigrationsIdGenerator : IMigrationsIdGenerator
{
    private readonly MigrationsIdGenerator _inner = new();

    public string GenerateId(string name)
    {
        var recoveryTimestamp = Environment.GetEnvironmentVariable("EVENT_EF_MIGRATION_TIMESTAMP")?.Trim();
        return string.IsNullOrWhiteSpace(recoveryTimestamp)
            ? _inner.GenerateId(name)
            : $"{recoveryTimestamp}_{name}";
    }

    public string GetName(string id) => _inner.GetName(id);

    public bool IsValidId(string value) => _inner.IsValidId(value);
}
#pragma warning restore EF1001
