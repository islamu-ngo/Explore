// ABOUTME: Query handler that securely prepares a public event Open Graph image render request.
// ABOUTME: Uses public event eligibility, effective tenant branding, and trusted public-image storage only.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.Events.OpenGraph;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Models.Storage;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Queries;

public sealed class GetPublicEventOpenGraphImageRequestHandler(
    IMediator mediator,
    ITenantPolicySettingService tenantPolicySettingService,
    IStorageObjectContentReader contentReader,
    IEventOpenGraphImageRenderer renderer)
    : IRequestHandler<GetPublicEventOpenGraphImageRequest, EventOpenGraphImageRenderResult?>
{
    public async Task<EventOpenGraphImageRenderResult?> Handle(
        GetPublicEventOpenGraphImageRequest request,
        CancellationToken cancellationToken)
    {
        EventDto? eventDto = await mediator.Send(
            new GetPublicEventDetailsRequest { SlugCode = request.SlugCode },
            cancellationToken);
        if (eventDto is null ||
            eventDto.EventStatusId != (int)EventStatusEnum.Published ||
            eventDto.VisibilityTypeId != (int)VisibilityTypeEnum.Public)
        {
            return null;
        }

        TenantPolicySettingsDto settings = await tenantPolicySettingService
            .ReadEffectiveTenantSettingsAsync(eventDto.TenantId);
        StorageObjectContentResult? featuredImage = eventDto.FeaturedImageId == Guid.Empty
            ? null
            : await contentReader.OpenAsync(eventDto.FeaturedImageId, publicImagesOnly: true, cancellationToken);
        using Stream? featuredImageStream = featuredImage?.Content;

        return await renderer.RenderAsync(
            new EventOpenGraphImageRenderRequest(
                eventDto.Title,
                eventDto.FirstSessionDate,
                eventDto.LastSessionDate,
                settings.BrandDisplayName,
                featuredImageStream,
                featuredImage?.ContentType),
            cancellationToken);
    }
}
