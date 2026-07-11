// ABOUTME: Resource assembler for the authenticated user's notification preference matrix.
// ABOUTME: Wraps the matrix DTO in HAL links so clients gate preference actions from _links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Notification;

public sealed class NotificationPreferenceMatrixResourceAssembler : ResourceAssemblerBase<NotificationPreferenceMatrixDto>
{
    public NotificationPreferenceMatrixResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<NotificationPreferenceMatrixDto> detailLinkPolicy,
        ICollectionLinkPolicy<NotificationPreferenceMatrixDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    protected override Dictionary<string, object>? GetEmbeddedResources(
        NotificationPreferenceMatrixDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        return null;
    }
}
