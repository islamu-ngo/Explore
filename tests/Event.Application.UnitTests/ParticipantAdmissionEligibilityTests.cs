// ABOUTME: Defines RED CQRS contracts for subject-correct completion, approval, and revocation.
// ABOUTME: Requires stable bounded failures, cancellation, and zero-PII command surfaces.

using System.Reflection;
using System.Runtime.CompilerServices;
using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Admissions;
using Explore.Application.Features.Admissions.Handlers.Commands;
using Explore.Application.Features.Admissions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace ApplicationUnitTests;

public sealed class ParticipantAdmissionEligibilityTests
{
    private static readonly DateTime UtcNow =
        new(
            2026,
            8,
            27,
            12,
            0,
            0,
            DateTimeKind.Utc);
    private const string RequestsNamespace =
        "Explore.Application.Features.Admissions.Requests.Commands.";
    private const string HandlersNamespace =
        "Explore.Application.Features.Admissions.Handlers.Commands.";

    [Test]
    public async Task SubjectCompletionCommandAndHandlerExist()
    {
        Assembly application =
            typeof(BaseCommandResponse).Assembly;
        Type? command = application.GetType(
            RequestsNamespace +
            "CompleteParticipantAdmissionCommand");
        Type? handler = application.GetType(
            HandlersNamespace +
            "CompleteParticipantAdmissionCommandHandler");

        await Assert.That(command).IsNotNull();
        await Assert.That(handler).IsNotNull();
    }

    [Test]
    public async Task ApprovalAndRevocationUseSeparateExplicitCommands()
    {
        Assembly application =
            typeof(BaseCommandResponse).Assembly;
        Type? approve = application.GetType(
            RequestsNamespace +
            "ApproveParticipantAdmissionCommand");
        Type? revoke = application.GetType(
            RequestsNamespace +
            "RevokeParticipantAdmissionCommand");

        await Assert.That(approve).IsNotNull();
        await Assert.That(revoke).IsNotNull();
        await Assert.That(approve).IsNotEqualTo(revoke);
    }

    [Test]
    public async Task CommandsExposeOnlyScopeIdentifiersAndNoPii()
    {
        Assembly application =
            typeof(BaseCommandResponse).Assembly;
        string[] commandNames =
        [
            "CompleteParticipantAdmissionCommand",
            "ApproveParticipantAdmissionCommand",
            "RevokeParticipantAdmissionCommand",
        ];
        string[] forbidden =
        [
            "email",
            "phone",
            "name",
            "address",
            "answer",
            "consenttext",
        ];

        foreach (string commandName in commandNames)
        {
            Type? command = application.GetType(
                RequestsNamespace + commandName);
            await Assert.That(command).IsNotNull();
            await Assert.That(
                    command!.GetProperties().Any(property =>
                        forbidden.Any(fragment =>
                            property.Name.Contains(
                                fragment,
                                StringComparison.OrdinalIgnoreCase))))
                .IsFalse();
        }
    }

    [Test]
    public async Task HandlersAcceptCancellationAndReturnStableCommandResponse()
    {
        Assembly application =
            typeof(BaseCommandResponse).Assembly;
        string[] handlerNames =
        [
            "CompleteParticipantAdmissionCommandHandler",
            "ApproveParticipantAdmissionCommandHandler",
            "RevokeParticipantAdmissionCommandHandler",
        ];

        foreach (string handlerName in handlerNames)
        {
            Type? handler = application.GetType(
                HandlersNamespace + handlerName);
            await Assert.That(handler).IsNotNull();
            MethodInfo? handle = handler!.GetMethod("Handle");
            await Assert.That(handle).IsNotNull();
            await Assert.That(
                    handle!.GetParameters().Last().ParameterType)
                .IsEqualTo(typeof(CancellationToken));
            await Assert.That(handle.ReturnType.IsGenericType)
                .IsTrue();
        }
    }

