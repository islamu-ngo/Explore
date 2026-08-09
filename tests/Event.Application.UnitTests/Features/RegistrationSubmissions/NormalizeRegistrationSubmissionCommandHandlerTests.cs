// ABOUTME: Proves native submission orchestration uses Phase 7 cross-field rules and persists only safe issues on rejection.
// ABOUTME: Verifies invalid submissions produce no answers and never pass rejected attendee content to persistence.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using Explore.Domain.ValueObjects;
using MediatR;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.RegistrationSubmissions;

public sealed class NormalizeRegistrationSubmissionCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task CrossFieldRequireRuleRecordsSafeIssueAndPersistsNoAnswer()
    {
        SubmissionScope scope = CreateScope();
        IRegistrationSubmissionRepository submissions = Substitute.For<IRegistrationSubmissionRepository>();
        IRegistrationFormAuthoringRepository forms = Substitute.For<IRegistrationFormAuthoringRepository>();
        IRegistrationInventoryRepository inventory = Substitute.For<IRegistrationInventoryRepository>();
        IRegistrationParticipantRepository participants = Substitute.For<IRegistrationParticipantRepository>();
        IRegistrationSensitiveValueProtector protector = Substitute.For<IRegistrationSensitiveValueProtector>();
        ISender sender = Substitute.For<ISender>();
        submissions.GetSubmissionAsync(scope.TenantId, scope.Submission.Id, Arg.Any<CancellationToken>())
            .Returns(scope.Submission);
        submissions.GetRequirementAsync(scope.TenantId, scope.Requirement.Id, Arg.Any<CancellationToken>())
            .Returns(scope.Requirement);
        forms.GetVersionAsync(scope.EventId, scope.Form.Id, scope.Version.Id, Arg.Any<CancellationToken>())
            .Returns(scope.Version);
        inventory.GetOrderWithLinesAsync(scope.OrderId, scope.TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(scope));
        IReadOnlyCollection<RegistrationAnswer>? persistedAnswers = null;
        IReadOnlyCollection<RegistrationConsentRecord>? persistedConsents = null;
        IReadOnlyCollection<RegistrationSubmissionIssue>? persistedIssues = null;
        submissions.PersistNormalizationAsync(
                Arg.Do<IReadOnlyCollection<RegistrationAnswer>>(answers => persistedAnswers = answers),
                Arg.Do<IReadOnlyCollection<RegistrationConsentRecord>>(consents => persistedConsents = consents),
                Arg.Do<IReadOnlyCollection<RegistrationSubmissionIssue>>(issues => persistedIssues = issues),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        using JsonDocument value = JsonDocument.Parse("true");
        NormalizeRegistrationSubmissionCommand command = new(scope.TenantId, scope.Submission.Id,
        [
            new(scope.Trigger.Id, RegistrationAnswerSubjectTypeEnum.RegistrationOrder, scope.OrderId, null,
                value.RootElement.Clone())
        ]);

        RegistrationSubmissionNormalizationResult result = await new NormalizeRegistrationSubmissionCommandHandler(
            inventory, submissions, forms, participants, protector, sender, TimeProvider.System)
            .Handle(command, CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(persistedAnswers).IsNotNull();
        await Assert.That(persistedAnswers!).IsEmpty();
        await Assert.That(persistedConsents!).IsEmpty();
        await Assert.That(persistedIssues).IsNotNull();
        await Assert.That(persistedIssues!.Single().Code).IsEqualTo("REQUIRED_FIELD_MISSING");
        await Assert.That(typeof(RegistrationSubmissionIssue).GetProperties()
            .Any(property => property.Name.Contains("Value", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Message", StringComparison.OrdinalIgnoreCase))).IsFalse();
        await sender.DidNotReceive().Send(
            Arg.Any<RecordRegistrationRequirementFulfillmentCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RequiredFieldsMustBeCompleteForEachParticipantSubject()
    {
        SubmissionScope scope = CreateScope(RegistrationRequirementSubjectTypeEnum.EveryParticipant);
        scope.Version.UpdateFieldValidation(scope.Target, true, false, null, null, null, null, null, null, null, null);
        scope.Version.UpdateFieldValidation(scope.Consent, true, false, null, null, null, null, null, null, null, null);
        IRegistrationSubmissionRepository submissions = Substitute.For<IRegistrationSubmissionRepository>();
        IRegistrationFormAuthoringRepository forms = Substitute.For<IRegistrationFormAuthoringRepository>();
        IRegistrationInventoryRepository inventory = Substitute.For<IRegistrationInventoryRepository>();
        IRegistrationParticipantRepository participants = Substitute.For<IRegistrationParticipantRepository>();
        IRegistrationSensitiveValueProtector protector = Substitute.For<IRegistrationSensitiveValueProtector>();
        ISender sender = Substitute.For<ISender>();
        submissions.GetSubmissionAsync(scope.TenantId, scope.Submission.Id, Arg.Any<CancellationToken>())
            .Returns(scope.Submission);
        submissions.GetRequirementAsync(scope.TenantId, scope.Requirement.Id, Arg.Any<CancellationToken>())
            .Returns(scope.Requirement);
        forms.GetVersionAsync(scope.EventId, scope.Form.Id, scope.Version.Id, Arg.Any<CancellationToken>())
            .Returns(scope.Version);
        RegistrationOrder order = CreateOrder(scope);
        inventory.GetOrderWithLinesAsync(scope.OrderId, scope.TenantId, Arg.Any<CancellationToken>()).Returns(order);
        submissions.PersistNormalizationAsync(
                Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(),
                Arg.Any<IReadOnlyCollection<RegistrationConsentRecord>>(),
                Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        Guid firstParticipantId = Guid.CreateVersion7();
        Guid secondParticipantId = Guid.CreateVersion7();
        participants.GetParticipantsByOrderAsync(scope.OrderId, scope.TenantId, Arg.Any<CancellationToken>()).Returns(
        [
            RegistrationParticipant.Create(firstParticipantId, scope.TenantId, scope.OrderId, null, ParticipantTypeEnum.Adult, null),
            RegistrationParticipant.Create(secondParticipantId, scope.TenantId, scope.OrderId, null, ParticipantTypeEnum.Adult, null)
        ]);
        using JsonDocument text = JsonDocument.Parse("\"Participant one\"");
        using JsonDocument consent = JsonDocument.Parse("true");
        NormalizeRegistrationSubmissionCommand command = new(scope.TenantId, scope.Submission.Id,
        [
            new(scope.Target.Id, RegistrationAnswerSubjectTypeEnum.Participant, firstParticipantId, null,
                text.RootElement.Clone()),
            new(scope.Consent.Id, RegistrationAnswerSubjectTypeEnum.Participant, secondParticipantId, null,
                consent.RootElement.Clone())
        ]);

        RegistrationSubmissionNormalizationResult result = await new NormalizeRegistrationSubmissionCommandHandler(
            inventory, submissions, forms, participants, protector, sender, TimeProvider.System)
            .Handle(command, CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count(issue => issue.Code == "REQUIRED_FIELD_MISSING")).IsEqualTo(2);
        await sender.DidNotReceive().Send(
            Arg.Any<RecordRegistrationRequirementFulfillmentCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GrantedConsentProducesNoOrdinaryAnswerOrIssue()
    {
        SubmissionScope scope = CreateScope();
        IRegistrationSubmissionRepository submissions = Substitute.For<IRegistrationSubmissionRepository>();
        IRegistrationFormAuthoringRepository forms = Substitute.For<IRegistrationFormAuthoringRepository>();
        IRegistrationInventoryRepository inventory = Substitute.For<IRegistrationInventoryRepository>();
        IRegistrationParticipantRepository participants = Substitute.For<IRegistrationParticipantRepository>();
        IRegistrationSensitiveValueProtector protector = Substitute.For<IRegistrationSensitiveValueProtector>();
        ISender sender = Substitute.For<ISender>();
        submissions.GetSubmissionAsync(scope.TenantId, scope.Submission.Id, Arg.Any<CancellationToken>()).Returns(scope.Submission);
        submissions.GetRequirementAsync(scope.TenantId, scope.Requirement.Id, Arg.Any<CancellationToken>()).Returns(scope.Requirement);
        forms.GetVersionAsync(scope.EventId, scope.Form.Id, scope.Version.Id, Arg.Any<CancellationToken>()).Returns(scope.Version);
        inventory.GetOrderWithLinesAsync(scope.OrderId, scope.TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(scope));
        IReadOnlyCollection<RegistrationAnswer>? persistedAnswers = null;
        IReadOnlyCollection<RegistrationConsentRecord>? persistedConsents = null;
        IReadOnlyCollection<RegistrationSubmissionIssue>? persistedIssues = null;
        submissions.PersistNormalizationAsync(
                Arg.Do<IReadOnlyCollection<RegistrationAnswer>>(answers => persistedAnswers = answers),
                Arg.Do<IReadOnlyCollection<RegistrationConsentRecord>>(consents => persistedConsents = consents),
                Arg.Do<IReadOnlyCollection<RegistrationSubmissionIssue>>(issues => persistedIssues = issues),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        using JsonDocument value = JsonDocument.Parse("true");
        NormalizeRegistrationSubmissionCommand command = new(scope.TenantId, scope.Submission.Id,
        [new(scope.Consent.Id, RegistrationAnswerSubjectTypeEnum.RegistrationOrder, scope.OrderId, null, value.RootElement.Clone())]);

        RegistrationSubmissionNormalizationResult result = await new NormalizeRegistrationSubmissionCommandHandler(
            inventory, submissions, forms, participants, protector, sender, TimeProvider.System)
            .Handle(command, CancellationToken.None);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.AnswerCount).IsEqualTo(0);
        await Assert.That(persistedAnswers!).IsEmpty();
        RegistrationConsentRecord persisted = persistedConsents!.Single();
        await Assert.That(persisted.PurposeCode).IsEqualTo("EVENT_UPDATES");
        await Assert.That(persisted.ConsentTextSnapshot).IsEqualTo("I agree to receive event updates by email.");
        await Assert.That(persisted.RegistrationFormVersion).IsEqualTo(1);
        await Assert.That(persisted.LanguageTag).IsEqualTo("en");
        await Assert.That(persisted.OrderSubjectId).IsEqualTo(scope.OrderId);
        await Assert.That(persistedIssues!).IsEmpty();
        await sender.Received(1).Send(
            Arg.Is<RecordRegistrationRequirementFulfillmentCommand>(command =>
                command.RegistrationSubmissionId == scope.Submission.Id &&
                command.SubjectType == RegistrationAnswerSubjectTypeEnum.RegistrationOrder &&
                command.SubjectId == scope.OrderId &&
                !command.IsSkipped),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NativeSubmissionPersistsConsentAndFulfillmentInOneCombinedOperation()
    {
        SubmissionScope scope = CreateScope();
        DateTime now = DateTime.UtcNow;
        RegistrationAttempt attempt = RegistrationAttempt.Create(
            scope.TenantId, scope.EventId, scope.OrderId, scope.Requirement.RegistrationWorkflowId,
            scope.Requirement.Id, Guid.CreateVersion7(), scope.Form.Id, scope.Version.Id,
            CapabilityTokenHash.Create(Hash("native-capability")), null, null, now, now.AddMinutes(10));
        attempt.ConcurrencyStamp = Guid.CreateVersion7();
        IRegistrationInventoryRepository inventory = Substitute.For<IRegistrationInventoryRepository>();
        IRegistrationSubmissionRepository submissions = Substitute.For<IRegistrationSubmissionRepository>();
        IRegistrationFormAuthoringRepository forms = Substitute.For<IRegistrationFormAuthoringRepository>();
        IRegistrationParticipantRepository participantRepository = Substitute.For<IRegistrationParticipantRepository>();
        IRegistrationSensitiveValueProtector protector = Substitute.For<IRegistrationSensitiveValueProtector>();
        IGuestCapabilityTokenService capabilities = Substitute.For<IGuestCapabilityTokenService>();
        ISender sender = Substitute.For<ISender>();
        RegistrationOrder order = RegistrationOrder.Create(
            scope.OrderId, scope.TenantId, scope.EventId, Guid.CreateVersion7(), null, BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(), RegistrationParticipationSnapshot.Create(
                Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            scope.Requirement.RegistrationWorkflowId, null, "EUR", now, now.AddMinutes(15));
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingIdentity, now);
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingParticipantDetails, now);
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, now);
        inventory.GetOrderWithLinesAsync(scope.OrderId, scope.TenantId, Arg.Any<CancellationToken>()).Returns(order);
        submissions.GetAttemptAsync(scope.TenantId, attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        submissions.GetRequirementAsync(scope.TenantId, scope.Requirement.Id, Arg.Any<CancellationToken>()).Returns(scope.Requirement);
        forms.GetVersionAsync(scope.EventId, scope.Form.Id, scope.Version.Id, Arg.Any<CancellationToken>()).Returns(scope.Version);
        capabilities.Matches(Arg.Any<string?>(), Arg.Any<CapabilityTokenHash>()).Returns(true);
        IReadOnlyCollection<RegistrationConsentRecord>? persistedConsents = null;
        submissions.PersistAcceptedWithNormalizationAsync(
                Arg.Any<RegistrationAttempt>(),
                Arg.Any<RegistrationSubmission>(),
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<RegistrationAnswer>>(),
                Arg.Do<IReadOnlyCollection<RegistrationConsentRecord>>(records => persistedConsents = records),
                Arg.Any<IReadOnlyCollection<RegistrationSubmissionIssue>>(),
                Arg.Any<IReadOnlyCollection<RegistrationRequirementFulfillment>>(),
                Arg.Any<CancellationToken>())
            .Returns(info => Task.FromResult(new RegistrationSubmissionPersistenceResult(
                RegistrationSubmissionPersistenceOutcome.Inserted, info.ArgAt<RegistrationSubmission>(1))));
        using JsonDocument value = JsonDocument.Parse("true");
        SubmitNativeRegistrationAttemptCommand command = new(
            scope.TenantId, scope.EventId, scope.OrderId, scope.Requirement.Id, attempt.Id,
            "capability", "idempotency", [new(scope.Consent.Id, RegistrationAnswerSubjectTypeEnum.RegistrationOrder,
                scope.OrderId, null, value.RootElement.Clone())]);

        NativeRegistrationSubmissionResult result = await new SubmitNativeRegistrationAttemptCommandHandler(
            inventory, submissions, forms, participantRepository, protector, capabilities, TimeProvider.System)
            .Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(persistedConsents).IsNotNull();
        await Assert.That(persistedConsents!.Single().OrderSubjectId).IsEqualTo(scope.OrderId);
        await Assert.That(persistedConsents.Single().ConsentTextSnapshot)
            .IsEqualTo("I agree to receive event updates by email.");
        await submissions.DidNotReceive().PersistAcceptedAsync(
            Arg.Any<RegistrationAttempt>(), Arg.Any<RegistrationSubmission>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await Assert.That(typeof(IRegistrationSubmissionRepository)
            .GetMethod(nameof(IRegistrationSubmissionRepository.PersistAcceptedWithNormalizationAsync))!
            .GetParameters()
            .Any(parameter => parameter.ParameterType == typeof(IReadOnlyCollection<RegistrationRequirementFulfillment>)))
            .IsTrue();
        await sender.DidNotReceive().Send(
            Arg.Any<RecordRegistrationRequirementFulfillmentCommand>(), Arg.Any<CancellationToken>());
    }

    private static SubmissionScope CreateScope(
        RegistrationRequirementSubjectTypeEnum subjectType = RegistrationRequirementSubjectTypeEnum.AllOrders)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenantId, eventId, "ATTENDEE_REGISTRATION", UtcNow);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow, 1, RegistrationRequirementCriticalityEnum.Required, false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration,
            RegistrationAnswerSyncModeEnum.FULL_CANONICAL, subjectType, null, UtcNow);
        RegistrationChannel channel = RegistrationChannel.Create(requirement, 1, true, null, UtcNow);
        RegistrationForm form = RegistrationForm.Create(tenantId, eventId, "platform.registration", "native", "Native", UtcNow);
        RegistrationFormVersion version = RegistrationFormVersion.Create(form, 1, "en", null, null, UtcNow);
        RegistrationFormSection section = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 1, "Details", UtcNow);
        RegistrationFormField trigger = RegistrationFormField.Create(
            Guid.CreateVersion7(), section, 1, "registration", "attending", "Attending",
            RegistrationFieldTypeEnum.Boolean, 1, RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers,
            false, false, UtcNow);
        RegistrationFormField target = RegistrationFormField.Create(
            Guid.CreateVersion7(), section, 2, "registration", "details", "Details",
            RegistrationFieldTypeEnum.ShortText, 1, RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers,
            false, false, UtcNow);
        RegistrationFormField consent = RegistrationFormField.Create(
            Guid.CreateVersion7(), section, 3, "registration", "marketing_consent", "Send me event updates",
            RegistrationFieldTypeEnum.Consent, 1, RegistrationOrganizerVisibilityEnum.Hidden,
            true, false, UtcNow, "EVENT_UPDATES", "2026-08", "I agree to receive event updates by email.");
        version.AddSection(section);
        version.AddField(section, trigger);
        version.AddField(section, target);
        version.AddField(section, consent);
        version.AddRule(RegistrationFormRule.Create(Guid.CreateVersion7(), version, 1,
            new(target.Namespace, target.Key), RegistrationFormRuleEffect.Require,
            new FormCondition.EqualsCondition(new(trigger.Namespace, trigger.Key), FormScalarValue.From(true)), UtcNow));
        form.AddVersion(version);
        RegistrationAttempt attempt = RegistrationAttempt.Create(
            tenantId, eventId, orderId, workflow.Id, requirement.Id, channel.Id, form.Id, version.Id,
            CapabilityTokenHash.Create(Hash("capability")), null, null, UtcNow, UtcNow.AddMinutes(10));
        RegistrationSubmission submission = attempt.SubmitNative(
            RegistrationEvidenceHash.Create(Hash("evidence")), UtcNow.AddMinutes(1), null);
        return new(tenantId, eventId, orderId, form, version, trigger, target, consent, requirement, submission);
    }

    private static RegistrationOrder CreateOrder(SubmissionScope scope) => RegistrationOrder.Create(
        scope.OrderId,
        scope.TenantId,
        scope.EventId,
        Guid.CreateVersion7(),
        null,
        BookingPartyTypeEnum.Individual,
        Guid.CreateVersion7(),
        RegistrationParticipationSnapshot.Create(
            Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
        scope.Requirement.RegistrationWorkflowId,
        null,
        "EUR",
        UtcNow,
        UtcNow.AddMinutes(15));

    private static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record SubmissionScope(
        Guid TenantId,
        Guid EventId,
        Guid OrderId,
        RegistrationForm Form,
        RegistrationFormVersion Version,
        RegistrationFormField Trigger,
        RegistrationFormField Target,
        RegistrationFormField Consent,
        RegistrationRequirement Requirement,
        RegistrationSubmission Submission);
}
