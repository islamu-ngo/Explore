// ABOUTME: Verifies the split migration worker owns the shared post-migration manifest sequence.
// ABOUTME: Uses an inert DbContext and substituted sequence so no database or external service starts.

extern alias migrationservice;

namespace Explore.Infrastructure.Tests.Infrastructure.ConfigurationManifest;

using Explore.Infrastructure.ConfigurationManifest;
using Explore.Persistence;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MigrationWorker = migrationservice::Event.MigrationService.Worker;

public sealed class ConfigurationManifestMigrationWorkerTests
{
    [Test]
    public async Task ExecuteAsync_RunsSharedSequenceBeforeStoppingOneShotHost()
    {
        var sequence = Substitute.For<IConfigurationManifestPostMigrationSequence>();
        sequence.RunAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddScoped(_ => new ExploreDbContext(
            new DbContextOptionsBuilder<ExploreDbContext>().Options));
        services.AddScoped(_ => sequence);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using (AsyncServiceScope verificationScope = provider.CreateAsyncScope())
        {
            var resolved = verificationScope.ServiceProvider
                .GetRequiredService<IConfigurationManifestPostMigrationSequence>();
            await Assert.That(ReferenceEquals(sequence, resolved)).IsTrue();
        }

        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var stopped = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        lifetime.When(instance => instance.StopApplication())
            .Do(_ => stopped.TrySetResult());
        var environment = Substitute.For<IHostEnvironment>();
        var configuration = new ConfigurationBuilder().Build();
        var databaseOptions = new PrimaryDatabaseConnectionOptions
        {
            Role = PrimaryDatabaseRole.Migrator,
            Provider = PrimaryDatabaseProvider.Sqlite
        };
        var worker = new MigrationWorker(
            provider,
            lifetime,
            environment,
            configuration,
            databaseOptions,
            NullLogger<MigrationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await sequence.Received(1).RunAsync(
            Arg.Any<Func<CancellationToken, Task>>(),
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested));
        lifetime.Received(1).StopApplication();
    }
}
