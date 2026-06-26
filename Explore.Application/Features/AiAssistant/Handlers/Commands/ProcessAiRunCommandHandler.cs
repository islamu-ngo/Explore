// ABOUTME: Processes queued AI assistant runs outside the HTTP request path.
// ABOUTME: Persists assistant text and proposal-only tool output while respecting Ask vs Build mode.

using System.Globalization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.AiAssistant.Prompting;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Models;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Handlers.Commands;

public sealed class ProcessAiRunCommandHandler : IRequestHandler<ProcessAiRunCommand>
{
    private readonly IAiConversationRepository _conversationRepository;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IMediator _mediator;
    private readonly AiPromptContextBuilder _promptContextBuilder;
    private readonly AiProviderResponseResolver _providerResponseResolver;

    public ProcessAiRunCommandHandler(
        IAiConversationRepository conversationRepository,
        IHierarchicalSettingsResolver settingsResolver,
        IAiChatProvider chatProvider,
        IMediator mediator)
    {
        _conversationRepository = conversationRepository;
        _settingsResolver = settingsResolver;
        _mediator = mediator;
        var toolRegistry = AiToolContractRegistry.CreateDefault();
        _promptContextBuilder = new AiPromptContextBuilder(new AiSystemPromptFactory(toolRegistry));
        _providerResponseResolver = new AiProviderResponseResolver(
            chatProvider,
            new AiStructuredActionParser(toolRegistry));
    }

    public async Task Handle(ProcessAiRunCommand request, CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty || request.ConversationId == Guid.Empty || request.RunId == Guid.Empty)
        {
            return;
        }

