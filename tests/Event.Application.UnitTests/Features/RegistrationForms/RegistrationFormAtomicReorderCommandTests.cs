// ABOUTME: Specifies atomic registration-form reorder commands and their application boundary behavior.
// ABOUTME: Locks authorization metadata, validation, complete membership, and optimistic concurrency.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.RegistrationForms.Handlers.Commands;
using Explore.Application.Features.RegistrationForms.Requests.Commands;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.RegistrationForms;

public sealed class RegistrationFormAtomicReorderCommandTests
{
    private static readonly DateTime Now = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ReorderCommands_ReuseRegistrationFormUpdateAuthorization()
    {
        foreach (Type commandType in new[]
                 {
                     typeof(ReorderRegistrationFormSectionsCommand),
                     typeof(ReorderRegistrationFormFieldsCommand)
                 })
        {
            AuthorizeResourceAttribute attribute = commandType.GetCustomAttribute<AuthorizeResourceAttribute>()!;
            await Assert.That(attribute.Resource).IsEqualTo(ResourceKinds.RegistrationForm);
            await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.RegistrationForms.Update);
        }
    }

    [Test]
    public async Task ReorderSections_WithCompleteMembership_PersistsOnceAndReturnsVersionIdentity()
    {
        var fixture = CreateFixture();
        RegistrationFormSection[] sections = [.. fixture.Version.Sections.OrderBy(section => section.Ordinal)];

        var response = await fixture.Service.ReorderSectionsAsync(
            new(fixture.Version.EventId, fixture.Version.RegistrationFormId, fixture.Version.Id,
                [sections[1].Id, sections[0].Id], fixture.Version.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.Id).IsEqualTo(fixture.Version.Id);
        await Assert.That(sections[1].Ordinal).IsEqualTo(1);
        Guid[] expectedOrder = [sections[1].Id, sections[0].Id];
        await fixture.Repository.Received(1).ReorderSectionsAsync(
            fixture.Version, Arg.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(expectedOrder)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReorderFields_WithMalformedMembership_ReturnsStableFailureWithoutPersistence()
    {
        var fixture = CreateFixture();
        RegistrationFormSection section = fixture.Version.Sections.First();
        Guid fieldId = section.Fields.First().Id;

        var response = await fixture.Service.ReorderFieldsAsync(
            new(fixture.Version.EventId, fixture.Version.RegistrationFormId, fixture.Version.Id, section.Id,
                [fieldId, fieldId], fixture.Version.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("registration_form_reorder_invalid");
        await fixture.Repository.DidNotReceive().ReorderFieldsAsync(
            Arg.Any<RegistrationFormVersion>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReorderFields_WithMoreThanTwoHundredIds_ReturnsStableValidationFailure()
    {
        var fixture = CreateFixture();
        RegistrationFormSection section = fixture.Version.Sections.First();
        Guid[] overBoundedOrder = [.. Enumerable.Range(0, 201).Select(_ => Guid.CreateVersion7())];
        var handler = new ReorderRegistrationFormFieldsCommandHandler(fixture.Service);

        var response = await handler.Handle(
            new(fixture.Version.EventId, fixture.Version.RegistrationFormId, fixture.Version.Id, section.Id,
                overBoundedOrder, fixture.Version.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("registration_form_reorder_invalid");
        await fixture.Repository.DidNotReceive().GetVersionForUpdateAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static Fixture CreateFixture()
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationForm form = RegistrationForm.Create(Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(),
            "platform.registration", "attendee", "Attendee", Now);
        RegistrationFormVersion version = RegistrationFormVersion.Create(
            Guid.CreateVersion7(), form, 1, "en-US", null, null, Now);
        RegistrationFormSection first = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 1, "First", Now);
        RegistrationFormSection second = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 2, "Second", Now);
        version.AddSection(first);
        version.AddSection(second);
        version.AddField(first, RegistrationFormField.Create(Guid.CreateVersion7(), first, 1,
            "platform.registration", "first", "First", RegistrationFieldTypeEnum.ShortText, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers, false, false, Now));
        version.AddField(first, RegistrationFormField.Create(Guid.CreateVersion7(), first, 2,
            "platform.registration", "second", "Second", RegistrationFieldTypeEnum.ShortText, 1,
            RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers, false, false, Now));

        IRegistrationFormAuthoringRepository repository = Substitute.For<IRegistrationFormAuthoringRepository>();
        repository.GetVersionForUpdateAsync(version.EventId, version.RegistrationFormId, version.Id,
            Arg.Any<CancellationToken>()).Returns(version);
        var eventRepository = Substitute.For<IEventRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        IFormSchemaArtifactGenerator generator = new FormSchemaArtifactGenerator();
        var service = new RegistrationFormAuthoringCommandService(repository, eventRepository, tenantContext,
            currentUser, new RegistrationFormPublishPreflightService(),
            new FormSchemaArtifactPublicationService(generator), new FixedTimeProvider(Now));
        return new(service, repository, version);
    }

    private sealed record Fixture(
        RegistrationFormAuthoringCommandService Service,
        IRegistrationFormAuthoringRepository Repository,
        RegistrationFormVersion Version);

    private sealed class FixedTimeProvider(DateTime value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(value);
    }
}
