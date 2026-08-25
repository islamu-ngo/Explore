// ABOUTME: Unit tests for ApplyEventTemplateSyncCommandHandler covering manual validation and sync-service delegation.
// ABOUTME: Verifies invalid plans throw ValidationException and valid plans are wrapped in BaseCommandResponse.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventTemplateSync;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTemplateSync.Commands.ApplyEventTemplateSync;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventTemplateSync;

public class ApplyEventTemplateSyncCommandHandlerTests
{
    [Test]
    public async Task Handle_WithInvalidPlan_ThrowsValidationException()
    {
        var service = Substitute.For<IEventTemplateSyncService>();
        var handler = new ApplyEventTemplateSyncCommandHandler(service);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await handler.Handle(new ApplyEventTemplateSyncCommand(Guid.NewGuid(), new TemplateSyncPlanDto(), 0), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WithValidPlan_InvokesServiceAndWrapsOutcome()
    {
        var service = Substitute.For<IEventTemplateSyncService>();
        var plan = new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, AddedDefinitionKeys = ["tenant.sync/field"] };
        var outcome = new TemplateSyncOutcomeDto(["tenant.sync/field"], [], [], 2, DateTimeOffset.UtcNow);
        service.ApplySyncAsync(Arg.Any<Guid>(), plan, 1, Arg.Any<CancellationToken>()).Returns(outcome);
        var handler = new ApplyEventTemplateSyncCommandHandler(service);

        var result = await handler.Handle(new ApplyEventTemplateSyncCommand(Guid.NewGuid(), plan, 1), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(outcome);
        await service.Received(1).ApplySyncAsync(Arg.Any<Guid>(), plan, 1, Arg.Any<CancellationToken>());
    }
}
