// ABOUTME: HAL resource assembler for the ATProto instance-governance setting group.
// ABOUTME: Applies the shared authorization-aware capability planning pipeline to its allowlisted controls.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Settings;

public sealed class AtprotoInstanceSettingGroupResourceAssembler
    : ResourceAssemblerBase<SettingGroupResponseDto, SettingGroupResponseDto>
{
    public AtprotoInstanceSettingGroupResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<SettingGroupResponseDto> detailLinkPolicy,
        ICollectionLinkPolicy<SettingGroupResponseDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