    [Test]
    public async Task StableFailureCodesCoverSubjectEvidenceApprovalAndRevocation()
    {
        Type? codes = typeof(BaseCommandResponse).Assembly
            .GetType(
                "Explore.Application.Features.Admissions." +
                "ParticipantAdmissionFailureCodes");

        await Assert.That(codes).IsNotNull();
        string[] required =
        [
            "ParticipantUnavailable",
            "SubjectAuthorityRequired",
            "CompletionEvidenceIncomplete",
            "ConsentEvidenceRequired",
            "ApprovalUnavailable",
            "AdmissionRevoked",
        ];
        string[] fields = codes!.GetFields(
                BindingFlags.Public
                | BindingFlags.Static)
            .Select(field => field.Name)
            .ToArray();
        await Assert.That(required.All(fields.Contains))
            .IsTrue();
    }

    [Test]
    public async Task CompletionValidationFailsBeforePersistence()
    {
        HandlerDependencies dependencies = CreateDependencies();
        var handler = CreateCompletionHandler(dependencies);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new CompleteParticipantAdmissionCommand(
                Guid.Empty,
                Guid.Empty,
                Guid.Empty,
                Guid.Empty),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await dependencies.Repository
            .DidNotReceiveWithAnyArgs()
            .LoadCompletionForUpdateAsync(
                default,
                default,
                default,
                default,
                default,
                default,
                default);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task EachCompletionScopeIdentifierIsRequired(
        int missingIndex)
    {
        HandlerDependencies dependencies = CreateDependencies();
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                linkedUserId: null,
                consentRequired: false,
                approvalRequired: false);
        Guid[] ids =
        [
            scenario.EventId,
            scenario.OrderId,
            scenario.Assignment.Id,
            scenario.Participant.Id,
        ];
        ids[missingIndex] = Guid.Empty;
        var handler = CreateCompletionHandler(dependencies);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new CompleteParticipantAdmissionCommand(
                ids[0],
                ids[1],
                ids[2],
                ids[3]),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await dependencies.Repository
            .DidNotReceiveWithAnyArgs()
            .LoadCompletionForUpdateAsync(
                default,
                default,
                default,
                default,
                default,
                default,
                default);
    }

    [Test]
    public async Task CompletionCancellationStopsBeforePersistence()
    {
        HandlerDependencies dependencies = CreateDependencies();
        var handler = CreateCompletionHandler(dependencies);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.That(async () =>
                await handler.Handle(
                    CreateCompletionCommand(),
                    cancellation.Token))
            .Throws<OperationCanceledException>();
        await dependencies.Repository
            .DidNotReceiveWithAnyArgs()
            .LoadCompletionForUpdateAsync(
                default,
                default,
                default,
                default,
                default,
                default,
                default);
    }

    [Test]
    public async Task CompletionRequiresAuthenticatedSubjectBeforePersistence()
    {
        HandlerDependencies dependencies =
            CreateDependencies(authenticated: false);
        var handler = CreateCompletionHandler(dependencies);

        BaseCommandResponse<Guid> result = await handler.Handle(
            CreateCompletionCommand(),
            CancellationToken.None);

        await Assert.That(result.FailureCode)
            .IsEqualTo(
                ParticipantAdmissionFailureCodes
                    .SubjectAuthorityRequired);
        await dependencies.Repository
            .DidNotReceiveWithAnyArgs()
            .LoadCompletionForUpdateAsync(
                default,
                default,
                default,
                default,
                default,
                default,
                default);
    }

    [Test]
    public async Task IncompleteCanonicalEvidenceReturnsStableFailure()
    {
        HandlerDependencies dependencies = CreateDependencies();
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                linkedUserId: null,
                consentRequired: false,
                approvalRequired: false);
        dependencies.Repository
            .LoadCompletionForUpdateAsync(
                scenario.TenantId,
                scenario.EventId,
                scenario.OrderId,
                scenario.Assignment.Id,
                scenario.Participant.Id,
                dependencies.UserId,
                Arg.Any<CancellationToken>())
            .Returns(
                new ParticipantAdmissionCompletionContext(
                    scenario.Eligibility,
                    scenario.Participant,
                    RequirementsComplete: false,
                    SubjectConsentRecordId: null));
        var handler = CreateCompletionHandler(dependencies);

