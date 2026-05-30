// ABOUTME: HAL resource assembler for AI assistant conversation resources.
// ABOUTME: Converts private conversation DTOs to HAL resources using AI link policies.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Ai;

public sealed class AiConversationResourceAssembler : ResourceAssemblerBase<AiConversationDto, AiConversationSummaryDto>
{
    public AiConversationResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<AiConversationDto> detailLinkPolicy,
        ICollectionLinkPolicy<AiConversationSummaryDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
