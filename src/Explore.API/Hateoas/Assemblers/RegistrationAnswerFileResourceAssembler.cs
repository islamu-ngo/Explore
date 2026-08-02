// ABOUTME: Assembles registration answer file DTOs through the shared HAL pipeline.
// ABOUTME: Keeps state-dependent release affordances centralized in the link policy.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Registration;

namespace Explore.API.Hateoas.Assemblers;

public sealed class RegistrationAnswerFileResourceAssembler
    : ResourceAssemblerBase<RegistrationAnswerFileDto, RegistrationAnswerFileDto>
{
    public RegistrationAnswerFileResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<RegistrationAnswerFileDto> detailLinkPolicy,
        ICollectionLinkPolicy<RegistrationAnswerFileDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