        var settings = await _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
            new SettingContext(TenantId: request.TenantId),
            cancellationToken);
        var allowToolProposals = AiAssistantInteractionModes.AllowsToolProposals(request.Mode);

        var conversation = await _conversationRepository.GetByIdForUpdateAsync(
            request.ConversationId,
            cancellationToken);

        if (!TryGetQueuedRun(conversation, request.TenantId, request.RunId, out var run))
        {
            return;
        }

        var disabledReason = AiAssistantAvailability.ResolveDisabledReason(settings);
        if (disabledReason is not null)
        {
            await FailAndActivateAsync(
                conversation!,
                run!,
                disabledReason,
                $"AI assistant is unavailable: {disabledReason}.",
                cancellationToken);
            return;
        }

        if (!AiAssistantAvailability.IsModelAllowed(settings, run!.ModelId))
        {
            await FailAndActivateAsync(
                conversation!,
                run,
                AiAssistantAvailability.ModelNotAllowedFailureCode,
                AiAssistantAvailability.ModelNotAllowedFailureMessage,
                cancellationToken);
            return;
        }

        var startedAt = DateTime.UtcNow;
        run.Start(startedAt);
        await _conversationRepository.Update(conversation!);

        var selectedReferences = await BuildSelectedReferenceContextAsync(
            conversation!,
            settings,
            cancellationToken);
        var providerPayload = _promptContextBuilder.Build(
            conversation!,
            settings,
            run.ModelId,
            allowToolProposals,
            selectedReferences);

        AiProviderResponseResolution providerResolution;
        try
        {
            providerResolution = await _providerResponseResolver.ResolveAsync(
                providerPayload,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await FailLatestRunAndActivateAsync(
                request,
                "provider_cancelled",
                "AI provider run was cancelled before completion.",
                CancellationToken.None);
            return;
        }

        var latestConversation = await _conversationRepository.GetByIdForUpdateAsync(
            request.ConversationId,
            CancellationToken.None);

        if (!TryGetInProgressRun(latestConversation, request.TenantId, request.RunId, out var latestRun))
        {
            return;
        }

        if (!providerResolution.Succeeded || providerResolution.Response is null || providerResolution.ParseResult is null)
        {
            var errorCode = providerResolution.FailureCode ?? "provider_failure";
            var errorMessage = providerResolution.FailureMessage ?? "AI provider failed.";
            await FailAndActivateAsync(
                latestConversation!,
                latestRun!,
                errorCode,
                errorMessage,
                CancellationToken.None);
            return;
        }

        var assistantText = BuildAssistantMessage(providerResolution.Response);
        var utcNow = DateTime.UtcNow;
        var assistantMessage = latestConversation!.AddMessage(AiMessageRole.Assistant, assistantText, null, utcNow);
        IReadOnlyList<AiParsedProposedAction> proposedActions = allowToolProposals
            ? providerResolution.ParseResult.Actions
            : Array.Empty<AiParsedProposedAction>();

        foreach (var proposedAction in proposedActions)
        {
            latestConversation.ProposeAction(
                proposedAction.Kind,
                proposedAction.PayloadJson,
                assistantMessage.Id,
                latestConversation.UserId,
                DateTime.UtcNow);
        }

        latestConversation.CompleteRun(latestRun!, DateTime.UtcNow);
        await _conversationRepository.Update(latestConversation);
    }

    private async Task FailLatestRunAndActivateAsync(
        ProcessAiRunCommand request,
        string failureCode,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdForUpdateAsync(
            request.ConversationId,
            cancellationToken);

        if (!TryGetActiveRun(conversation, request.TenantId, request.RunId, out var run))
        {
            return;
        }

        await FailAndActivateAsync(conversation!, run!, failureCode, failureMessage, cancellationToken);
    }

    private async Task FailAndActivateAsync(
        AiConversation conversation,
        AiRun run,
        string failureCode,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        var failedAt = DateTime.UtcNow;
        conversation.FailRun(run, failureCode, failureMessage, failedAt);
        conversation.Activate(failedAt);
        await _conversationRepository.Update(conversation);
    }

    private static bool TryGetQueuedRun(
        AiConversation? conversation,
        Guid tenantId,
        Guid runId,
        out AiRun? run)
    {
        run = null;
        if (conversation is null || conversation.TenantId != tenantId)
        {
            return false;
        }

        run = conversation.Runs.FirstOrDefault(candidate => candidate.Id == runId);
        return run?.Status == AiRunStatus.Queued;
    }

    private static bool TryGetInProgressRun(
        AiConversation? conversation,
        Guid tenantId,
        Guid runId,
        out AiRun? run)
    {
        run = null;
        if (conversation is null || conversation.TenantId != tenantId)
        {
            return false;
        }

        run = conversation.Runs.FirstOrDefault(candidate => candidate.Id == runId);
        return run?.Status == AiRunStatus.InProgress;
    }

    private static bool TryGetActiveRun(
        AiConversation? conversation,
        Guid tenantId,
        Guid runId,
        out AiRun? run)
    {
        run = null;
        if (conversation is null || conversation.TenantId != tenantId)
        {
            return false;
        }

        run = conversation.Runs.FirstOrDefault(candidate => candidate.Id == runId);
        return run?.Status is AiRunStatus.Queued or AiRunStatus.InProgress;
    }

    private static string BuildAssistantMessage(AiChatResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.AssistantMessage))
        {
            return response.AssistantMessage;
        }

        return response.ProposedActions.Count > 0
            ? "I prepared a proposed action for your review."
            : "The AI provider returned an empty response.";
    }

    private async Task<IReadOnlyList<AiSelectedReferenceDto>> BuildSelectedReferenceContextAsync(
        AiConversation conversation,
        AiAssistantSettingGroup settings,
        CancellationToken cancellationToken)
    {
        var selectedReferences = conversation.References
            .OrderBy(reference => reference.CreatedAt)
            .ThenBy(reference => reference.Id)
            .Take(settings.SelectedReferenceLimit)
            .Select(reference => new AiSelectedReferenceDto(
                reference.Kind.ToString(),
                reference.ReferenceId,
                reference.DisplayName,
                reference.Summary))
            .ToList();

        if (selectedReferences.Count == 0)
        {
            return [];
        }

        var enrichedReferences = new List<AiSelectedReferenceDto>(selectedReferences.Count);
        foreach (AiSelectedReferenceDto reference in selectedReferences)
        {
            if (IsEventReference(reference))
            {
                var eventDetails = await _mediator.Send(
                    new GetEventDetailsRequest { Id = reference.ReferenceId },
                    cancellationToken);
                if (eventDetails is not null)
                {
                    enrichedReferences.Add(new AiSelectedReferenceDto(
                        AiReferenceKind.Event.ToString(),
                        eventDetails.Id,
                        eventDetails.Title,
                        BuildEventReferenceSummary(eventDetails)));
                    continue;
                }
            }

            enrichedReferences.Add(reference);
        }

        return enrichedReferences;
    }

    private static bool IsEventReference(AiSelectedReferenceDto reference)
        => string.Equals(reference.Kind, AiReferenceKind.Event.ToString(), StringComparison.OrdinalIgnoreCase);

    private static string BuildEventReferenceSummary(EventDto eventDetails)
    {
        var parts = new List<string>
        {
            $"status: {eventDetails.EventStatusFullName}",
            $"format: {eventDetails.EventFormatFullName}",
            $"visibility: {eventDetails.VisibilityTypeFullName}",
            $"host: {eventDetails.ActorDisplayName}"
        };

        if (!string.IsNullOrWhiteSpace(eventDetails.Subtitle))
        {
            parts.Add($"subtitle: {eventDetails.Subtitle.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(eventDetails.Description))
        {
            parts.Add($"description: {eventDetails.Description.Trim()}");
        }

        if (eventDetails.FirstSessionDate.HasValue || eventDetails.LastSessionDate.HasValue)
        {
            var firstSessionDate = eventDetails.FirstSessionDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "unknown";
            var lastSessionDate = eventDetails.LastSessionDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "unknown";
            parts.Add($"dates: {firstSessionDate} to {lastSessionDate}");
        }

        if (!string.IsNullOrWhiteSpace(eventDetails.Timezone))
        {
            parts.Add($"timezone: {eventDetails.Timezone.Trim()}");
        }

        if (eventDetails.SessionCount.HasValue)
        {
            parts.Add($"sessions: {eventDetails.SessionCount.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        return string.Join("; ", parts);
    }
}
