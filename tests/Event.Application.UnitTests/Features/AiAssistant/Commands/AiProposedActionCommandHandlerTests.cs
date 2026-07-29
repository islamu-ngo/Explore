// ABOUTME: Unit tests for confirming and rejecting AI-proposed actions.
// ABOUTME: Verifies tenant ownership, duplicate safety, and CreateEventDraft MediatR execution.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.AiAssistant.Handlers.Commands;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Ai;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Commands;

public sealed class AiProposedActionCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _actionId = Guid.CreateVersion7();
    private readonly Guid _conversationId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IAiConversationRepository _conversationRepository = Substitute.For<IAiConversationRepository>();
    private readonly IOrganizationMemberRepository _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public AiProposedActionCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(_userId);
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(_userId, Arg.Any<string>()).Returns([]);
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(_userId, Arg.Any<string>()).Returns([]);
        _mediator.Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = _eventId,
                Message = "Created"
            });
    }

    [Test]
    public async Task Confirm_WhenUserIsUnauthenticated_FailsBeforeRepositoryOrMediator()
    {
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unauthenticated");
        await _conversationRepository.DidNotReceive().GetProposedActionForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenActionBelongsToDifferentUser_FailsClosedWithoutExecution()
    {
        AiProposedAction action = CreateProposedAction(userId: Guid.CreateVersion7());
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("proposed_action_not_found");
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Proposed);
        await _conversationRepository.DidNotReceive().UpdateProposedActionAsync(Arg.Any<AiProposedAction>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenCreateEventDraftIsProposed_DispatchesCreateEventCommandAndMarksExecuted()
    {
        AiProposedAction action = CreateProposedAction(payloadJson: "{\"title\":\"Community Dinner\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}");
        CreateEventCommand? sentCommand = null;
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_eventId);
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Executed);
        await Assert.That(action.ResultResourceId).IsEqualTo(_eventId);
        await Assert.That(action.ConfirmedBy).IsEqualTo(_userId);
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.Title).IsEqualTo("Community Dinner");
        await Assert.That(sentCommand.Request.Sessions).IsEmpty();
        await Assert.That(sentCommand.Request.Days).IsEmpty();
        await Assert.That(sentCommand.Request.Rooms).IsEmpty();
        await Assert.That(sentCommand.Request.AgendaItems).IsEmpty();
        await _conversationRepository.Received(1).CreateToolExecutionAsync(
            Arg.Is<AiToolExecution>(execution =>
                execution.TenantId == _tenantId
                && execution.ProposedActionId == _actionId
                && execution.ToolName == "CreateEventDraft"
                && execution.Succeeded
                && execution.CompletedAt != null
                && execution.FailureCode == null
                && execution.FailureMessage == null),
            Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).UpdateProposedActionAsync(action, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenCreateEventDraftHasImageInput_StoresPosterAndSetsFeaturedImageId()
    {
        var uploadSessionId = Guid.CreateVersion7();
        var storageObjectId = Guid.CreateVersion7();
        var imageBytes = ValidPngBytes();
        var imageAttachmentsJson = "[{\"mediaType\":\"image/png\",\"data\":\""
            + Convert.ToBase64String(imageBytes)
            + "\",\"fileName\":\"poster.png\",\"sizeBytes\":" + imageBytes.LongLength + "}]";
        AiProposedAction action = CreateProposedAction(
            payloadJson: "{\"title\":\"Poster Event\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}",
            sourceImageAttachmentsJson: imageAttachmentsJson);
        CreateStorageUploadSessionCommand? uploadCommand = null;
        FinalizeStorageUploadSessionCommand? finalizeCommand = null;
        CreateEventCommand? sentCommand = null;
        byte[]? finalizedBytes = null;

        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateStorageUploadSessionCommand>(command => uploadCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<StorageUploadSessionDto>
            {
                Success = true,
                Id = CreateUploadSessionDto(uploadSessionId)
            });
        _mediator.Send(Arg.Do<FinalizeStorageUploadSessionCommand>(command =>
            {
                finalizeCommand = command;
                using var copy = new MemoryStream();
                command.Content.CopyTo(copy);
                finalizedBytes = copy.ToArray();
                if (command.Content.CanSeek)
                {
                    command.Content.Position = 0;
                }
            }), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<StorageUploadSessionDto>
            {
                Success = true,
                Id = CreateUploadSessionDto(uploadSessionId, storageObjectId)
            });
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(uploadCommand).IsNotNull();
        await Assert.That(uploadCommand!.UploadSessionDto.ContentType).IsEqualTo("image/png");
        await Assert.That(uploadCommand.UploadSessionDto.OriginalFileName).IsEqualTo("poster.png");
        await Assert.That(uploadCommand.UploadSessionDto.Purpose).IsEqualTo(StorageObjectPurposes.EventImage);
        await Assert.That(uploadCommand.UploadSessionDto.Visibility).IsEqualTo(StorageObjectVisibilities.PublicImage);
        await Assert.That(finalizeCommand).IsNotNull();
        await Assert.That(finalizeCommand!.UploadSessionId).IsEqualTo(uploadSessionId);
        await Assert.That(finalizeCommand.ContentType).IsEqualTo("image/png");
        await Assert.That(finalizedBytes).IsEquivalentTo(imageBytes);
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.FeaturedImageId).IsEqualTo(storageObjectId);
        await Assert.That(sentCommand.Request.Title).IsEqualTo("Poster Event");
    }

    [Test]
    public async Task Confirm_WhenImageBytesDoNotMatchMime_RejectsBeforeMaterialization()
    {
        var imageAttachmentsJson = "[{\"mediaType\":\"image/png\",\"data\":\""
            + Convert.ToBase64String("<svg></svg>"u8.ToArray())
            + "\",\"fileName\":\"poster.png\",\"sizeBytes\":11}]";
        AiProposedAction action = CreateProposedAction(
            payloadJson: "{\"title\":\"Poster Event\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}",
            sourceImageAttachmentsJson: imageAttachmentsJson);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>())
            .Returns(action);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(
            CreateConfirmCommand(),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_ai_image_attachment");
        await _mediator.DidNotReceive().Send(
            Arg.Any<CreateStorageUploadSessionCommand>(),
            Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(
            Arg.Any<FinalizeStorageUploadSessionCommand>(),
            Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(
            Arg.Any<CreateEventCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenImageHasActiveTail_RejectsBeforeMaterialization()
    {
        byte[] imageBytes = [.. ValidPngBytes(), 0x41];
        await AssertInvalidImageRejectedAsync(
            "[{\"mediaType\":\"image/png\",\"data\":\""
            + Convert.ToBase64String(imageBytes)
            + "\",\"fileName\":\"poster.png\",\"sizeBytes\":" + imageBytes.LongLength + "}]");
    }

    [Test]
    public async Task Confirm_WhenImageMimeIsNotBrowserSafe_RejectsBeforeMaterialization()
    {
        await AssertInvalidImageRejectedAsync(
            "[{\"mediaType\":\"image/svg+xml\",\"data\":\""
            + Convert.ToBase64String("<svg></svg>"u8.ToArray())
            + "\",\"fileName\":\"poster.svg\",\"sizeBytes\":11}]");
    }

    [Test]
    public async Task Confirm_WhenImageExtensionDoesNotMatchMime_RejectsBeforeMaterialization()
    {
        byte[] imageBytes = ValidPngBytes();
        await AssertInvalidImageRejectedAsync(
            "[{\"mediaType\":\"image/png\",\"data\":\""
            + Convert.ToBase64String(imageBytes)
            + "\",\"fileName\":\"poster.jpg\",\"sizeBytes\":" + imageBytes.LongLength + "}]");
    }

    [Test]
    public async Task Confirm_WhenDeclaredImageSizeDoesNotMatchBytes_RejectsBeforeMaterialization()
    {
        byte[] imageBytes = ValidPngBytes();
        await AssertInvalidImageRejectedAsync(
            "[{\"mediaType\":\"image/png\",\"data\":\""
            + Convert.ToBase64String(imageBytes)
            + "\",\"fileName\":\"poster.png\",\"sizeBytes\":" + (imageBytes.LongLength + 1) + "}]");
    }

    [Test]
    public async Task Confirm_WhenImageDataIsMalformedBase64_RejectsBeforeMaterialization()
    {
        await AssertInvalidImageRejectedAsync(
            "[{\"mediaType\":\"image/png\",\"data\":\"not-base64!\",\"fileName\":\"poster.png\",\"sizeBytes\":8}]");
    }

    [Test]
    public async Task Confirm_WhenCreateEventDraftUsesAllowedOrganization_DispatchesOrganizationScopedCreateCommand()
    {
        var organizationId = Guid.CreateVersion7();
        AiProposedAction action = CreateProposedAction(payloadJson: "{\"title\":\"Community Dinner\",\"organizationId\":\"" + organizationId + "\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}");
        CreateEventCommand? sentCommand = null;
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(_userId, Arg.Any<string>()).Returns([organizationId]);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Executed);
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.OrganizationId).IsEqualTo(organizationId);
        await Assert.That(sentCommand.Request.Title).IsEqualTo("Community Dinner");
    }

    [Test]
    public async Task Confirm_WhenCreateEventDraftContainsReferences_PreservesReferencesForCreateCommand()
    {
        var categoryId = Guid.CreateVersion7();
        var tagId = Guid.CreateVersion7();
        AiProposedAction action = CreateProposedAction(
            payloadJson: $$"""
              {
                "title": "Community Dinner",
                "eventTypeId": 999,
                "audienceGenderId": 999,
                "audienceAgeId": 999,
                "visibilityTypeId": 999,
                "eventFormatId": 999,
                "madhabId": 999,
                "categoryIds": ["{{categoryId}}"],
                "tagIds": ["{{tagId}}"],
                "participationConfiguration": {
                  "participationHandlingModeId": 1,
                  "advanceRegistrationObligationId": 1
                }
              }
              """);
        CreateEventCommand? sentCommand = null;
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.EventTypeId).IsEqualTo(999);
        await Assert.That(sentCommand.Request.AudienceGenderId).IsEqualTo(999);
        await Assert.That(sentCommand.Request.AudienceAgeId).IsEqualTo(999);
        await Assert.That(sentCommand.Request.VisibilityTypeId).IsEqualTo(999);
        await Assert.That(sentCommand.Request.EventFormatId).IsEqualTo(999);
        await Assert.That(sentCommand.Request.MadhabId).IsEqualTo(999);
        await Assert.That(sentCommand.Request.CategoryIds).IsEquivalentTo([categoryId]);
        await Assert.That(sentCommand.Request.TagIds).IsEquivalentTo([tagId]);
    }

    [Test]
    public async Task Confirm_WhenCreateEventDraftContainsPrimaryStructuredDetails_DispatchesStructuredCreateCommand()
    {
        var speakerActorId = Guid.CreateVersion7();
        AiProposedAction action = CreateProposedAction(
            payloadJson: $$"""
              {
                "title": "Poster Event",
                "islamicAspect": {
                  "genderMode": 3
                },
                "location": {
                  "fullName": "Islamic Centre Brussels",
                  "address": "Rue Example 10",
                  "postcode": "1000",
                  "country": "Belgium",
                  "city": "Brussels"
                },
                "room": {
                  "name": "Main Hall"
                },
                "session": {
                  "title": "Opening Lecture",
                  "startTime": "2026-07-10T18:00:00Z",
                  "endTime": "2026-07-10T20:00:00Z",
                  "speakerActorIds": ["{{speakerActorId}}"]
                },
                "participationConfiguration": {
                  "participationHandlingModeId": 1,
                  "advanceRegistrationObligationId": 1
                }
              }
              """);
        CreateEventCommand? sentCommand = null;
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.IslamicAspect).IsNotNull();
        await Assert.That(sentCommand.Request.IslamicAspect!.GenderMode).IsEqualTo(GenderSegregationMode.Segregated);
        await Assert.That(sentCommand.Request.Locations.Single().TempKey).IsEqualTo("primary-location");
        await Assert.That(sentCommand.Request.Rooms.Single().LocationTempKey).IsEqualTo("primary-location");
        await Assert.That(sentCommand.Request.Sessions.Single().Title).IsEqualTo("Opening Lecture");
        await Assert.That(sentCommand.Request.Sessions.Single().RoomTempKey).IsEqualTo("primary-room");
        await Assert.That(sentCommand.Request.Sessions.Single().SpeakerActorIds).IsEquivalentTo([speakerActorId]);
        await Assert.That(sentCommand.Request.AgendaItems).IsEmpty();
    }

    [Test]
    public async Task Confirm_WhenConversationHasSelectedOrganizationActor_OverwritesAiOwnerScope()
    {
        var selectedActorId = Guid.CreateVersion7();
        var selectedOrganizationId = Guid.CreateVersion7();
        var aiOrganizationId = Guid.CreateVersion7();
        var aiGroupId = Guid.CreateVersion7();
        AiProposedAction action = CreateProposedAction(
            actorId: selectedActorId,
            payloadJson: "{\"title\":\"Community Dinner\",\"organizationId\":\"" + aiOrganizationId + "\",\"groupId\":\"" + aiGroupId + "\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}");
        CreateEventCommand? sentCommand = null;
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(_userId, Arg.Any<string>())
            .Returns([selectedOrganizationId]);
        _actorRepository.GetActorWithDetails(selectedActorId).Returns(CreateActor(
            selectedActorId,
            ActorTypeEnum.Organization,
            organizationId: selectedOrganizationId));
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.OrganizationId).IsEqualTo(selectedOrganizationId);
        await Assert.That(sentCommand.Request.GroupId).IsNull();
    }

    [Test]
    public async Task Confirm_WhenConversationHasSelectedUserActor_ClearsAiOwnerScope()
    {
        var selectedActorId = Guid.CreateVersion7();
        var aiOrganizationId = Guid.CreateVersion7();
        AiProposedAction action = CreateProposedAction(
            actorId: selectedActorId,
            payloadJson: "{\"title\":\"Community Dinner\",\"organizationId\":\"" + aiOrganizationId + "\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}");
        CreateEventCommand? sentCommand = null;
        _organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(_userId, Arg.Any<string>())
            .Returns([aiOrganizationId]);
        _actorRepository.GetActorWithDetails(selectedActorId).Returns(CreateActor(
            selectedActorId,
            ActorTypeEnum.User,
            userId: _userId));
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.OrganizationId).IsNull();
        await Assert.That(sentCommand.Request.GroupId).IsNull();
    }


    [Test]
    public async Task Confirm_WhenConversationHasSelectedGroupActor_OverwritesAiOwnerScope()
    {
        var selectedActorId = Guid.CreateVersion7();
        var selectedGroupId = Guid.CreateVersion7();
        var aiOrganizationId = Guid.CreateVersion7();
        AiProposedAction action = CreateProposedAction(
            actorId: selectedActorId,
            payloadJson: "{\"title\":\"Community Dinner\",\"organizationId\":\"" + aiOrganizationId + "\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}");
        CreateEventCommand? sentCommand = null;
        _groupMemberRepository.GetGroupIdsWhereUserHasPermission(_userId, Arg.Any<string>())
            .Returns([selectedGroupId]);
        _actorRepository.GetActorWithDetails(selectedActorId).Returns(CreateActor(
            selectedActorId,
            ActorTypeEnum.Group,
            groupId: selectedGroupId));
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);
        _mediator.Send(Arg.Do<CreateEventCommand>(command => sentCommand = command), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid> { Success = true, Id = _eventId });

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(sentCommand).IsNotNull();
        await Assert.That(sentCommand!.Request.OrganizationId).IsNull();
        await Assert.That(sentCommand.Request.GroupId).IsEqualTo(selectedGroupId);
    }

    [Test]
    public async Task Confirm_WhenActionAlreadyExecuted_ReturnsExistingResultWithoutReexecution()
    {
        AiProposedAction action = CreateProposedAction();
        action.Confirm(_userId, DateTime.UtcNow);
        action.MarkExecuted(_eventId);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_eventId);
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().CreateToolExecutionAsync(Arg.Any<AiToolExecution>(), Arg.Any<CancellationToken>());
        await _conversationRepository.DidNotReceive().UpdateProposedActionAsync(Arg.Any<AiProposedAction>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Confirm_WhenPayloadMappingFails_MarksActionFailedWithoutMediatorDispatch()
    {
        AiProposedAction action = CreateProposedAction(payloadJson: "{\"organizationId\":\"" + Guid.CreateVersion7() + "\"}");
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(CreateConfirmCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Failed);
        await Assert.That(action.FailureCode).IsNotNull();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).CreateToolExecutionAsync(
            Arg.Is<AiToolExecution>(execution =>
                execution.TenantId == _tenantId
                && execution.ProposedActionId == _actionId
                && execution.ToolName == "CreateEventDraft"
                && !execution.Succeeded
                && execution.CompletedAt != null
                && execution.FailureCode == action.FailureCode
                && execution.FailureMessage == action.FailureMessage
                && execution.FailureMessage != action.PayloadJson),
            Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).UpdateProposedActionAsync(action, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Reject_WhenActionIsProposed_MarksRejectedWithoutExecution()
    {
        AiProposedAction action = CreateProposedAction();
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateRejectHandler().Handle(CreateRejectCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_actionId);
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Rejected);
        await Assert.That(action.RejectedBy).IsEqualTo(_userId);
        await _conversationRepository.DidNotReceive().CreateToolExecutionAsync(Arg.Any<AiToolExecution>(), Arg.Any<CancellationToken>());
        await _conversationRepository.Received(1).UpdateProposedActionAsync(action, Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Reject_WhenActionAlreadyRejected_ReturnsSuccessWithoutUpdate()
    {
        AiProposedAction action = CreateProposedAction();
        action.Reject(_userId, DateTime.UtcNow);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateRejectHandler().Handle(CreateRejectCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_actionId);
        await _conversationRepository.DidNotReceive().UpdateProposedActionAsync(Arg.Any<AiProposedAction>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Reject_WhenActionAlreadyExecuted_FailsWithoutSideEffects()
    {
        AiProposedAction action = CreateProposedAction();
        action.Confirm(_userId, DateTime.UtcNow);
        action.MarkExecuted(_eventId);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>()).Returns(action);

        BaseCommandResponse<Guid> result = await CreateRejectHandler().Handle(CreateRejectCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_proposed_action_state");
        await Assert.That(action.Status).IsEqualTo(AiProposedActionStatus.Executed);
        await _conversationRepository.DidNotReceive().UpdateProposedActionAsync(Arg.Any<AiProposedAction>(), Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateEventCommand>(), Arg.Any<CancellationToken>());
    }

    private ConfirmAiProposedActionCommandHandler CreateConfirmHandler()
        => new(
            _conversationRepository,
            _organizationMemberRepository,
            _groupMemberRepository,
            _actorRepository,
            _tenantContext,
            _currentUserService,
            _mediator);

    private static byte[] ValidPngBytes() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    private RejectAiProposedActionCommandHandler CreateRejectHandler()
        => new(_conversationRepository, _tenantContext, _currentUserService);

    private ConfirmAiProposedActionCommand CreateConfirmCommand()
        => new() { ProposedActionId = _actionId };

    private RejectAiProposedActionCommand CreateRejectCommand()
        => new() { ProposedActionId = _actionId };

    private AiProposedAction CreateProposedAction(
        Guid? tenantId = null,
        Guid? userId = null,
        Guid? actorId = null,
        string payloadJson = "{\"title\":\"Draft\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}",
        string? sourceImageAttachmentsJson = null)
    {
        Guid actionTenantId = tenantId ?? _tenantId;
        var conversation = new AiConversation
        {
            Id = _conversationId,
            TenantId = actionTenantId,
            UserId = userId ?? _userId,
            ActorId = actorId,
            Status = AiConversationStatus.Active
        };

        AiMessage? assistantMessage = null;
        if (!string.IsNullOrWhiteSpace(sourceImageAttachmentsJson))
        {
            var utcNow = DateTime.UtcNow;
            conversation.AddMessage(
                AiMessageRole.User,
                "Create an event draft from this poster.",
                userId ?? _userId,
                utcNow,
                sourceImageAttachmentsJson);
            assistantMessage = conversation.AddMessage(
                AiMessageRole.Assistant,
                "I prepared a proposed action for your review.",
                null,
                utcNow.AddMilliseconds(1));
        }

        return new AiProposedAction
        {
            Id = _actionId,
            TenantId = actionTenantId,
            ConversationId = _conversationId,
            Conversation = conversation,
            MessageId = assistantMessage?.Id,
            Message = assistantMessage,
            Kind = AiProposedActionKind.CreateEventDraft,
            Status = AiProposedActionStatus.Proposed,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
    }

    private async Task AssertInvalidImageRejectedAsync(string imageAttachmentsJson)
    {
        AiProposedAction action = CreateProposedAction(
            payloadJson: "{\"title\":\"Poster Event\",\"participationConfiguration\":{\"participationHandlingModeId\":1,\"advanceRegistrationObligationId\":1}}",
            sourceImageAttachmentsJson: imageAttachmentsJson);
        _conversationRepository.GetProposedActionForUpdateAsync(_actionId, Arg.Any<CancellationToken>())
            .Returns(action);

        BaseCommandResponse<Guid> result = await CreateConfirmHandler().Handle(
            CreateConfirmCommand(),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_ai_image_attachment");
        await _mediator.DidNotReceive().Send(
            Arg.Any<CreateStorageUploadSessionCommand>(),
            Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(
            Arg.Any<FinalizeStorageUploadSessionCommand>(),
            Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(
            Arg.Any<CreateEventCommand>(),
            Arg.Any<CancellationToken>());
    }

    private Actor CreateActor(
        Guid actorId,
        ActorTypeEnum actorType,
        Guid? userId = null,
        Guid? organizationId = null,
        Guid? groupId = null)
        => new()
        {
            Id = actorId,
            ActorTypeId = (int)actorType,
            ActorType = new ActorType { Id = (int)actorType, FullName = actorType.ToString(), MasterCode = actorType.ToString() },
            UserId = userId,
            OrganizationId = organizationId,
            GroupId = groupId,
            Pii = new ActorPii { ActorId = actorId, DisplayName = actorType.ToString() }
        };

    private StorageUploadSessionDto CreateUploadSessionDto(Guid uploadSessionId, Guid? storageObjectId = null)
        => new()
        {
            Id = uploadSessionId,
            TenantId = _tenantId,
            Provider = StorageProviders.Local,
            ExpectedSizeBytes = 4,
            ReservedBytes = 4,
            ContentType = "image/png",
            SafeDisplayName = "poster.png",
            Purpose = StorageObjectPurposes.EventImage,
            Visibility = StorageObjectVisibilities.PublicImage,
            Status = storageObjectId.HasValue
                ? StorageUploadSessionStates.Finalized
                : StorageUploadSessionStates.Reserved,
            StorageObjectId = storageObjectId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            MaxUploadBytes = AiMessageImageMaxBytes,
            TenantQuotaBytes = AiMessageImageMaxBytes,
            UsedBytes = storageObjectId.HasValue ? 4 : 0,
            TotalReservedBytes = storageObjectId.HasValue ? 0 : 4
        };

    private const long AiMessageImageMaxBytes = 5 * 1024 * 1024;
}
