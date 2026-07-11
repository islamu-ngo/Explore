// ABOUTME: HAL resource assembler for EmailDispatch operator status rows.
// ABOUTME: Lets status collections expose server-authored replay and park affordances.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EmailDispatch;

public sealed class EmailDispatchStatusResourceAssembler : ResourceAssemblerBase<EmailDispatchStatusDto, EmailDispatchStatusDto>
{
    public EmailDispatchStatusResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EmailDispatchStatusDto> detailLinkPolicy,
        ICollectionLinkPolicy<EmailDispatchStatusDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
