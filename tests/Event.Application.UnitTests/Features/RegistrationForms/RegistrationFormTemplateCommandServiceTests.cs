// ABOUTME: Verifies registration-form template application-service authority and tenant boundaries.
// ABOUTME: Covers platform admin creation, tenant source isolation, published-only sources, and instantiation target fences.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationForms;
using Explore.Application.Exceptions;
using Explore.Application.Features.RegistrationForms.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using NSubstitute;

namespace Event.Application.UnitTests.Features.RegistrationForms;

public sealed class RegistrationFormTemplateCommandServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task CreateAsync_RequiresPublishedSourceTenantVisibilityAndInstanceAdminForPlatformTemplates()
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationFormVersion draft = Source(tenantId, published: false);
        RegistrationFormVersion otherTenantPublished = Source(Guid.CreateVersion7(), published: true);
        IRegistrationFormAuthoringRepository forms = Substitute.For<IRegistrationFormAuthoringRepository>();
        forms.GetTemplateSourceVersionAsync(draft.EventId, draft.RegistrationFormId, draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        forms.GetTemplateSourceVersionAsync(otherTenantPublished.EventId, otherTenantPublished.RegistrationFormId, otherTenantPublished.Id, Arg.Any<CancellationToken>()).Returns(otherTenantPublished);
        RegistrationFormTemplateCommandService service = Service(tenantId, forms, isInstanceAdmin: false);

        BaseCommandResponse<Guid> draftResult = await service.CreateAsync(
            new CreateRegistrationFormTemplateCommand(Input(draft, isPlatformOwned: false)), CancellationToken.None);

        await Assert.That(draftResult.IsSuccess).IsFalse();
        await Assert.That(draftResult.FailureCode).IsEqualTo("registration_form_template_source_not_published");
        await Assert.That(() => service.CreateAsync(
                new CreateRegistrationFormTemplateCommand(Input(otherTenantPublished, isPlatformOwned: false)), CancellationToken.None))
            .Throws<NotFoundException>();
        await Assert.That(() => service.CreateAsync(
                new CreateRegistrationFormTemplateCommand(Input(otherTenantPublished, isPlatformOwned: true)), CancellationToken.None))
            .Throws<AuthorizationException>();

        RegistrationFormTemplateCommandService platformService = Service(tenantId, forms, isInstanceAdmin: true);
        BaseCommandResponse<Guid> platformResult = await platformService.CreateAsync(
            new CreateRegistrationFormTemplateCommand(Input(otherTenantPublished, isPlatformOwned: true)), CancellationToken.None);

        await Assert.That(platformResult.IsSuccess).IsTrue();
    }

    [Test]
    public async Task InstantiateAsync_UsesVisibleTemplateAndCurrentTenantWorkflowWithoutRewritingTemplateSource()
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationFormVersion source = Source(tenantId, published: true);
        RegistrationFormTemplate template = RegistrationFormTemplate.Create(
            tenantId, "Template", "Description", "Registration", null, source, Now.AddMinutes(2));
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(
            tenantId, source.EventId, "registration", Now.AddMinutes(3));
        workflow.ConcurrencyStamp = Guid.CreateVersion7();
        Explore.Domain.Event targetEvent = new(EventStatusEnum.Draft)
        {
            Id = source.EventId,
            TenantId = tenantId,
            Tenant = null!,
            ActorId = Guid.CreateVersion7(),
            Actor = null!,
            Title = "Target",
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            EventProvenanceTypeId = 1,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        IRegistrationFormTemplateRepository templates = Substitute.For<IRegistrationFormTemplateRepository>();
        IRegistrationFormAuthoringRepository forms = Substitute.For<IRegistrationFormAuthoringRepository>();
        IEventRepository events = Substitute.For<IEventRepository>();
        templates.GetAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        forms.GetWorkflowForUpdateAsync(source.EventId, workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        forms.GetTemplateSourceVersionAsync(source.EventId, source.RegistrationFormId, source.Id, Arg.Any<CancellationToken>()).Returns(source);
        events.GetAuthorizationTargetByIdAsync(source.EventId, Arg.Any<CancellationToken>()).Returns(targetEvent);
        RegistrationFormTemplateCommandService service = Service(tenantId, forms, templates, events, isInstanceAdmin: false);

        BaseCommandResponse<Guid> result = await service.InstantiateAsync(
            new InstantiateRegistrationFormTemplateCommand(template.Id, new InstantiateRegistrationFormTemplateInputDto(
                source.EventId, workflow.Id, "tenant.registration", "copy", "Copy", workflow.ConcurrencyStamp)), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await forms.Received(1).CreateFormAsync(Arg.Is<RegistrationForm>(form =>
            form.TenantId == tenantId &&
            form.EventId == source.EventId &&
            form.Versions.Single().SourceTemplateFormId == source.RegistrationFormId &&
            form.Versions.Single().SourceTemplateVersionId == source.Id &&
            form.Versions.Single().Sections.Single().Fields.Single().RegistrationFormId == form.Id),
            Arg.Any<CancellationToken>());
        await Assert.That(template.SourceRegistrationFormVersionId).IsEqualTo(source.Id);
    }

    [Test]
    public async Task InstantiateAsync_HidesCrossTenantTemplateWorkflowAndEvent()
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationFormVersion source = Source(tenantId, published: true);
        RegistrationFormTemplate otherTemplate = RegistrationFormTemplate.Create(
            Guid.CreateVersion7(), "Other", "Description", "Registration", null, source, Now.AddMinutes(2));
        IRegistrationFormTemplateRepository templates = Substitute.For<IRegistrationFormTemplateRepository>();
        templates.GetAsync(otherTemplate.Id, Arg.Any<CancellationToken>()).Returns(otherTemplate);
        RegistrationFormTemplateCommandService service = Service(tenantId, Substitute.For<IRegistrationFormAuthoringRepository>(), templates, Substitute.For<IEventRepository>(), false);

        await Assert.That(() => service.InstantiateAsync(new InstantiateRegistrationFormTemplateCommand(
                otherTemplate.Id,
                new InstantiateRegistrationFormTemplateInputDto(source.EventId, Guid.CreateVersion7(), "tenant", "copy", "Copy", Guid.CreateVersion7())), CancellationToken.None))
            .Throws<NotFoundException>();
    }

    [Test]
    public async Task InstantiateCommand_UsesEventWorkflowAuthorityInsteadOfTemplateIdAuthority()
    {
        AuthorizeResourceAttribute attribute = typeof(InstantiateRegistrationFormTemplateCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: false)
            .Cast<AuthorizeResourceAttribute>()
            .Single();
        var command = new InstantiateRegistrationFormTemplateCommand(
            Guid.CreateVersion7(),
            new InstantiateRegistrationFormTemplateInputDto(Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant", "copy", "Copy", Guid.CreateVersion7()));

        await Assert.That(attribute.Resource).IsEqualTo(ResourceKinds.Event);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.Events.ManageRegistrationWorkflow);
        await Assert.That(((ISecureRequest)command).ResourceId).IsEqualTo(command.Input.EventId.ToString());
    }

    private static RegistrationFormTemplateCommandService Service(
        Guid tenantId,
        IRegistrationFormAuthoringRepository forms,
        IRegistrationFormTemplateRepository? templates = null,
        IEventRepository? events = null,
        bool isInstanceAdmin = false)
    {
        IAdminContext admin = Substitute.For<IAdminContext>();
        admin.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(isInstanceAdmin);
        ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        return new RegistrationFormTemplateCommandService(
            templates ?? Substitute.For<IRegistrationFormTemplateRepository>(),
            forms,
            events ?? Substitute.For<IEventRepository>(),
            new TestTenantContext(tenantId),
            currentUser,
            admin,
            new FixedTimeProvider(Now.AddMinutes(10)));
    }

    private static RegistrationFormTemplateInputDto Input(RegistrationFormVersion source, bool isPlatformOwned) => new(
        "Template", "Description", "Registration", null,
        source.EventId, source.RegistrationFormId, source.Id, isPlatformOwned);

    private static RegistrationFormVersion Source(Guid tenantId, bool published)
    {
        RegistrationForm form = RegistrationForm.Create(tenantId, Guid.CreateVersion7(),
            "platform.registration", Guid.NewGuid().ToString("N"), "Source", Now);
        RegistrationFormVersion version = RegistrationFormVersion.Create(form, 1, "en", null, null, Now);
        RegistrationFormSection section = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 1, "Details", Now);
        RegistrationFormField field = RegistrationFormField.Create(Guid.CreateVersion7(), section, 1,
            "person", "email", "Email", RegistrationFieldTypeEnum.Email, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers, false, true, Now);
        version.AddSection(section);
        version.AddField(section, field);
        form.AddVersion(version);
        if (published)
        {
            new FormSchemaArtifactPublicationService(new FormSchemaArtifactGenerator()).Publish(version, Now.AddMinutes(1));
        }

        return version;
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
