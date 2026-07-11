// ABOUTME: Unit tests for ApplyEventSessionTemplateSyncCommandHandler covering manual validation and sync-service delegation.
// ABOUTME: Verifies invalid plans throw ValidationException and valid plans are wrapped in BaseCommandResponse.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessionTemplateSync.Commands.ApplyEventSessionTemplateSync;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionTemplateSync;

public class ApplyEventSessionTemplateSyncCommandHandlerTests
{
    [Test]
    public async Task Handle_WithInvalidPlan_ThrowsValidationException()
    {
        var service = Substitute.For<IEventSessionTemplateSyncService>();
        var handler = new ApplyEventSessionTemplateSyncCommandHandler(service);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await handler.Handle(new ApplyEventSessionTemplateSyncCommand(Guid.NewGuid(), new TemplateSyncPlanDto(), 0), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithValidPlan_InvokesServiceAndWrapsOutcome()
    {
        var service = Substitute.For<IEventSessionTemplateSyncService>();
        var plan = new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, AddedDefinitionKeys = ["tenant.sync/session"] };
        var outcome = new TemplateSyncOutcomeDto(["tenant.sync/session"], [], [], 2, DateTimeOffset.UtcNow);
        service.ApplySyncAsync(Arg.Any<Guid>(), plan, 1, Arg.Any<CancellationToken>()).Returns(outcome);
        var handler = new ApplyEventSessionTemplateSyncCommandHandler(service);

        var result = await handler.Handle(new ApplyEventSessionTemplateSyncCommand(Guid.NewGuid(), plan, 1), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(outcome);
        await service.Received(1).ApplySyncAsync(Arg.Any<Guid>(), plan, 1, Arg.Any<CancellationToken>());
    }
}
