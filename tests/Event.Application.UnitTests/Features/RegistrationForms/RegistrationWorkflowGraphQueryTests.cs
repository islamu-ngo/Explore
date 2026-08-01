// ABOUTME: Verifies the workflow management query composes event-owned form and immutable version summaries.
// ABOUTME: Pins status, hash, provenance, and concurrency metadata without leaking persistence DTO projections.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.RegistrationForms.Handlers.Queries;
using Explore.Application.Features.RegistrationForms.Requests.Queries;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.RegistrationForms;

public sealed class RegistrationWorkflowGraphQueryTests
{
    [Test]
    public async Task GetWorkflow_ComposesEventFormsAndImmutableVersionSummaries()
    {
        DateTime now = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid sourceFormId = Guid.CreateVersion7();
        Guid sourceVersionId = Guid.CreateVersion7();
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenantId, eventId, "registration", now);
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
        workflow.AddRequirement(requirement);
        RegistrationForm form = RegistrationForm.Create(
            tenantId, eventId, "platform.registration", "attendee", "Attendee", now);
        RegistrationFormVersion version = RegistrationFormVersion.Create(
            form, 1, "en-US", sourceFormId, sourceVersionId, now);
        RegistrationFormSection section = RegistrationFormSection.Create(
            Guid.CreateVersion7(), version, 1, "Details", now);
        version.AddSection(section);
        version.AddField(section, RegistrationFormField.Create(
            Guid.CreateVersion7(), section, 1, "platform.registration", "email", "Email",
            RegistrationFieldTypeEnum.Email, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers,
            false, false, now));
        form.AddVersion(version);
        new FormSchemaArtifactPublicationService(new FormSchemaArtifactGenerator()).Publish(version, now.AddMinutes(1));

        IRegistrationFormAuthoringRepository repository = Substitute.For<IRegistrationFormAuthoringRepository>();
        repository.GetWorkflowAsync(eventId, "registration", Arg.Any<CancellationToken>()).Returns(workflow);
        repository.GetFormsAsync(eventId, Arg.Any<CancellationToken>()).Returns([form]);
        repository.GetAttachedRequirementIdsAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { requirement.Id });

        var handler = new GetRegistrationWorkflowQueryHandler(repository);
        var result = await handler.Handle(new GetRegistrationWorkflowQuery(eventId, "registration"), default);

        await Assert.That(result).IsNotNull();
        var formSummary = result!.Forms.Single();
        await Assert.That(formSummary.Id).IsEqualTo(form.Id);
        var versionSummary = formSummary.Versions.Single();
        await Assert.That(versionSummary.Version).IsEqualTo(1);
        await Assert.That(versionSummary.StatusCode).IsEqualTo("PUBLISHED");
        await Assert.That(versionSummary.SchemaHash).IsEqualTo(version.SchemaHash);
        await Assert.That(versionSummary.SourceTemplateFormId).IsEqualTo(sourceFormId);
        await Assert.That(versionSummary.SourceTemplateVersionId).IsEqualTo(sourceVersionId);
        await Assert.That(versionSummary.ConcurrencyStamp).IsEqualTo(version.ConcurrencyStamp);
        await Assert.That(result.Requirements.Single().IsAttached).IsTrue();
        await repository.Received(1).GetFormsAsync(eventId, Arg.Any<CancellationToken>());
        await repository.Received(1).GetAttachedRequirementIdsAsync(eventId, Arg.Any<CancellationToken>());
    }
}
