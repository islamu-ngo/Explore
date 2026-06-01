// ABOUTME: HATEOAS link policies for authenticated AI assistant conversation resources.
// ABOUTME: Emits conversation navigation and send affordances through the standard fail-closed HAL pipeline.

namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Ai;
using Explore.Application.Hateoas;

public sealed class AiConversationDetailLinkPolicy : ILinkPolicy<AiConversationDto>
{
    public IEnumerable<LinkDefinition> GetLinks(AiConversationDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetAiConversation,
            new { conversationId = dto.Id },
            "GET",
            "AI conversation",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.AiConversations.View, ResourceDescriptors.AiConversation, dto);

        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetAiConversations,
            null,
            "GET",
            "AI conversations",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.AiConversations.View, ResourceDescriptors.AiConversation, dto);

        if (IsActive(dto.Status))
        {
            yield return new LinkDefinition(
                LinkRelations.SendMessage,
                RouteNames.SendAiMessage,
                new { conversationId = dto.Id },
                "POST",
                "Send message",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.AiConversations.SendMessage, ResourceDescriptors.AiConversation, dto);
        }
    }

    private static bool IsActive(string status) =>
        string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
}

public sealed class AiConversationCollectionLinkPolicy : ICollectionLinkPolicy<AiConversationSummaryDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(AiConversationSummaryDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetAiConversation,
            new { conversationId = dto.Id },
            "GET",
            "AI conversation",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.AiConversations.View, ResourceDescriptors.AiConversationSummary, dto);

        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetAiConversations,
            null,
            "GET",
            "AI conversations",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.AiConversations.View, ResourceDescriptors.AiConversationSummary, dto);

        if (IsActive(dto.Status))
        {
            yield return new LinkDefinition(
                LinkRelations.SendMessage,
                RouteNames.SendAiMessage,
                new { conversationId = dto.Id },
                "POST",
                "Send message",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.AiConversations.SendMessage, ResourceDescriptors.AiConversationSummary, dto);
        }
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Create,
            RouteNames.CreateAiConversation,
            null,
            "POST",
            "Create AI conversation",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.AiConversations.Create, ResourceKinds.AiConversation);
    }

    private static bool IsActive(string status) =>
        string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
}
