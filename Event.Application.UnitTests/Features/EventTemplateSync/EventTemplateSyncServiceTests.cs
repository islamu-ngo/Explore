// ABOUTME: Unit tests for EventTemplateSyncService covering stale-base handling, quota enforcement, projection refresh, and rollback-style failures.
// ABOUTME: Exercises the transactional orchestration behavior without hitting EF Core or persistence implementations.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventTemplateSync;
using Explore.Application.Exceptions;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventTemplateSync;

public class EventTemplateSyncServiceTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventTemplateRepository _templateRepository = Substitute.For<IEventTemplateRepository>();
    private readonly IEventCustomPropertyRepository _runtimeRepository = Substitute.For<IEventCustomPropertyRepository>();
    private readonly IEventTemplateDiffService _diffService = Substitute.For<IEventTemplateDiffService>();
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater = Substitute.For<IEventCustomPropertyProjectionUpdater>();
    private readonly IAuditLogRepository _auditRepository = Substitute.For<IAuditLogRepository>();
    private readonly ICustomPropertyQuotaResolver _quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public EventTemplateSyncServiceTests()
    {
        _quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(10);
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var transaction = call.Arg<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>();
                if (transaction is null)
                    throw new InvalidOperationException("Missing transaction delegate.");

                return transaction(CancellationToken.None);
            });
    }

    [Test]
    public async Task ApplySyncAsync_WhenBaseVersionIsStale_ReturnsStaleSyncConflict()
    {
        _eventRepository.GetById(_eventId).Returns(CreateEvent(sourceTemplateVersion: 2));

        var result = await CreateSut().ApplySyncAsync(_eventId, CreatePlan(addedDefinitionKeys: ["tenant.sync/field"]), 1, CancellationToken.None);

        await Assert.That(result.Conflicts.Count).IsEqualTo(1);
        await Assert.That(result.Conflicts[0].Reason).IsEqualTo(ConcurrencyConflictException.StaleSyncBase);
        await _projectionUpdater.DidNotReceive().RefreshForEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySyncAsync_WhenDefinitionConcurrencyDiffers_ReturnsConcurrentUpdateConflict()
    {
        var currentEvent = CreateEvent();
        var trackedDefinition = CreateRuntimeDefinition();
        trackedDefinition.ConcurrencyStamp = Guid.NewGuid();
        var diffStamp = Guid.NewGuid();
        var plan = CreatePlan(modifiedDefinitionKeys: ["tenant.sync/field"]);
        var diff = new TemplateDiffDto(2, 1, [], [new ModifiedDefinitionDto("tenant.sync", "field", diffStamp, [new FieldChangeDto("DisplayName", "Old", "New", "string")])], [], [], [], [], []);

        _eventRepository.GetById(_eventId).Returns(currentEvent, currentEvent);
        _diffService.ComputeDiffAsync(_eventId, 2, Arg.Any<CancellationToken>()).Returns(diff);
        _runtimeRepository.GetTrackedDefinitionsForEvent(_eventId, Arg.Any<CancellationToken>()).Returns([trackedDefinition]);
        _templateRepository.GetPublishedTemplateVersion(_tenantId, currentEvent.SourceTemplateKey!, 2, Arg.Any<CancellationToken>())
            .Returns(CreateTemplate(2, CreateTemplateDefinition(trackedDefinition.Namespace, trackedDefinition.Key, trackedDefinition.SourceTemplateDefinitionId!.Value, "New")));

        var result = await CreateSut().ApplySyncAsync(_eventId, plan, 1, CancellationToken.None);

        await Assert.That(result.Conflicts.Count).IsEqualTo(1);
        await Assert.That(result.Conflicts[0].Reason).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await _projectionUpdater.DidNotReceive().RefreshForEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySyncAsync_WhenQuotaExceeded_ThrowsValidationException()
    {
        _quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _eventRepository.GetById(_eventId).Returns(CreateEvent());
        _diffService.ComputeDiffAsync(_eventId, 2, Arg.Any<CancellationToken>()).Returns(new TemplateDiffDto(2, 1, [], [], [], [], [], [], []));

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await CreateSut().ApplySyncAsync(_eventId, CreatePlan(addedDefinitionKeys: ["tenant.sync/field"]), 1, CancellationToken.None));
    }

    [Test]
    public async Task ApplySyncAsync_WhenRetiringDefinition_PreservesHistoricalValues()
    {
        var currentEvent = CreateEvent();
        var trackedDefinition = CreateRuntimeDefinition();
        SetValues(trackedDefinition, [new EventCustomPropertyValue { Id = Guid.NewGuid(), EventCustomPropertyDefinitionId = trackedDefinition.Id, EventId = _eventId, TenantId = _tenantId }]);
        var plan = CreatePlan(retiredDefinitionKeys: ["tenant.sync/field"]);
        var diff = new TemplateDiffDto(2, 1, [], [], [new RetiredDefinitionDto("tenant.sync", "field", trackedDefinition.ConcurrencyStamp)], [], [], [], []);

        _eventRepository.GetById(_eventId).Returns(currentEvent, currentEvent);
        _diffService.ComputeDiffAsync(_eventId, 2, Arg.Any<CancellationToken>()).Returns(diff);
        _runtimeRepository.GetTrackedDefinitionsForEvent(_eventId, Arg.Any<CancellationToken>()).Returns([trackedDefinition]);
        _templateRepository.GetPublishedTemplateVersion(_tenantId, currentEvent.SourceTemplateKey!, 2, Arg.Any<CancellationToken>()).Returns(CreateTemplate(2));

        var result = await CreateSut().ApplySyncAsync(_eventId, plan, 1, CancellationToken.None);

        await Assert.That(result.Applied.Count).IsEqualTo(1);
        await Assert.That(trackedDefinition.IsActive).IsFalse();
        await Assert.That(trackedDefinition.Values.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ApplySyncAsync_WhenRetiringOption_PreservesHistoricalValuesAndClearsDefaultOption()
    {
        var currentEvent = CreateEvent();
        var trackedDefinition = CreateRuntimeDefinition(propertyType: PropertyType.Option);
        var trackedOption = CreateRuntimeOption(trackedDefinition.Id, "tenant.sync", "old_option", sourceTemplateOptionId: Guid.NewGuid(), displayName: "Old Option");
        trackedDefinition.DefaultOptionId = trackedOption.Id;
        SetOptions(trackedDefinition, [trackedOption]);
        SetValues(trackedDefinition,
        [
            new EventCustomPropertyValue
            {
                Id = Guid.NewGuid(),
                EventCustomPropertyDefinitionId = trackedDefinition.Id,
                EventId = _eventId,
                TenantId = _tenantId,
                OptionId = trackedOption.Id
            }
        ]);
        var plan = CreatePlan(retiredOptionKeys: ["tenant.sync/old_option"]);
        var diff = new TemplateDiffDto(2, 1, [], [], [], [], [], [new RetiredOptionDto("tenant.sync", "old_option", trackedOption.ConcurrencyStamp)], []);
        var templateDefinition = CreateTemplateDefinition(trackedDefinition.Namespace, trackedDefinition.Key, trackedDefinition.SourceTemplateDefinitionId!.Value, "Field", PropertyType.Option);

        _eventRepository.GetById(_eventId).Returns(currentEvent, currentEvent);
        _diffService.ComputeDiffAsync(_eventId, 2, Arg.Any<CancellationToken>()).Returns(diff);
        _runtimeRepository.GetTrackedDefinitionsForEvent(_eventId, Arg.Any<CancellationToken>()).Returns([trackedDefinition]);
        _templateRepository.GetPublishedTemplateVersion(_tenantId, currentEvent.SourceTemplateKey!, 2, Arg.Any<CancellationToken>()).Returns(CreateTemplate(2, templateDefinition));

        var result = await CreateSut().ApplySyncAsync(_eventId, plan, 1, CancellationToken.None);

        await Assert.That(result.Applied.Count).IsEqualTo(1);
        await Assert.That(trackedOption.IsActive).IsFalse();
        await Assert.That(trackedDefinition.DefaultOptionId).IsNull();
        await Assert.That(trackedDefinition.Values.Single().OptionId).IsEqualTo(trackedOption.Id);
    }

    [Test]
    public async Task ApplySyncAsync_WhenModificationSucceeds_RefreshesProjection()
    {
        var currentEvent = CreateEvent();
        var trackedDefinition = CreateRuntimeDefinition(displayName: "Old");
        var plan = CreatePlan(modifiedDefinitionKeys: ["tenant.sync/field"]);
        var diff = new TemplateDiffDto(2, 1, [], [new ModifiedDefinitionDto("tenant.sync", "field", trackedDefinition.ConcurrencyStamp, [new FieldChangeDto("DisplayName", "Old", "New", "string")])], [], [], [], [], []);

        _eventRepository.GetById(_eventId).Returns(currentEvent, currentEvent);
        _diffService.ComputeDiffAsync(_eventId, 2, Arg.Any<CancellationToken>()).Returns(diff);
        _runtimeRepository.GetTrackedDefinitionsForEvent(_eventId, Arg.Any<CancellationToken>()).Returns([trackedDefinition]);
        _templateRepository.GetPublishedTemplateVersion(_tenantId, currentEvent.SourceTemplateKey!, 2, Arg.Any<CancellationToken>())
            .Returns(CreateTemplate(2, CreateTemplateDefinition(trackedDefinition.Namespace, trackedDefinition.Key, trackedDefinition.SourceTemplateDefinitionId!.Value, "New")));

        var result = await CreateSut().ApplySyncAsync(_eventId, plan, 1, CancellationToken.None);

        await Assert.That(result.Applied.Count).IsEqualTo(1);
        await _projectionUpdater.Received(1).RefreshForEventAsync(_eventId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySyncAsync_WhenTransactionThrows_ReturnsApplyFailedConflict()
    {
        _eventRepository.GetById(_eventId).Returns(CreateEvent());
        _diffService.ComputeDiffAsync(_eventId, 2, Arg.Any<CancellationToken>()).Returns(new TemplateDiffDto(2, 1, [], [], [], [], [], [], []));
        _templateRepository.GetPublishedTemplateVersion(_tenantId, "event-template", 2, Arg.Any<CancellationToken>()).Returns(CreateTemplate(2));
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>(), Arg.Any<CancellationToken>())
            .Returns<Task<TemplateSyncOutcomeDto>>(_ => throw new InvalidOperationException("boom"));

        var result = await CreateSut().ApplySyncAsync(_eventId, CreatePlan(addedDefinitionKeys: ["tenant.sync/field"]), 1, CancellationToken.None);

        await Assert.That(result.Conflicts.Count).IsEqualTo(1);
        await Assert.That(result.Conflicts[0].Reason).IsEqualTo("apply_failed");
    }

    private EventTemplateSyncService CreateSut() => new(
        _eventRepository,
        _templateRepository,
        _runtimeRepository,
        _diffService,
        _projectionUpdater,
        _auditRepository,
        _quotaResolver,
        _currentUserService,
        _unitOfWork);

    private Explore.Domain.Event CreateEvent(int sourceTemplateVersion = 1) => new()
    {
        Id = _eventId,
        TenantId = _tenantId,
        SourceTemplateId = Guid.NewGuid(),
        SourceTemplateKey = "event-template",
        SourceTemplateVersion = sourceTemplateVersion,
        Title = "Event",
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!
    };

    private TemplateSyncPlanDto CreatePlan(
        IReadOnlyList<string>? addedDefinitionKeys = null,
        IReadOnlyList<string>? modifiedDefinitionKeys = null,
        IReadOnlyList<string>? retiredDefinitionKeys = null,
        IReadOnlyList<string>? retiredOptionKeys = null)
        => new()
        {
            TargetTemplateVersion = 2,
            BaseProvenanceVersion = 1,
            AddedDefinitionKeys = addedDefinitionKeys ?? [],
            ModifiedDefinitionKeys = modifiedDefinitionKeys ?? [],
            RetiredDefinitionKeys = retiredDefinitionKeys ?? [],
            RetiredOptionKeys = retiredOptionKeys ?? []
        };

    private static EventTemplate CreateTemplate(int version, params EventTemplateCustomPropertyDefinition[] definitions)
    {
        var template = new EventTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TemplateKey = "event-template",
            DisplayName = "Template",
            Version = version,
            IsPublished = true,
            IsActive = true
        };
        var field = typeof(EventTemplate).GetField("_definitions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventTemplateCustomPropertyDefinition>)field.GetValue(template)!;
        list.AddRange(definitions);
        return template;
    }

    private static EventTemplateCustomPropertyDefinition CreateTemplateDefinition(string ns, string key, Guid id, string displayName, PropertyType propertyType = PropertyType.Text)
        => new()
        {
            Id = id,
            EventTemplateId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Namespace = ns,
            Key = key,
            DisplayName = displayName,
            PropertyType = propertyType,
            IsActive = true,
            ExposureLevel = ExposureLevel.Public
        };

    private static EventCustomPropertyDefinition CreateRuntimeDefinition(string ns = "tenant.sync", string key = "field", string displayName = "Field", PropertyType propertyType = PropertyType.Text)
        => new()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Namespace = ns,
            Key = key,
            DisplayName = displayName,
            PropertyType = propertyType,
            IsActive = true,
            ExposureLevel = ExposureLevel.Public,
            SourceTemplateDefinitionId = Guid.NewGuid(),
            SourceTemplateId = Guid.NewGuid(),
            SourceTemplateKey = "event-template",
            SourceTemplateVersion = 1,
            InstantiatedAt = DateTimeOffset.UtcNow,
            ConcurrencyStamp = Guid.NewGuid()
        };

    private static EventCustomPropertyOption CreateRuntimeOption(Guid definitionId, string ns, string key, Guid? sourceTemplateOptionId, string displayName)
        => new()
        {
            Id = Guid.NewGuid(),
            EventCustomPropertyDefinitionId = definitionId,
            Namespace = ns,
            Key = key,
            DisplayName = displayName,
            Value = key,
            IsActive = true,
            SourceTemplateOptionId = sourceTemplateOptionId,
            SourceTemplateVersion = 1,
            ConcurrencyStamp = Guid.NewGuid()
        };

    private static void SetOptions(EventCustomPropertyDefinition definition, IEnumerable<EventCustomPropertyOption> options)
    {
        var field = typeof(EventCustomPropertyDefinition).GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventCustomPropertyOption>)field.GetValue(definition)!;
        list.AddRange(options);
    }

    private static void SetValues(EventCustomPropertyDefinition definition, IEnumerable<EventCustomPropertyValue> values)
    {
        var field = typeof(EventCustomPropertyDefinition).GetField("_values", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventCustomPropertyValue>)field.GetValue(definition)!;
        list.AddRange(values);
    }
}
