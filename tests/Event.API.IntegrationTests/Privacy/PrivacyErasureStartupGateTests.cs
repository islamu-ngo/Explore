// ABOUTME: Proves external authority replay failure blocks API startup before hosted workers run.
// ABOUTME: Verifies the startup gate preserves caller cancellation and sanitizes failures.

using Event.Api.IntegrationTests.Fixtures;
using Explore.API.BackgroundServices;
using Explore.Application.Configuration;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Privacy;

public sealed class PrivacyErasureStartupGateTests
{
    [Test]
    public async Task CoLocatedTopology_DoesNotResolveReplayService()
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IOptions<PrivacyErasureDurabilityOptions>>(Options.Create(new PrivacyErasureDurabilityOptions()))
            .BuildServiceProvider();

        await PrivacyErasureStartupGate.RunAsync(services, CancellationToken.None);
    }

    [Test]
    public async Task ReplayFailure_PreventsHostedWorkerInvocation()
    {
        await using var factory = new ReplayFailureFactory();

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Task.Run(() => factory.CreateClient()));

        await Assert.That(factory.HostedWorkerStarted).IsFalse();
        await Assert.That(exception!.Message).Contains("API startup is blocked");
        await Assert.That(exception.Message).DoesNotContain(ReplayFailure.RawProviderMessage);
    }

    [Test]
    public async Task Cancellation_PropagatesWithoutStartingHostedWorker()
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IOptions<PrivacyErasureDurabilityOptions>>(Options.Create(new PrivacyErasureDurabilityOptions
            {
                Topology = PrivacyErasureAuthorityTopology.ExternalDatabase
            }))
            .AddScoped<IPrivacyErasureReplayService, ReplayCancellation>()
            .BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            PrivacyErasureStartupGate.RunAsync(services, cancellation.Token));
    }

    private sealed class ReplayFailureFactory : AuthenticatedWebApplicationFactory
    {
        private readonly StartupMarker _marker = new();

        public ReplayFailureFactory()
        {
            AdditionalConfiguration["Testing:EnablePrivacyErasureStartupGate"] = "true";
            AdditionalConfiguration["PrivacyErasure:Authority:Topology"] = "ExternalDatabase";
            AdditionalConfiguration["ConnectionStrings:PrivacyErasureAuthority"] =
                "Host=unused;Database=unused;Username=unused";
        }

        public bool HostedWorkerStarted => _marker.Started;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<PrivacyErasureDurabilityOptions>(options =>
                {
                    options.Topology = PrivacyErasureAuthorityTopology.ExternalDatabase;
                });
                services.RemoveAll<IPrivacyErasureReplayService>();
                services.AddScoped<IPrivacyErasureReplayService, ReplayFailure>();
                services.AddSingleton(_marker);
                services.AddHostedService<StartupMarkerHostedService>();
            });
        }
    }

    private sealed class ReplayFailure : IPrivacyErasureReplayService
    {
        public const string RawProviderMessage = "provider-endpoint-and-secret-canary";

        public Task ReplayAsync(CancellationToken cancellationToken) =>
            throw new IOException(RawProviderMessage);
    }

    private sealed class ReplayCancellation : IPrivacyErasureReplayService
    {
        public Task ReplayAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class StartupMarker
    {
        public bool Started { get; set; }
    }

    private sealed class StartupMarkerHostedService(StartupMarker marker) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            marker.Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
