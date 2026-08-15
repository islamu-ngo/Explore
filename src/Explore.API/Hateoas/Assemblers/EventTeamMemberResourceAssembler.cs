// ABOUTME: HAL assembler for event-team member collection resources.
// ABOUTME: Applies event-scoped assignment and revocation policies through the shared batch pipeline.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventRoleAssignment;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Assemblers;

public sealed class EventTeamMemberResourceAssembler(
    IHateoasLinkGenerator linkGenerator,
    ILinkPolicy<EventTeamMemberDto> detailLinkPolicy,
    ICollectionLinkPolicy<EventTeamMemberDto> collectionLinkPolicy)
    : ResourceAssemblerBase<EventTeamMemberDto, EventTeamMemberDto>(
        linkGenerator,
        detailLinkPolicy,
        collectionLinkPolicy);
