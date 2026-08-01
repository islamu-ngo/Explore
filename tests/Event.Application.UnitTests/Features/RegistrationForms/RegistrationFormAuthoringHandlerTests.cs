// ABOUTME: Exercises every named registration-form command handler plus critical mutation behavior.
// ABOUTME: Verifies validation failure codes, stale writes, immutable publication, and artifact pinning.

using System.Reflection;
using System.Runtime.CompilerServices;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.RegistrationForms.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.RegistrationForms;

public sealed class RegistrationFormAuthoringHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    [Arguments(nameof(CreateRegistrationWorkflowCommand))]
    [Arguments(nameof(UpdateRegistrationWorkflowCommand))]
    [Arguments(nameof(CreateRegistrationRequirementCommand))]
    [Arguments(nameof(UpdateRegistrationRequirementCommand))]
    [Arguments(nameof(DeleteRegistrationRequirementCommand))]
    [Arguments(nameof(CreateRegistrationFormCommand))]
    [Arguments(nameof(CreateRegistrationFormVersionCommand))]
    [Arguments(nameof(AddRegistrationFormSectionCommand))]
    [Arguments(nameof(UpdateRegistrationFormSectionCommand))]
    [Arguments(nameof(DeleteRegistrationFormSectionCommand))]
    [Arguments(nameof(AddRegistrationFormFieldCommand))]
    [Arguments(nameof(UpdateRegistrationFormFieldCommand))]
    [Arguments(nameof(DeleteRegistrationFormFieldCommand))]
    [Arguments(nameof(AddRegistrationFormFieldOptionCommand))]
    [Arguments(nameof(UpdateRegistrationFormFieldOptionCommand))]
    [Arguments(nameof(RetireRegistrationFormFieldOptionCommand))]
    [Arguments(nameof(AddRegistrationFormRuleCommand))]
    [Arguments(nameof(UpdateRegistrationFormRuleCommand))]
    [Arguments(nameof(DeleteRegistrationFormRuleCommand))]
    [Arguments(nameof(PublishRegistrationFormVersionCommand))]
    public async Task NamedHandler_DispatchesThroughManualValidation(string commandName)
    {
        Assembly assembly = typeof(CreateRegistrationWorkflowCommand).Assembly;
        Type commandType = assembly.GetType($"Explore.Application.Features.RegistrationForms.Requests.Commands.{commandName}")!;
        Type handlerType = assembly.GetType(
            $"Explore.Application.Features.RegistrationForms.Handlers.Commands.{commandName}Handler")!;
        object handler = Activator.CreateInstance(handlerType, CreateService(out _, out _))!;
        object command = RuntimeHelpers.GetUninitializedObject(commandType);
        MethodInfo handle = handlerType.GetMethod("Handle", [commandType, typeof(CancellationToken)])!;

        var task = (Task)handle.Invoke(handler, [command, CancellationToken.None])!;
        await task;
        var response = (BaseCommandResponse<Guid>)task.GetType().GetProperty("Result")!.GetValue(task)!;

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("registration_form_validation_failed");
    }

    [Test]
    public async Task UpdateWorkflow_WithStaleStamp_ThrowsConcurrencyConflict()
    {
        RegistrationFormAuthoringCommandService service = CreateService(
            out IRegistrationFormAuthoringRepository repository, out Guid tenantId);
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenantId, Guid.CreateVersion7(), "registration", Now);
        workflow.ConcurrencyStamp = Guid.CreateVersion7();
        repository.GetWorkflowForUpdateAsync(workflow.EventId, workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);

        Task Act() => service.UpdateWorkflowAsync(new(workflow.EventId, workflow.Id, "changed", Guid.CreateVersion7()),
            CancellationToken.None);

        await Assert.That(Act).Throws<Explore.Application.Exceptions.ConcurrencyConflictException>();
    }

    [Test]
    public async Task AddSection_ToPublishedVersion_IsRejected()
    {
        RegistrationFormAuthoringCommandService service = CreateService(
            out IRegistrationFormAuthoringRepository repository, out Guid tenantId);
        RegistrationFormVersion version = BuildVersion(tenantId);
        new FormSchemaArtifactPublicationService(new FormSchemaArtifactGenerator()).Publish(version, Now.AddMinutes(1));
        repository.GetVersionForUpdateAsync(version.EventId, version.RegistrationFormId, version.Id,
            Arg.Any<CancellationToken>()).Returns(version);

        Task Act() => service.AddSectionAsync(new(version.EventId, version.RegistrationFormId, version.Id, 2,
            "Late section", version.ConcurrencyStamp), CancellationToken.None);

        await Assert.That(Act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Publish_PinsFourArtifactsAndHash_ThenPersistsVersion()
    {
        RegistrationFormAuthoringCommandService service = CreateService(
            out IRegistrationFormAuthoringRepository repository, out Guid tenantId);
        RegistrationFormVersion version = BuildVersion(tenantId);
        var expected = new FormSchemaArtifactGenerator().Generate(version);
        repository.GetVersionForUpdateAsync(version.EventId, version.RegistrationFormId, version.Id,
            Arg.Any<CancellationToken>()).Returns(version);

        BaseCommandResponse<Guid> response = await service.PublishAsync(new(version.EventId, version.RegistrationFormId,
            version.Id, version.ConcurrencyStamp), CancellationToken.None);

        await Assert.That(response.Success).IsTrue();
        await Assert.That(version.DataSchemaArtifact).IsEqualTo(expected.DataSchemaJson);
        await Assert.That(version.UiSchemaArtifact).IsEqualTo(expected.UiSchemaJson);
        await Assert.That(version.LogicSchemaArtifact).IsEqualTo(expected.LogicSchemaJson);
        await Assert.That(version.MappingArtifact).IsEqualTo(expected.MappingArtifactJson);
        await Assert.That(version.SchemaHash).IsEqualTo(expected.SchemaHash);
        await repository.Received(1).UpdateVersionAsync(version, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Publish_WithIncompleteChoiceField_ReturnsStablePreflightFailureCode()
    {
        RegistrationFormAuthoringCommandService service = CreateService(
            out IRegistrationFormAuthoringRepository repository, out Guid tenantId);
        RegistrationFormVersion version = BuildVersion(tenantId);
        RegistrationFormSection section = version.Sections.Single();
        version.AddField(section, RegistrationFormField.Create(Guid.CreateVersion7(), section, 2,
            "platform.registration", "choice", "Choice", RegistrationFieldTypeEnum.SingleChoice, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers, false, false, Now));
        repository.GetVersionForUpdateAsync(version.EventId, version.RegistrationFormId, version.Id,
            Arg.Any<CancellationToken>()).Returns(version);

        BaseCommandResponse<Guid> response = await service.PublishAsync(new(version.EventId, version.RegistrationFormId,
            version.Id, version.ConcurrencyStamp), CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("registration_form_preflight_failed");
        await repository.DidNotReceive().UpdateVersionAsync(Arg.Any<RegistrationFormVersion>(),
            Arg.Any<CancellationToken>());
    }

    private static RegistrationFormAuthoringCommandService CreateService(
        out IRegistrationFormAuthoringRepository repository,
        out Guid tenantId)
    {
        repository = Substitute.For<IRegistrationFormAuthoringRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantId = Guid.CreateVersion7();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        IFormSchemaArtifactGenerator generator = new FormSchemaArtifactGenerator();
        return new(repository, eventRepository, tenantContext, currentUser,
            new RegistrationFormPublishPreflightService(), new FormSchemaArtifactPublicationService(generator),
            new FixedTimeProvider(Now));
    }

    private static RegistrationFormVersion BuildVersion(Guid tenantId)
    {
        RegistrationForm form = RegistrationForm.Create(Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(),
            "platform.registration", "attendee", "Attendee", Now);
        RegistrationFormVersion version = RegistrationFormVersion.Create(Guid.CreateVersion7(), form, 1, "en-US", null, null, Now);
        version.ConcurrencyStamp = Guid.CreateVersion7();
        RegistrationFormSection section = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 1, "Details", Now);
        version.AddSection(section);
        RegistrationFormField field = RegistrationFormField.Create(Guid.CreateVersion7(), section, 1,
            "platform.registration", "email", "Email", RegistrationFieldTypeEnum.Email, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers, false, false, Now);
        version.AddField(section, field);
        return version;
    }

    private sealed class FixedTimeProvider(DateTime value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(value);
    }
}
