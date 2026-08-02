// ABOUTME: Verifies the API-hosted registration-finalization polling cycle invokes the durable drain.
// ABOUTME: Confirms each cycle resolves MediatR from a fresh dependency-injection scope.

using Explore.API.BackgroundServices;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class RegistrationFinalizationWorkerTests
{
    [Test]
    public async Task RunOnceAsync_SendsSharedDrainCommandFromHostedScope()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<DrainRegistrationFinalizationEffectsCommand>(), CancellationToken.None)
            .Returns(2);
        var services = new ServiceCollection();
        services.AddScoped(_ => sender);
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        var worker = new RegistrationFinalizationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RegistrationFinalizationWorker>.Instance);

        int completed = await worker.RunOnceAsync(CancellationToken.None);

        await Assert.That(completed).IsEqualTo(2);
        await sender.Received(1).Send(
            Arg.Is<DrainRegistrationFinalizationEffectsCommand>(command =>
                command.LeaseOwner == "registration-finalization-worker" &&
                command.BatchSize == 100 && command.LeaseSeconds == 60),
            CancellationToken.None);
    }
}
