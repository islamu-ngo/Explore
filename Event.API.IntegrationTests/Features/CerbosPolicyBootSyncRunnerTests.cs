// ABOUTME: Unit-style tests for API zero-touch Cerbos policy package boot synchronization.
// ABOUTME: Verifies boot sync gates on complete Admin API configuration and never requires direct Admin API calls in API.

using Explore.API.BackgroundServices;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

public class CerbosPolicyBootSyncRunnerTests
{
    [Test]
    public async Task RunOnceAsync_WithCompleteAdminConfiguration_PublishesPackageOnce()
    {
        var packageService = Substitute.For<IPolicyPackageService>();
        packageService.PublishAsync(Arg.Any<CancellationToken>())
            .Returns(CreateSuccessResult());
        var runner = CreateRunner(packageService, CreateAdminSettings(
            endpoints: ["https://cerbos.example"],
            username: "admin",
            password: "secret"));

        await runner.RunOnceAsync(CancellationToken.None);

        await packageService.Received(1).PublishAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunOnceAsync_WithoutAdminEndpoint_SkipsPublish()
    {
        var packageService = Substitute.For<IPolicyPackageService>();
        var runner = CreateRunner(packageService, CreateAdminSettings(
            endpoints: [],
            username: "admin",
            password: "secret"));

        await runner.RunOnceAsync(CancellationToken.None);

        await packageService.DidNotReceive().PublishAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunOnceAsync_WithPartialAdminCredentials_SkipsPublish()
    {
        var packageService = Substitute.For<IPolicyPackageService>();
        var runner = CreateRunner(packageService, CreateAdminSettings(
            endpoints: ["https://cerbos.example"],
            username: "admin",
            password: string.Empty));

        await runner.RunOnceAsync(CancellationToken.None);

        await packageService.DidNotReceive().PublishAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunOnceAsync_WhenPublisherReturnsFailure_DoesNotThrow()
    {
        var packageService = Substitute.For<IPolicyPackageService>();
        packageService.PublishAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PolicyPackagePublishResult(
                Succeeded: false,
                PackageId: "islamuevent-authorization-policies",
                ContentHash: "abc123",
                Message: "safe failure",
                PublishedAt: DateTimeOffset.UtcNow,
                Warnings: ["safe warning"])));
        var runner = CreateRunner(packageService, CreateAdminSettings(
            endpoints: ["https://cerbos.example"],
            username: "admin",
            password: "secret"));

        await runner.RunOnceAsync(CancellationToken.None);

        await packageService.Received(1).PublishAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunOnceAsync_WhenDisabled_SkipsPublish()
    {
        var packageService = Substitute.For<IPolicyPackageService>();
        var runner = CreateRunner(
            packageService,
            CreateAdminSettings(endpoints: ["https://cerbos.example"], username: "admin", password: "secret"),
            new CerbosPolicyBootSyncOptions { Enabled = false, InitialDelaySeconds = 0, TimeoutSeconds = 5 });

        await runner.RunOnceAsync(CancellationToken.None);

        await packageService.DidNotReceive().PublishAsync(Arg.Any<CancellationToken>());
    }

    private static CerbosPolicyBootSyncRunner CreateRunner(
        IPolicyPackageService packageService,
        CerbosAdminApiSettings adminSettings,
        CerbosPolicyBootSyncOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => packageService);
        var serviceProvider = services.BuildServiceProvider();

        return new CerbosPolicyBootSyncRunner(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(adminSettings),
            Options.Create(options ?? new CerbosPolicyBootSyncOptions { InitialDelaySeconds = 0, TimeoutSeconds = 5 }),
            Substitute.For<ILogger<CerbosPolicyBootSyncRunner>>());
    }

    private static CerbosAdminApiSettings CreateAdminSettings(
        IReadOnlyList<string> endpoints,
        string username,
        string password)
    {
        return new CerbosAdminApiSettings
        {
            Endpoints = [.. endpoints],
            AdminUsername = username,
            AdminPassword = password
        };
    }

    private static Task<PolicyPackagePublishResult> CreateSuccessResult()
    {
        return Task.FromResult(new PolicyPackagePublishResult(
            Succeeded: true,
            PackageId: "islamuevent-authorization-policies",
            ContentHash: "abc123",
            Message: "published",
            PublishedAt: DateTimeOffset.UtcNow,
            Warnings: []));
    }
}
