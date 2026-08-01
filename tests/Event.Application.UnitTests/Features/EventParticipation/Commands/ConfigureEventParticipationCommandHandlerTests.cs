// ABOUTME: Verifies dedicated event participation reconfiguration behavior and typed failure translation.
// ABOUTME: Covers tenant-bound reads, configuration concurrency, cache sequencing, and no update after Domain validation failure.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventParticipation.Handlers.Commands;
using Explore.Application.Features.EventParticipation.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventParticipation.Commands;

public sealed class ConfigureEventParticipationCommandHandlerTests
{
    [Test]
    public async Task Handle_WithCurrentStamp_ReconfiguresAndInvalidatesEventCaches()
    {
        var configurations = Substitute.For<IEventParticipationConfigurationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var cache = Substitute.For<HybridCache>();
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        EventParticipationConfiguration configuration = CreateConfiguration(eventId, tenantId);
        tenantContext.TenantId.Returns(tenantId);
        configurations.GetByEventAndTenantAsync(eventId, tenantId, Arg.Any<CancellationToken>())
            .Returns(configuration);
        var handler = new ConfigureEventParticipationCommandHandler(configurations, tenantContext, cache);

        var result = await handler.Handle(new ConfigureEventParticipationCommand
        {
            EventId = eventId,
            ExpectedConcurrencyStamp = configuration.ConcurrencyStamp,
            ParticipationConfiguration = new ConfigureEventParticipationDto
            {
                ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.ExternalManaged,
                AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.Required
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(configuration.ParticipationHandlingModeId)
            .IsEqualTo((int)ParticipationHandlingModeEnum.ExternalManaged);
        await configurations.Received(1).UpdateAsync(configuration, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleStamp_ReturnsConflictWithoutMutation()
    {
        var configurations = Substitute.For<IEventParticipationConfigurationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        EventParticipationConfiguration configuration = CreateConfiguration(eventId, tenantId);
        tenantContext.TenantId.Returns(tenantId);
        configurations.GetByEventAndTenantAsync(eventId, tenantId, Arg.Any<CancellationToken>())
            .Returns(configuration);
        var handler = new ConfigureEventParticipationCommandHandler(
            configurations,
            tenantContext,
            Substitute.For<HybridCache>());

        var result = await handler.Handle(new ConfigureEventParticipationCommand
        {
            EventId = eventId,
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            ParticipationConfiguration = InformationOnly()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_participation_configuration_concurrency_conflict");
        await configurations.DidNotReceive().UpdateAsync(Arg.Any<EventParticipationConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithIllegalDomainCombination_TranslatesTypedErrorWithoutMutation()
    {
        var configurations = Substitute.For<IEventParticipationConfigurationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        EventParticipationConfiguration configuration = CreateConfiguration(eventId, tenantId);
        tenantContext.TenantId.Returns(tenantId);
        configurations.GetByEventAndTenantAsync(eventId, tenantId, Arg.Any<CancellationToken>())
            .Returns(configuration);
        var handler = new ConfigureEventParticipationCommandHandler(
            configurations,
            tenantContext,
            Substitute.For<HybridCache>());

        var result = await handler.Handle(new ConfigureEventParticipationCommand
        {
            EventId = eventId,
            ExpectedConcurrencyStamp = configuration.ConcurrencyStamp,
            ParticipationConfiguration = new ConfigureEventParticipationDto
            {
                ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.WalkIn,
                AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.Required
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_participation_configuration_validation_failed");
        await Assert.That(result.Errors).Contains(error => error.Contains(
            nameof(EventParticipationConfigurationErrorCode.AdvanceRegistrationObligationNotAllowed),
            StringComparison.Ordinal));
        await Assert.That(configuration.ParticipationHandlingModeId)
            .IsEqualTo((int)ParticipationHandlingModeEnum.InformationOnly);
        await configurations.DidNotReceive().UpdateAsync(Arg.Any<EventParticipationConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenModeWouldInvalidateAttachment_ReturnsStableConflictWithoutUpdate()
    {
        var configurations = Substitute.For<IEventParticipationConfigurationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        DateTime now = DateTime.UtcNow;
        EventParticipationConfiguration configuration = EventParticipationConfiguration.Create(
            eventId, tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            null,
            now);
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenantId, eventId, "registration", now);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow, 1, RegistrationRequirementCriticalityEnum.Required, false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration,
            RegistrationAnswerSyncModeEnum.COMPLETION_ONLY,
            RegistrationRequirementSubjectTypeEnum.AllOrders,
            null,
            now);
        requirement.AddChannel(RegistrationChannel.Create(requirement, 1, true, null, now));
        workflow.AddRequirement(requirement);
        configuration.AttachRequirement(Guid.CreateVersion7(), workflow, requirement, null, false, now);
        tenantContext.TenantId.Returns(tenantId);
        configurations.GetByEventAndTenantAsync(eventId, tenantId, Arg.Any<CancellationToken>())
            .Returns(configuration);
        var handler = new ConfigureEventParticipationCommandHandler(
            configurations,
            tenantContext,
            Substitute.For<HybridCache>());

        BaseCommandResponse<Guid> result = await handler.Handle(new ConfigureEventParticipationCommand
        {
            EventId = eventId,
            ExpectedConcurrencyStamp = configuration.ConcurrencyStamp,
            ParticipationConfiguration = new ConfigureEventParticipationDto
            {
                ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.WalkIn,
                AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo("event_participation_configuration_attachment_conflict");
        await configurations.DidNotReceive().UpdateAsync(
            Arg.Any<EventParticipationConfiguration>(), Arg.Any<CancellationToken>());
    }

    private static EventParticipationConfiguration CreateConfiguration(Guid eventId, Guid tenantId)
    {
        EventParticipationConfiguration configuration = EventParticipationConfiguration.Create(
            eventId,
            tenantId,
            (int)ParticipationHandlingModeEnum.InformationOnly,
            (int)AdvanceRegistrationObligationEnum.NotApplicable,
            identityAccessModeId: null,
            guestRecoveryPolicy: null,
            DateTime.UtcNow);
        configuration.ConcurrencyStamp = Guid.CreateVersion7();
        return configuration;
    }

    private static ConfigureEventParticipationDto InformationOnly() => new()
    {
        ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly,
        AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
    };
}
