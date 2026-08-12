// ABOUTME: Assembles registration-form template catalog DTOs into HAL resources.
// ABOUTME: Uses the standard resource assembler pipeline with separate detail and collection policies.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationForms;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Assemblers;

public sealed class RegistrationFormTemplateResourceAssembler(
    IHateoasLinkGenerator linkGenerator,
    ILinkPolicy<RegistrationFormTemplateDto> detailPolicy,
    ICollectionLinkPolicy<RegistrationFormTemplateDto> collectionPolicy)
    : ResourceAssemblerBase<RegistrationFormTemplateDto, RegistrationFormTemplateDto>(
        linkGenerator,
        detailPolicy,
        collectionPolicy);
