namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.AtprotoRecord;

/// <summary>
/// Resource assembler for AtprotoRecord entities.
/// Converts AtprotoRecordDto and AtprotoRecordListDto to HAL resources with appropriate links.
/// Part of ATProto federation support.
/// </summary>
public sealed class AtprotoRecordResourceAssembler : ResourceAssemblerBase<AtprotoRecordDto, AtprotoRecordListDto>
{
    public AtprotoRecordResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<AtprotoRecordDto> detailLinkPolicy,
        ICollectionLinkPolicy<AtprotoRecordListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for ATProto record details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        AtprotoRecordDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // ATProto records link to IndexedDid via _links
        return null;
    }
}
