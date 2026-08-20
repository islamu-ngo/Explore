// ABOUTME: Unit tests for the secure public event Open Graph image query.
// ABOUTME: Proves public eligibility, tenant-effective branding, trusted stream use, fallback, disposal, and cancellation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.OpenGraph;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Models.Storage;
using Explore.Domain.Enums;
using NSubstitute;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.Events.Queries;

public sealed class GetPublicEventOpenGraphImageRequestHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
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
            _eventRepository,
            _tenantPolicySettings,
            _contentReader,
            _renderer);
    }

    [Test]
    public async Task Handle_ForPublishedPublicEvent_UsesEffectiveTenantBrandAndRendersPng()
    {
        DomainEvent eventEntity = CreatePublicEvent();
        ConfigurePublicEvent(eventEntity);
        _tenantPolicySettings.ReadEffectiveTenantSettingsAsync(eventEntity.TenantId)
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
        await _tenantPolicySettings.Received(1).ReadEffectiveTenantSettingsAsync(eventEntity.TenantId);
        await _renderer.Received(1).RenderAsync(
            Arg.Is<EventOpenGraphImageRenderRequest>(renderRequest =>
                renderRequest.Title == eventEntity.Title &&
                renderRequest.FirstSessionDate == eventEntity.FirstSessionDate &&
                renderRequest.LastSessionDate == eventEntity.LastSessionDate &&
                renderRequest.BrandDisplayName == "Instance locked brand"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ForNonPublicEvent_ReturnsNullWithoutReadingStorageOrRendering()
    {
        DomainEvent eventEntity = CreatePublicEvent();
        eventEntity.VisibilityTypeId = (int)VisibilityTypeEnum.Private;
        ConfigurePublicEvent(eventEntity);

        EventOpenGraphImageRenderResult? result = await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(result).IsNull();
        await _tenantPolicySettings.DidNotReceiveWithAnyArgs().ReadEffectiveTenantSettingsAsync(default);
        await _contentReader.DidNotReceiveWithAnyArgs().OpenAsync(default, default, default);
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
    }

    [Test]
    public async Task Handle_WithMalformedSlugCode_ReturnsNullWithoutQueryingRepository()
    {
        EventOpenGraphImageRenderResult? result = await _handler.Handle(
            new GetPublicEventOpenGraphImageRequest { SlugCode = "secure-event-" },
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await _eventRepository.DidNotReceiveWithAnyArgs().GetPublicEventForOpenGraphAsync(default!, default);
        await _tenantPolicySettings.DidNotReceiveWithAnyArgs().ReadEffectiveTenantSettingsAsync(default);
        await _contentReader.DidNotReceiveWithAnyArgs().OpenAsync(default, default, default);
        await _renderer.DidNotReceiveWithAnyArgs().RenderAsync(default!, default);
    }

    [Test]
    public async Task Handle_UsesOnlyFeaturedImageIdAsPublicImageSource()
    {
        DomainEvent eventEntity = CreatePublicEvent();
        var stream = new TrackingStream();
        ConfigurePublicEvent(eventEntity);
        _contentReader.OpenAsync(eventEntity.FeaturedImageId!.Value, publicImagesOnly: true, Arg.Any<CancellationToken>())
            .Returns(new StorageObjectContentResult(stream, "image/png", 3, null, null));

        await _handler.Handle(Request(), CancellationToken.None);

        await _contentReader.Received(1).OpenAsync(
            eventEntity.FeaturedImageId!.Value,
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
        DomainEvent eventEntity = CreatePublicEvent();
        ConfigurePublicEvent(eventEntity);
        _contentReader.OpenAsync(eventEntity.FeaturedImageId!.Value, publicImagesOnly: true, Arg.Any<CancellationToken>())
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
        DomainEvent eventEntity = CreatePublicEvent();
        var stream = new TrackingStream();
        ConfigurePublicEvent(eventEntity);
        _contentReader.OpenAsync(eventEntity.FeaturedImageId!.Value, publicImagesOnly: true, Arg.Any<CancellationToken>())
            .Returns(new StorageObjectContentResult(stream, "image/png", 3, null, null));
        _renderer.RenderAsync(Arg.Any<EventOpenGraphImageRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<EventOpenGraphImageRenderResult>(new InvalidOperationException("renderer failed")));

        async Task Act() => await _handler.Handle(Request(), CancellationToken.None);

        await Assert.That(Act).Throws<InvalidOperationException>();
        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Handle_PassesCancellationTokenThroughRepositoryStorageAndRenderer()
    {
        DomainEvent eventEntity = CreatePublicEvent();
        using var source = new CancellationTokenSource();
        CancellationToken cancellationToken = source.Token;
        ConfigurePublicEvent(eventEntity);
        _contentReader.OpenAsync(eventEntity.FeaturedImageId!.Value, publicImagesOnly: true, cancellationToken)
            .Returns((StorageObjectContentResult?)null);

        await _handler.Handle(Request(), cancellationToken);

        await _eventRepository.Received(1).GetPublicEventForOpenGraphAsync(
            "code",
            cancellationToken);
        await _contentReader.Received(1).OpenAsync(eventEntity.FeaturedImageId!.Value, publicImagesOnly: true, cancellationToken);
        await _renderer.Received(1).RenderAsync(Arg.Any<EventOpenGraphImageRenderRequest>(), cancellationToken);
    }

    private void ConfigurePublicEvent(DomainEvent eventEntity)
    {
        _eventRepository.GetPublicEventForOpenGraphAsync("code", Arg.Any<CancellationToken>())
            .Returns(eventEntity);
    }

    private static GetPublicEventOpenGraphImageRequest Request() => new()
    {
        SlugCode = "secure-event-code"
    };

    private static DomainEvent CreatePublicEvent() => new(EventStatusEnum.Published)
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        FeaturedImageId = Guid.CreateVersion7(),
        Title = "Secure event",
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        FirstSessionDate = new DateOnly(2026, 8, 1),
        LastSessionDate = new DateOnly(2026, 8, 2),
        Actor = null!,
        Tenant = null!,
        EventStatus = null!,
        VisibilityType = null!,
        EventFormat = null!
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
