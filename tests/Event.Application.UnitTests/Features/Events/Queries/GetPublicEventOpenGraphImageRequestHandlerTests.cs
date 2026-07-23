// ABOUTME: Unit tests for the secure public event Open Graph image query.
// ABOUTME: Proves public eligibility, tenant-effective branding, trusted stream use, fallback, disposal, and cancellation.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.OpenGraph;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Models.Storage;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Queries;

public sealed class GetPublicEventOpenGraphImageRequestHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ITenantPolicySettingService _tenantPolicySettings = Substitute.For<ITenantPolicySettingService>();
    private readonly IStorageObjectContentReader _contentReader = Substitute.For<IStorageObjectContentReader>();
    private readonly IEventOpenGraphImageRenderer _renderer = Substitute.For<IEventOpenGraphImageRenderer>();
    private readonly GetPublicEventOpenGraphImageRequestHandler _handler;

    public GetPublicEventOpenGraphImageRequestHandlerTests()
    {
        _tenantPolicySettings.ReadEffectiveTenantSettingsAsync(Arg.Any<Guid>())
            .Returns(new TenantPolicySettingsDto { BrandDisplayName = "Default brand" });
        _renderer.RenderAsync(Arg.Any<EventOpenGraphImageRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EventOpenGraphImageRenderResult([1, 2, 3], "etag"));
        _handler = new GetPublicEventOpenGraphImageRequestHandler(
            _mediator,
            _tenantPolicySettings,
            _contentReader,
            _renderer);
    }

    [Test]
    public async Task Handle_ForPublishedPublicEvent_UsesEffectiveTenantBrandAndRendersPng()
    {
        EventDto eventDto = CreatePublicEvent();
        ConfigurePublicEvent(eventDto);
        _tenantPolicySettings.ReadEffectiveTenantSettingsAsync(eventDto.TenantId)
            .Returns(new TenantPolicySettingsDto
            {
                BrandDisplayName = "Instance locked brand",
                CanOverrideBrandDisplayName = false
            });

        EventOpenGraphImageRenderResult? result = await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.PngBytes.Length).IsEqualTo(3);
        await Assert.That(result.PngBytes[0]).IsEqualTo((byte)1);
        await Assert.That(result.PngBytes[1]).IsEqualTo((byte)2);
        await Assert.That(result.PngBytes[2]).IsEqualTo((byte)3);
        await Assert.That(result.ETag).IsEqualTo("etag");
        await _tenantPolicySettings.Received(1).ReadEffectiveTenantSettingsAsync(eventDto.TenantId);
        await _renderer.Received(1).RenderAsync(
            Arg.Is<EventOpenGraphImageRenderRequest>(renderRequest =>
                renderRequest.Title == eventDto.Title &&
                renderRequest.FirstSessionDate == eventDto.FirstSessionDate &&
                renderRequest.LastSessionDate == eventDto.LastSessionDate &&
                renderRequest.BrandDisplayName == "Instance locked brand"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ForNonPublicEvent_ReturnsNullWithoutReadingStorageOrRendering()
    {
        EventDto eventDto = CreatePublicEvent();
        eventDto.VisibilityTypeId = (int)VisibilityTypeEnum.Private;
        ConfigurePublicEvent(eventDto);

        EventOpenGraphImageRenderResult? result = await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _tenantPolicySettings.DidNotReceiveWithAnyArgs().ReadEffectiveTenantSettingsAsync(default);
        await _contentReader.DidNotReceiveWithAnyArgs().OpenAsync(default, default, default);
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
    }

    [Test]
    public async Task Handle_UsesOnlyFeaturedImageIdAsPublicImageSource()
    {
        EventDto eventDto = CreatePublicEvent();
        eventDto.FeaturedImageUri = "https://untrusted.example/private-image.png";
        var stream = new TrackingStream();
        ConfigurePublicEvent(eventDto);
        _contentReader.OpenAsync(eventDto.FeaturedImageId, publicImagesOnly: true, Arg.Any<CancellationToken>())
            .Returns(new StorageObjectContentResult(stream, "image/png", 3, null, null));

        await _handler.Handle(Request(), CancellationToken.None);

        await _contentReader.Received(1).OpenAsync(
            eventDto.FeaturedImageId,
            publicImagesOnly: true,
            Arg.Any<CancellationToken>());
        await _renderer.Received(1).RenderAsync(
            Arg.Is<EventOpenGraphImageRenderRequest>(renderRequest =>
                ReferenceEquals(renderRequest.FeaturedImage, stream) &&
                renderRequest.FeaturedImageContentType == "image/png"),
            Arg.Any<CancellationToken>());
        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Handle_WhenPublicImageIsUnavailable_RendersFallbackWithoutImage()
    {
        EventDto eventDto = CreatePublicEvent();
        ConfigurePublicEvent(eventDto);
        _contentReader.OpenAsync(eventDto.FeaturedImageId, publicImagesOnly: true, Arg.Any<CancellationToken>())
            .Returns((StorageObjectContentResult?)null);

        EventOpenGraphImageRenderResult? result = await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await _renderer.Received(1).RenderAsync(
            Arg.Is<EventOpenGraphImageRenderRequest>(renderRequest =>
                renderRequest.FeaturedImage == null &&
                renderRequest.FeaturedImageContentType == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRendererFails_DisposesPublicImageStream()
    {
        EventDto eventDto = CreatePublicEvent();
        var stream = new TrackingStream();
        ConfigurePublicEvent(eventDto);
        _contentReader.OpenAsync(eventDto.FeaturedImageId, publicImagesOnly: true, Arg.Any<CancellationToken>())
            .Returns(new StorageObjectContentResult(stream, "image/png", 3, null, null));
        _renderer.RenderAsync(Arg.Any<EventOpenGraphImageRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<EventOpenGraphImageRenderResult>(new InvalidOperationException("renderer failed")));

        async Task Act() => await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(Act).Throws<InvalidOperationException>();
        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Handle_PassesCancellationTokenThroughMediatRStorageAndRenderer()
    {
        EventDto eventDto = CreatePublicEvent();
        using var source = new CancellationTokenSource();
        CancellationToken cancellationToken = source.Token;
        ConfigurePublicEvent(eventDto);
        _contentReader.OpenAsync(eventDto.FeaturedImageId, publicImagesOnly: true, cancellationToken)
            .Returns((StorageObjectContentResult?)null);

        await _handler.Handle(Request(), cancellationToken);

        await _mediator.Received(1).Send(
            Arg.Is<GetPublicEventDetailsRequest>(request => request.SlugCode == "secure-event-code"),
            cancellationToken);
        await _contentReader.Received(1).OpenAsync(eventDto.FeaturedImageId, publicImagesOnly: true, cancellationToken);
        await _renderer.Received(1).RenderAsync(Arg.Any<EventOpenGraphImageRenderRequest>(), cancellationToken);
    }

    private void ConfigurePublicEvent(EventDto eventDto)
    {
        _mediator.Send(
                Arg.Is<GetPublicEventDetailsRequest>(request => request.SlugCode == "secure-event-code"),
                Arg.Any<CancellationToken>())
            .Returns(eventDto);
    }

    private static GetPublicEventOpenGraphImageRequest Request() => new()
    {
        SlugCode = "secure-event-code"
    };

    private static EventDto CreatePublicEvent() => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        FeaturedImageId = Guid.CreateVersion7(),
        Title = "Secure event",
        ActorDisplayName = "Organizer",
        ActorTypeFullName = "Organization",
        EventStatusId = (int)EventStatusEnum.Published,
        EventStatusFullName = "Published",
        EventStatusMasterCode = "published",
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        VisibilityTypeFullName = "Public",
        VisibilityTypeMasterCode = "public",
        EventFormatFullName = "In person",
        EventFormatMasterCode = "in-person",
        FirstSessionDate = new DateOnly(2026, 8, 1),
        LastSessionDate = new DateOnly(2026, 8, 2)
    };

    private sealed class TrackingStream : MemoryStream
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
