// ABOUTME: Pins the Task 7.7 CQRS request and anonymous descriptor contract surface.
// ABOUTME: Ensures attachment writes remain authorized while the optional-questionnaire query stays public.

using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.RegistrationForms;
using Explore.Application.Features.RegistrationForms.Handlers.Commands;
using Explore.Application.Features.RegistrationForms.Handlers.Queries;
using Explore.Application.Features.RegistrationForms.Requests.Commands;
using Explore.Application.Features.RegistrationForms.Requests.Queries;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.RegistrationForms;

public sealed class ParticipationRequirementAttachmentContractTests
{
    [Test]
    public async Task OptionalQuestionnaire_DoesNotDiscloseWhenTheEventIsNotPubliclyEligible()
    {
        var attachments = Substitute.For<IParticipationRequirementAttachmentRepository>();
        var events = Substitute.For<IEventRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        tenantContext.TenantId.Returns(tenantId);
        events.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetOptionalQuestionnaireQueryHandler(attachments, events, tenantContext);

        OptionalQuestionnaireDto? result = await handler.Handle(
            new GetOptionalQuestionnaireQuery(eventId), CancellationToken.None);

        await Assert.That(result).IsNull();
        await attachments.DidNotReceiveWithAnyArgs()
            .GetOptionalQuestionnaireAsync(default, default, default);
    }

    [Test]
    public async Task Task77ContractsExistWithExactAuthorizationActions()
    {
        Type applicationAssemblyAnchor = typeof(RegistrationWorkflowDto);
        Type? attach = applicationAssemblyAnchor.Assembly.GetType(
            "Explore.Application.Features.RegistrationForms.Requests.Commands.AttachRegistrationRequirementCommand");
        Type? detach = applicationAssemblyAnchor.Assembly.GetType(
            "Explore.Application.Features.RegistrationForms.Requests.Commands.DetachRegistrationRequirementCommand");
        Type? query = applicationAssemblyAnchor.Assembly.GetType(
            "Explore.Application.Features.RegistrationForms.Requests.Queries.GetOptionalQuestionnaireQuery");
        Type? dto = applicationAssemblyAnchor.Assembly.GetType(
            "Explore.Application.DTOs.RegistrationForms.OptionalQuestionnaireDto");

        await Assert.That(attach).IsNotNull();
        await Assert.That(detach).IsNotNull();
        await Assert.That(query).IsNotNull();
        await Assert.That(dto).IsNotNull();
        await Assert.That(AuthorizationActions.RegistrationForms.Attach).IsEqualTo("attach");
        await Assert.That(AuthorizationActions.RegistrationForms.Detach).IsEqualTo("detach");
    }

    [Test]
    public async Task SuccessfulAttachInvalidatesEventDetailAndTenantListCaches()
    {
        (EventParticipationConfiguration configuration, RegistrationWorkflow workflow,
            RegistrationRequirement requirement) = CreateAttachmentGraph();
        var repository = Substitute.For<IParticipationRequirementAttachmentRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var unitOfWork = TransactionalUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        tenantContext.TenantId.Returns(configuration.TenantId);
        repository.GetConfigurationForUpdateAsync(
                configuration.Id, configuration.TenantId, Arg.Any<CancellationToken>())
            .Returns(configuration);
        repository.GetWorkflowForUpdateAsync(
                configuration.Id, configuration.TenantId, workflow.Id, Arg.Any<CancellationToken>())
            .Returns(workflow);
        var handler = new AttachRegistrationRequirementCommandHandler(
            repository, tenantContext, unitOfWork, TimeProvider.System, cache);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new AttachRegistrationRequirementCommand(
                configuration.Id,
                workflow.Id,
                requirement.Id,
                false,
                null,
                null,
                configuration.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync(
            $"event:detail:{configuration.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            CacheTags.EventListByTenant(configuration.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SuccessfulDetachInvalidatesEventDetailAndTenantListCaches()
    {
        (EventParticipationConfiguration configuration, RegistrationWorkflow workflow,
            RegistrationRequirement requirement) = CreateAttachmentGraph();
        configuration.AttachRequirement(
            Guid.CreateVersion7(), workflow, requirement, null, false, DateTime.UtcNow);
        var repository = Substitute.For<IParticipationRequirementAttachmentRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var unitOfWork = TransactionalUnitOfWork();
        var cache = Substitute.For<HybridCache>();
        tenantContext.TenantId.Returns(configuration.TenantId);
        repository.GetConfigurationForUpdateAsync(
                configuration.Id, configuration.TenantId, Arg.Any<CancellationToken>())
            .Returns(configuration);
        var handler = new DetachRegistrationRequirementCommandHandler(
            repository, tenantContext, unitOfWork, TimeProvider.System, cache);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new DetachRegistrationRequirementCommand(
                configuration.Id, requirement.Id, configuration.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveAsync(
            $"event:detail:{configuration.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            CacheTags.EventListByTenant(configuration.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InvalidAttachmentCommandsDoNotInvalidateCaches()
    {
        var repository = Substitute.For<IParticipationRequirementAttachmentRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var attach = new AttachRegistrationRequirementCommandHandler(
            repository, tenantContext, unitOfWork, TimeProvider.System, cache);
        var detach = new DetachRegistrationRequirementCommandHandler(
            repository, tenantContext, unitOfWork, TimeProvider.System, cache);

        BaseCommandResponse<Guid> attachResult = await attach.Handle(
            new AttachRegistrationRequirementCommand(
                Guid.Empty, Guid.Empty, Guid.Empty, false, null, null, Guid.Empty),
            CancellationToken.None);
        BaseCommandResponse<Guid> detachResult = await detach.Handle(
            new DetachRegistrationRequirementCommand(Guid.Empty, Guid.Empty, Guid.Empty),
            CancellationToken.None);

        await Assert.That(attachResult.IsSuccess).IsFalse();
        await Assert.That(detachResult.IsSuccess).IsFalse();
        await cache.DidNotReceiveWithAnyArgs().RemoveAsync((string)null!, default);
        await cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync((string)null!, default);
    }

    private static (EventParticipationConfiguration Configuration, RegistrationWorkflow Workflow,
        RegistrationRequirement Requirement) CreateAttachmentGraph()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        DateTime now = DateTime.UtcNow;
        EventParticipationConfiguration configuration = EventParticipationConfiguration.Create(
            eventId,
            tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            null,
            now);
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(
            tenantId, eventId, "registration", now);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow,
            1,
            RegistrationRequirementCriticalityEnum.Required,
            false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration,
            RegistrationAnswerSyncModeEnum.COMPLETION_ONLY,
            RegistrationRequirementSubjectTypeEnum.AllOrders,
            null,
            now);
        requirement.AddChannel(RegistrationChannel.Create(requirement, 1, true, null, now));
        workflow.AddRequirement(requirement);
        return (configuration, workflow, requirement);
    }

    private static IUnitOfWork TransactionalUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()
                (CancellationToken.None));
        return unitOfWork;
    }
}