        BaseCommandResponse<Guid> result = await handler.Handle(
            CreateCompletionCommand(scenario),
            CancellationToken.None);

        await Assert.That(result.FailureCode)
            .IsEqualTo(
                ParticipantAdmissionFailureCodes
                    .CompletionEvidenceIncomplete);
        await dependencies.Repository
            .DidNotReceiveWithAnyArgs()
            .ApplyDecisionAsync(default!, default);
    }

    [Test]
    public async Task RequiredConsentMustBeSubjectOwnedCanonicalEvidence()
    {
        HandlerDependencies dependencies = CreateDependencies();
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                linkedUserId: null,
                consentRequired: true,
                approvalRequired: false);
        dependencies.Repository
            .LoadCompletionForUpdateAsync(
                scenario.TenantId,
                scenario.EventId,
                scenario.OrderId,
                scenario.Assignment.Id,
                scenario.Participant.Id,
                dependencies.UserId,
                Arg.Any<CancellationToken>())
            .Returns(
                new ParticipantAdmissionCompletionContext(
                    scenario.Eligibility,
                    scenario.Participant,
                    RequirementsComplete: true,
                    SubjectConsentRecordId: null));
        var handler = CreateCompletionHandler(dependencies);

        BaseCommandResponse<Guid> result = await handler.Handle(
            CreateCompletionCommand(scenario),
            CancellationToken.None);

        await Assert.That(result.FailureCode)
            .IsEqualTo(
                ParticipantAdmissionFailureCodes
                    .ConsentEvidenceRequired);
    }

    [Test]
    public async Task CompletionClaimsSubjectAndRecordsOnlyEvidenceReferences()
    {
        HandlerDependencies dependencies = CreateDependencies();
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                linkedUserId: null,
                consentRequired: true,
                approvalRequired: false);
        Guid consentId = Guid.CreateVersion7();
        dependencies.Repository
            .LoadCompletionForUpdateAsync(
                scenario.TenantId,
                scenario.EventId,
                scenario.OrderId,
                scenario.Assignment.Id,
                scenario.Participant.Id,
                dependencies.UserId,
                Arg.Any<CancellationToken>())
            .Returns(
                new ParticipantAdmissionCompletionContext(
                    scenario.Eligibility,
                    scenario.Participant,
                    RequirementsComplete: true,
                    consentId));
        var handler = CreateCompletionHandler(dependencies);

        BaseCommandResponse<Guid> result = await handler.Handle(
            CreateCompletionCommand(scenario),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(scenario.Participant.LinkedUserId)
            .IsEqualTo(dependencies.UserId);
        await Assert.That(
                scenario.Eligibility.SubjectConsentRecordId)
            .IsEqualTo(consentId);
        await Assert.That(
                scenario.Eligibility.DescribeReadiness(
                    orderConfirmed: true,
                    paymentSatisfied: true)
                    .IsReady)
            .IsTrue();
        await dependencies.Repository.Received(1)
            .ApplyDecisionAsync(
                scenario.Eligibility,
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApprovalRequiresServerResolvedTenantActor()
    {
        HandlerDependencies dependencies = CreateDependencies();
        dependencies.Actors
            .GetActorByUserIdAndTenantId(
                dependencies.UserId,
                dependencies.TenantId,
                Arg.Any<CancellationToken>())
            .Returns((Actor?)null);
        var handler =
            new ApproveParticipantAdmissionCommandHandler(
                dependencies.Repository,
                dependencies.Actors,
                dependencies.CurrentUser,
                dependencies.Tenant,
                dependencies.UnitOfWork,
                dependencies.TimeProvider);
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                dependencies.UserId,
                consentRequired: false,
                approvalRequired: true);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApproveParticipantAdmissionCommand(
                scenario.EventId,
                scenario.OrderId,
                scenario.Assignment.Id,
                scenario.Participant.Id),
            CancellationToken.None);

        await Assert.That(result.FailureCode)
            .IsEqualTo(
                ParticipantAdmissionFailureCodes
                    .ApprovalUnavailable);
        await dependencies.Repository
            .DidNotReceiveWithAnyArgs()
            .LoadForUpdateAsync(default, default, default);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    public async Task ApprovalRejectsEachCrossScopeIdentifier(
        int mismatchedIndex)
    {
        HandlerDependencies dependencies = CreateDependencies();
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                dependencies.UserId,
                consentRequired: false,
                approvalRequired: true);
        Actor actor = CreateActor(dependencies.UserId);
        dependencies.Actors
            .GetActorByUserIdAndTenantId(
                dependencies.UserId,
                dependencies.TenantId,
                Arg.Any<CancellationToken>())
            .Returns(actor);
        dependencies.Repository.LoadForUpdateAsync(
                scenario.TenantId,
                scenario.Assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(scenario.Eligibility);
        Guid[] scope =
        [
            scenario.EventId,
            scenario.OrderId,
            scenario.Participant.Id,
        ];
        scope[mismatchedIndex] = Guid.CreateVersion7();
        var handler =
            new ApproveParticipantAdmissionCommandHandler(
                dependencies.Repository,
                dependencies.Actors,
                dependencies.CurrentUser,
                dependencies.Tenant,
                dependencies.UnitOfWork,
                dependencies.TimeProvider);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApproveParticipantAdmissionCommand(
                scope[0],
                scope[1],
                scenario.Assignment.Id,
                scope[2]),
            CancellationToken.None);

        await Assert.That(result.FailureCode)
            .IsEqualTo(
                ParticipantAdmissionFailureCodes
                    .ParticipantUnavailable);
        await Assert.That(scenario.Eligibility.ApprovedAt)
            .IsNull();
    }

    [Test]
    public async Task ApprovalPersistsServerResolvedActorDecision()
    {
        HandlerDependencies dependencies = CreateDependencies();
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                dependencies.UserId,
                consentRequired: false,
                approvalRequired: true);
        Actor actor = CreateActor(dependencies.UserId);
        dependencies.Actors
            .GetActorByUserIdAndTenantId(
                dependencies.UserId,
                dependencies.TenantId,
                Arg.Any<CancellationToken>())
            .Returns(actor);
        dependencies.Repository.LoadForUpdateAsync(
                scenario.TenantId,
                scenario.Assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(scenario.Eligibility);
        var handler =
            new ApproveParticipantAdmissionCommandHandler(
                dependencies.Repository,
                dependencies.Actors,
                dependencies.CurrentUser,
                dependencies.Tenant,
                dependencies.UnitOfWork,
                dependencies.TimeProvider);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ApproveParticipantAdmissionCommand(
                scenario.EventId,
                scenario.OrderId,
                scenario.Assignment.Id,
                scenario.Participant.Id),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(scenario.Eligibility.ApprovedAt)
            .IsEqualTo(UtcNow);
        await Assert.That(
                scenario.Eligibility.ApprovedByActorId)
            .IsEqualTo(actor.Id);
        await dependencies.Repository.Received(1)
            .ApplyDecisionAsync(
                scenario.Eligibility,
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RevocationIsTerminalAndUsesStableOutcome()
    {
        HandlerDependencies dependencies = CreateDependencies();
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                dependencies.UserId,
                consentRequired: false,
                approvalRequired: true);
        Actor actor = CreateActor(dependencies.UserId);
        dependencies.Actors
            .GetActorByUserIdAndTenantId(
                dependencies.UserId,
                dependencies.TenantId,
                Arg.Any<CancellationToken>())
            .Returns(actor);
        dependencies.Repository.LoadForUpdateAsync(
                scenario.TenantId,
                scenario.Assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(scenario.Eligibility);
        dependencies.Repository.GetIssuedTicketForUpdateAsync(
                scenario.TenantId,
                scenario.Assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(CreateActiveTicket());
        var revoke =
            new RevokeParticipantAdmissionCommandHandler(
                dependencies.Repository,
                dependencies.Actors,
                dependencies.CurrentUser,
                dependencies.Tenant,
                dependencies.UnitOfWork,
                dependencies.TimeProvider);

        BaseCommandResponse<Guid> revoked =
            await revoke.Handle(
                new RevokeParticipantAdmissionCommand(
                    scenario.EventId,
                    scenario.OrderId,
                    scenario.Assignment.Id,
                    scenario.Participant.Id),
                CancellationToken.None);

        await Assert.That(revoked.IsSuccess).IsTrue();
        await Assert.That(scenario.Eligibility.RevokedAt)
            .IsEqualTo(UtcNow);
        AdmissionTicket issued =
            await dependencies.Repository
                .GetIssuedTicketForUpdateAsync(
                    scenario.TenantId,
                    scenario.Assignment.Id,
                    CancellationToken.None)
            ?? throw new InvalidOperationException();
        await Assert.That(issued.AdmissionTicketStatusId)
            .IsEqualTo(
                (int)AdmissionTicketStatusEnum.Revoked);
        await dependencies.Repository.Received(1)
            .ApplyDecisionAsync(
                scenario.Eligibility,
                Arg.Any<CancellationToken>());
        var approve =
            new ApproveParticipantAdmissionCommandHandler(
                dependencies.Repository,
                dependencies.Actors,
                dependencies.CurrentUser,
                dependencies.Tenant,
                dependencies.UnitOfWork,
                dependencies.TimeProvider);
        BaseCommandResponse<Guid> replay =
            await approve.Handle(
                new ApproveParticipantAdmissionCommand(
                    scenario.EventId,
                    scenario.OrderId,
                    scenario.Assignment.Id,
                    scenario.Participant.Id),
                CancellationToken.None);
        await Assert.That(replay.FailureCode)
            .IsEqualTo(
                ParticipantAdmissionFailureCodes
                    .AdmissionRevoked);
    }

    [Test]
    public async Task RevocationDoesNotTransitionInactiveTicket()
    {
        HandlerDependencies dependencies = CreateDependencies();
        EligibilityScenario scenario =
            CreateScenario(
                dependencies.TenantId,
                dependencies.UserId,
                consentRequired: false,
                approvalRequired: true);
        Actor actor = CreateActor(dependencies.UserId);
        AdmissionTicket suspended = CreateTicket(
            AdmissionTicketStatusEnum.Suspended);
        dependencies.Actors
            .GetActorByUserIdAndTenantId(
                dependencies.UserId,
                dependencies.TenantId,
                Arg.Any<CancellationToken>())
            .Returns(actor);
        dependencies.Repository.LoadForUpdateAsync(
                scenario.TenantId,
                scenario.Assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(scenario.Eligibility);
        dependencies.Repository.GetIssuedTicketForUpdateAsync(
                scenario.TenantId,
                scenario.Assignment.Id,
                Arg.Any<CancellationToken>())
            .Returns(suspended);
        var handler =
            new RevokeParticipantAdmissionCommandHandler(
                dependencies.Repository,
                dependencies.Actors,
                dependencies.CurrentUser,
                dependencies.Tenant,
                dependencies.UnitOfWork,
                dependencies.TimeProvider);

        BaseCommandResponse<Guid> result = await handler.Handle(
            new RevokeParticipantAdmissionCommand(
                scenario.EventId,
                scenario.OrderId,
                scenario.Assignment.Id,
                scenario.Participant.Id),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(suspended.AdmissionTicketStatusId)
            .IsEqualTo(
                (int)AdmissionTicketStatusEnum.Suspended);
    }

    private static CompleteParticipantAdmissionCommand
        CreateCompletionCommand(
            EligibilityScenario? scenario = null)
    {
        scenario ??= CreateScenario(
            linkedUserId: null,
            consentRequired: false,
            approvalRequired: false);
        return new CompleteParticipantAdmissionCommand(
            scenario.EventId,
            scenario.OrderId,
            scenario.Assignment.Id,
            scenario.Participant.Id);
    }

    private static CompleteParticipantAdmissionCommandHandler
        CreateCompletionHandler(
            HandlerDependencies dependencies) =>
        new(
            dependencies.Repository,
            dependencies.CurrentUser,
            dependencies.Tenant,
            dependencies.UnitOfWork,
            dependencies.TimeProvider);

    private static HandlerDependencies CreateDependencies(
        bool authenticated = true)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        var currentUser =
            Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(authenticated);
        currentUser.UserId.Returns(
            authenticated ? userId : null);
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(tenantId);
        return new HandlerDependencies(
            tenantId,
            userId,
            Substitute.For<
                IParticipantAdmissionEligibilityRepository>(),
            Substitute.For<IActorRepository>(),
            currentUser,
            tenant,
            new InlineUnitOfWork(),
            new FixedTimeProvider(UtcNow));
    }

    private static EligibilityScenario CreateScenario(
        Guid? tenantId = null,
        Guid? linkedUserId = null,
        bool consentRequired = false,
        bool approvalRequired = false)
    {
        Guid effectiveTenantId =
            tenantId ?? Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        RegistrationParticipant participant =
            RegistrationParticipant.Create(
                effectiveTenantId,
                orderId,
                linkedUserId,
                ParticipantTypeEnum.Adult,
                guardian: null);
        RegistrationTicketAssignment assignment =
            RegistrationTicketAssignment.Create(
                effectiveTenantId,
                orderId,
                Guid.CreateVersion7(),
                1,
                participant.Id,
                AssignmentStatusEnum.Assigned,
                assignmentDeadline: null,
                UtcNow);
        ParticipantAdmissionEligibility eligibility =
            ParticipantAdmissionEligibility.Create(
                effectiveTenantId,
                eventId,
                assignment,
                participant,
                consentRequired,
                approvalRequired,
                UtcNow);
        return new EligibilityScenario(
            effectiveTenantId,
            eventId,
            orderId,
            participant,
            assignment,
            eligibility);
    }

    private static Actor CreateActor(Guid userId) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = userId,
            Pii = new ActorPii
            {
                DisplayName = "Readiness operator",
            },
        };

    private static AdmissionTicket CreateActiveTicket() =>
        CreateTicket(AdmissionTicketStatusEnum.Active);

    private static AdmissionTicket CreateTicket(
        AdmissionTicketStatusEnum status)
    {
        var ticket = (AdmissionTicket)
            RuntimeHelpers.GetUninitializedObject(
                typeof(AdmissionTicket));
        typeof(AdmissionTicket)
            .GetField(
                "_credentials",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(
                ticket,
                new List<AdmissionTicketCredential>());
        typeof(AdmissionTicket)
            .GetProperty(
                nameof(AdmissionTicket.AdmissionTicketStatusId))!
            .SetValue(
                ticket,
                (int)status);
        return ticket;
    }

    private sealed record HandlerDependencies(
        Guid TenantId,
        Guid UserId,
        IParticipantAdmissionEligibilityRepository Repository,
        IActorRepository Actors,
        ICurrentUserService CurrentUser,
        ITenantContext Tenant,
        IUnitOfWork UnitOfWork,
        TimeProvider TimeProvider);

    private sealed record EligibilityScenario(
        Guid TenantId,
        Guid EventId,
        Guid OrderId,
        RegistrationParticipant Participant,
        RegistrationTicketAssignment Assignment,
        ParticipantAdmissionEligibility Eligibility);

    private sealed class FixedTimeProvider(
        DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(utcNow);
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            operation(ct);
    }
}
