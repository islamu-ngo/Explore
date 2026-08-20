// ABOUTME: Unit tests for public event-report option query handling.
// ABOUTME: Verifies reportable-status checks, tenant boundaries, and HybridCache reason taxonomy usage.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Queries;
using Explore.Application.Features.EventReporting.Policies;
using Explore.Application.Features.EventReporting.Requests.Queries;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Queries;

public sealed class GetEventReportOptionsRequestHandlerTests
{
    private const string ReasonOptionsCacheKey = "event-reporting:reason-options:v1";

    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TestHybridCache _cache = new();
    private readonly EventReportSubmissionOptions _options = new()
    {
        MaxReporterTextLength = 1234
    };

    [Test]
    public async Task Handle_WhenEventIsPublished_ReturnsReportableReasonOptions()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(eventId, tenantId, EventStatusEnum.Published));

        var result = await CreateHandler().Handle(
            new GetEventReportOptionsRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EventId).IsEqualTo(eventId);
        await Assert.That(result.IsReportable).IsTrue();
        await Assert.That(result.UnavailableReasonCode).IsNull();
        await Assert.That(result.MaxReporterTextLength).IsEqualTo(1234);
        await Assert.That(result.ReasonOptions).Count().IsEqualTo(EventReportReasonCodePolicy.AllowedReasonCodes.Count);
        await Assert.That(result.ReasonOptions.Select(option => option.ReasonCode)).Contains("spam");
        await Assert.That(result.ReasonOptions.Single(option => option.ReasonCode == "spam").ReasonName).IsEqualTo("Spam");
        await Assert.That(_cache.FactoryCallsByKey[ReasonOptionsCacheKey]).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_WhenReasonOptionsAreRequestedTwice_UsesCachedReasonCatalog()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(eventId, tenantId, EventStatusEnum.Published));
        var handler = CreateHandler();

        var first = await handler.Handle(new GetEventReportOptionsRequest { EventId = eventId }, CancellationToken.None);
        var second = await handler.Handle(new GetEventReportOptionsRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(first!.ReasonOptions.Count).IsEqualTo(second!.ReasonOptions.Count);
        await Assert.That(_cache.FactoryCallsByKey[ReasonOptionsCacheKey]).IsEqualTo(1);
        await _eventRepository.Received(2).GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotPublished_ReturnsNotReportableWithoutReasonOptions()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(eventId, tenantId, EventStatusEnum.Draft));

        var result = await CreateHandler().Handle(
            new GetEventReportOptionsRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsReportable).IsFalse();
        await Assert.That(result.UnavailableReasonCode).IsEqualTo("event_not_reportable_status");
        await Assert.That(result.ReasonOptions).IsEmpty();
        await Assert.That(_cache.FactoryCallsByKey).IsEmpty();
    }

    [Test]
    public async Task Handle_WhenEventBelongsToAnotherTenant_ReturnsNull()
    {
        var currentTenantId = Guid.NewGuid();
        var eventId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(currentTenantId);
        _eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(eventId, Guid.NewGuid(), EventStatusEnum.Published));

        var result = await CreateHandler().Handle(
            new GetEventReportOptionsRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await Assert.That(_cache.FactoryCallsByKey).IsEmpty();
    }

    private GetEventReportOptionsRequestHandler CreateHandler()
    {
        return new GetEventReportOptionsRequestHandler(
            _eventRepository,
            _tenantContext,
            _cache,
            Options.Create(_options));
    }

    private static Explore.Domain.Event CreateEvent(Guid eventId, Guid tenantId, EventStatusEnum status)
    {
        return new Explore.Domain.Event(status)
        {
            Id = eventId,
            TenantId = tenantId,
            Tenant = null!,
            Title = "Reportable Event",
            Actor = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
    }

    private sealed class TestHybridCache : HybridCache
    {
        private readonly Dictionary<string, object?> _values = new();
        private readonly Dictionary<string, int> _factoryCallsByKey = new();

        public Dictionary<string, int> FactoryCallsByKey => _factoryCallsByKey;

        public override async ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (_values.ContainsKey(key))
            {
                return (T)_values[key]!;
            }

            _factoryCallsByKey[key] = _factoryCallsByKey.GetValueOrDefault(key) + 1;
            var created = await factory(state, cancellationToken);
            _values[key] = created;
            return created;
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            return ValueTask.CompletedTask;
        }
    }
}
