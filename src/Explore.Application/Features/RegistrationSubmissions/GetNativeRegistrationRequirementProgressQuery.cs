// ABOUTME: Resolves attendee-safe launch descriptors for pending native order requirements.
// ABOUTME: Fails closed on ambiguous published forms and derives every subject from server-owned order state.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.RegistrationSubmissions;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.RegistrationSubmissions.Commands;

public sealed record GetNativeRegistrationRequirementProgressQuery(
    Guid TenantId,
    Guid EventId,
    Guid OrderId) : IRequest<NativeRegistrationRequirementProgressCollectionDto?>;

public sealed class GetNativeRegistrationRequirementProgressQueryHandler(
    IRegistrationInventoryRepository inventory,
    IRegistrationFormAuthoringRepository authoring,
    IRegistrationParticipantRepository participantRepository,
    IRegistrationFinalizationRepository finalization)
    : IRequestHandler<GetNativeRegistrationRequirementProgressQuery, NativeRegistrationRequirementProgressCollectionDto?>
{
    public async Task<NativeRegistrationRequirementProgressCollectionDto?> Handle(
        GetNativeRegistrationRequirementProgressQuery request,
        CancellationToken cancellationToken)
    {
        RegistrationOrder? order = await inventory.GetOrderWithLinesAsync(
            request.OrderId, request.TenantId, cancellationToken);
        NativeRegistrationLaunchAuthority? authority = await NativeRegistrationLaunchDescriptorResolver.ResolveAsync(
            authoring, order, request.EventId, cancellationToken);
        if (authority is null)
        {
            return null;
        }

        if (authority.Version is null)
        {
            return new(order!.Id, []);
        }

        RegistrationWorkflow workflow = authority.Workflow;

        IReadOnlyList<RegistrationParticipant> participants = await participantRepository.GetParticipantsByOrderAsync(
            order.Id, request.TenantId, cancellationToken);
        IReadOnlyList<RegistrationTicketAssignment> assignments = await participantRepository
            .GetAssignmentsWithParticipantsByOrderAsync(order.Id, request.TenantId, cancellationToken);
        var descriptors = new List<NativeRegistrationLaunchDescriptorDto>();
        foreach (RegistrationRequirement requirement in workflow.Requirements
                     .Where(value => !value.IsDeleted).OrderBy(value => value.Ordinal))
        {
            IReadOnlyList<RegistrationRequirementFulfillment> fulfillments = await finalization.GetFulfillmentsAsync(
                request.TenantId, order.Id, requirement.Id, cancellationToken);
            IReadOnlyList<NativeRegistrationAnswerSubjectDto> subjects = NativeRegistrationAttemptContractBuilder.Subjects(
                order, requirement, participants, assignments, fulfillments);
            NativeRegistrationRequirementProgressDto progress = NativeRegistrationAttemptContractBuilder.Progress(subjects);
            if (subjects.Count == 0 || progress.IsComplete)
            {
                continue;
            }

            foreach (RegistrationChannel channel in requirement.Channels
                         .Where(value => !value.IsDeleted && value.IsNative).OrderBy(value => value.Ordinal))
            {
                descriptors.Add(new(
                    requirement.Id,
                    channel.Id,
                    authority.Version.RegistrationFormId,
                    authority.Version.Id,
                    requirement.CanSkip,
                    subjects,
                    progress));
            }
        }

        return new(order.Id, descriptors);
    }
}

internal sealed record NativeRegistrationLaunchAuthority(
    RegistrationWorkflow Workflow,
    RegistrationFormVersion? Version);

internal static class NativeRegistrationLaunchDescriptorResolver
{
    public static async Task<NativeRegistrationLaunchAuthority?> ResolveAsync(
        IRegistrationFormAuthoringRepository authoring,
        RegistrationOrder? order,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        RegistrationWorkflow? workflow = await authoring.GetWorkflowAsync(
            eventId, "registration", cancellationToken);
        if (order is null || workflow is null || order.EventId != eventId ||
            order.RegistrationOrderStatusId != (int)RegistrationOrderStatusEnum.AwaitingRequirements ||
            order.RegistrationWorkflowVersionId != workflow.Id)
        {
            return null;
        }

        IReadOnlyList<RegistrationFormVersion> published = await authoring.GetPublishedVersionsAsync(
            eventId, 2, cancellationToken);
        return new(workflow, published.Count == 1 ? published[0] : null);
    }
}
