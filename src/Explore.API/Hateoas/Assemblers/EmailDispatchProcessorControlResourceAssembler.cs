// ABOUTME: HAL assembler for sanitized instance-wide SMTP processor control state.
// ABOUTME: Runs control affordances through the shared authorization capability pipeline.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EmailDispatch;

namespace Explore.API.Hateoas.Assemblers;

public sealed class EmailDispatchProcessorControlResourceAssembler
    : ResourceAssemblerBase<EmailDispatchProcessorControlDto, EmailDispatchProcessorControlDto>
{
    public EmailDispatchProcessorControlResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EmailDispatchProcessorControlDto> detailLinkPolicy,
        ICollectionLinkPolicy<EmailDispatchProcessorControlDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
