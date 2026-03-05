// ABOUTME: Unit tests for TestTmsConnectionCommandHandler — verifies TMS connection test flow.
// ABOUTME: Tests success and failure scenarios for TMS provider connectivity checks.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Localization.Handlers.Commands;
using Explore.Application.Features.Localization.Requests.Commands;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class TestTmsConnectionCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenConnectionSucceeds_ReturnsSuccess()
    {
        var provider = Substitute.For<ITranslationManagementProvider>();
        provider.TestConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var handler = new TestTmsConnectionCommandHandler(provider);

        var result = await handler.Handle(new TestTmsConnectionCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).Contains("successful");
    }

    [Test]
    public async Task Handle_WhenConnectionFails_ReturnsFailure()
    {
        var provider = Substitute.For<ITranslationManagementProvider>();
        provider.TestConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = new TestTmsConnectionCommandHandler(provider);

        var result = await handler.Handle(new TestTmsConnectionCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("failed");
    }
}
