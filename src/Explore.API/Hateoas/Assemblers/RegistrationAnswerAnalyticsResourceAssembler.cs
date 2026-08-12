// ABOUTME: Assembles governed registration-answer analytics DTOs through the shared HAL pipeline.
// ABOUTME: Keeps aggregate analytics affordances centralized in registration analytics link policies.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationAnalytics;

namespace Explore.API.Hateoas.Assemblers;

public sealed class RegistrationAnswerAnalyticsResourceAssembler
    : ResourceAssemblerBase<RegistrationAnswerAnalyticsDto, RegistrationAnswerAnalyticsDto>
{
    public RegistrationAnswerAnalyticsResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<RegistrationAnswerAnalyticsDto> detailLinkPolicy,
        ICollectionLinkPolicy<RegistrationAnswerAnalyticsDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
