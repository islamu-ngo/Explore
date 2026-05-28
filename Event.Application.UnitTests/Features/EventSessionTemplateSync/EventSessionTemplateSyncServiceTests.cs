// ABOUTME: Unit tests for EventSessionTemplateSyncService covering stale-base conflicts and projection refresh on success.
// ABOUTME: Mirrors the event-template sync orchestration tests for the session scope.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Exceptions;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionTemplateSync;

public class EventSessionTemplateSyncServiceTests
{
    [Test]
    public async Task ApplySyncAsync_WhenBaseVersionIsStale_ReturnsConflict()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var diffService = Substitute.For<IEventSessionTemplateDiffService>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var sessionId = Guid.NewGuid();
        eventSessionRepository.GetById(sessionId).Returns(new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = Guid.NewGuid(), SourceTemplateId = Guid.NewGuid(), SourceTemplateKey = "session-template", SourceTemplateVersion = 2 });

        var service = new EventSessionTemplateSyncService(eventSessionRepository, templateRepository, runtimeRepository, diffService, projectionUpdater, auditRepository, quotaResolver, currentUser, unitOfWork);
        var plan = new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, AddedDefinitionKeys = ["tenant.sync/session"] };

        var result = await service.ApplySyncAsync(sessionId, plan, 1, CancellationToken.None);

        await Assert.That(result.Conflicts.Count).IsEqualTo(1);
        await projectionUpdater.DidNotReceive().RefreshForEventSessionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySyncAsync_WhenChangeCountQuotaExceeded_ThrowsQuotaExceededException()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var diffService = Substitute.For<IEventSessionTemplateDiffService>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key, tenantId, Arg.Any<CancellationToken>()).Returns(0);
        eventSessionRepository.GetById(sessionId).Returns(new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = tenantId, SourceTemplateId = Guid.NewGuid(), SourceTemplateKey = "session-template", SourceTemplateVersion = 1 });
        diffService.ComputeDiffAsync(sessionId, 2, Arg.Any<CancellationToken>()).Returns(new TemplateDiffDto(2, 1, [], [], [], [], [], [], []));

        var service = new EventSessionTemplateSyncService(eventSessionRepository, templateRepository, runtimeRepository, diffService, projectionUpdater, auditRepository, quotaResolver, currentUser, unitOfWork);

        var exception = await Assert.ThrowsAsync<QuotaExceededException>(async () =>
            await service.ApplySyncAsync(sessionId, new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, AddedDefinitionKeys = ["tenant.sync/session"] }, 1, CancellationToken.None));

        await Assert.That(exception.Details.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key);
        await Assert.That(exception.Details.Limit).IsEqualTo(0);
        await Assert.That(exception.Details.Actual).IsEqualTo(1);
        await Assert.That(exception.Details.Attempted).IsEqualTo(1);
        await Assert.That(exception.Details.Scope).IsEqualTo("event_session_template_sync");
        await Assert.That(exception.Details.TenantId).IsEqualTo(tenantId);

        await templateRepository.DidNotReceiveWithAnyArgs().GetPublishedSessionTemplateVersion(default, default!, default, default);
    }

    [Test]
    public async Task ApplySyncAsync_WhenPayloadQuotaExceeded_ThrowsQuotaExceededException()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var diffService = Substitute.For<IEventSessionTemplateDiffService>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key, tenantId, Arg.Any<CancellationToken>()).Returns(10);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxPayloadBytes.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);
        eventSessionRepository.GetById(sessionId).Returns(new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = tenantId, SourceTemplateId = Guid.NewGuid(), SourceTemplateKey = "session-template", SourceTemplateVersion = 1 });
        diffService.ComputeDiffAsync(sessionId, 2, Arg.Any<CancellationToken>()).Returns(new TemplateDiffDto(2, 1, [], [], [], [], [], [], []));

        var service = new EventSessionTemplateSyncService(eventSessionRepository, templateRepository, runtimeRepository, diffService, projectionUpdater, auditRepository, quotaResolver, currentUser, unitOfWork);

        var exception = await Assert.ThrowsAsync<QuotaExceededException>(async () =>
            await service.ApplySyncAsync(sessionId, new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, AddedDefinitionKeys = ["tenant.sync/session"] }, 1, CancellationToken.None));

        await Assert.That(exception.Details.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxPayloadBytes.Key);
        await Assert.That(exception.Details.Limit).IsEqualTo(1);
        await Assert.That(exception.Details.Actual).IsNotNull();
        await Assert.That(exception.Details.Actual!.Value).IsGreaterThan(1);
        await Assert.That(exception.Details.Attempted).IsEqualTo(exception.Details.Actual);
        await Assert.That(exception.Details.Scope).IsEqualTo("event_session_template_sync_payload");
        await Assert.That(exception.Details.TenantId).IsEqualTo(tenantId);

        await templateRepository.DidNotReceiveWithAnyArgs().GetPublishedSessionTemplateVersion(default, default!, default, default);
    }

    [Test]
    public async Task ApplySyncAsync_WhenAddedDefinitionWouldExceedRuntimeDefinitionQuota_ThrowsQuotaExceededException()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var diffService = Substitute.For<IEventSessionTemplateDiffService>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var unitOfWork = CreateUnitOfWork();

        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sourceTemplateId = Guid.NewGuid();
        var session = new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = tenantId, SourceTemplateId = sourceTemplateId, SourceTemplateKey = "session-template", SourceTemplateVersion = 1 };
        var existingDefinition = new EventSessionCustomPropertyDefinition { Id = Guid.NewGuid(), EventSessionId = sessionId, TenantId = tenantId, Namespace = "tenant.sync", Key = "existing", DisplayName = "Existing", PropertyType = PropertyType.Text, IsActive = true, ExposureLevel = ExposureLevel.Public, InstantiatedAt = DateTimeOffset.UtcNow };
        var templateDefinition = new EventSessionTemplateCustomPropertyDefinition { Id = Guid.NewGuid(), EventSessionTemplateId = sourceTemplateId, TenantId = tenantId, Namespace = "tenant.sync", Key = "new_session_field", DisplayName = "New Session Field", PropertyType = PropertyType.Text, IsActive = true, ExposureLevel = ExposureLevel.Public };
        var template = new EventSessionTemplate { Id = sourceTemplateId, EventTemplateId = Guid.NewGuid(), TenantId = tenantId, SessionTemplateKey = session.SourceTemplateKey!, DisplayName = "Session Template", Version = 2, IsPublished = true, IsActive = true };
        SetSessionTemplateDefinitions(template, [templateDefinition]);
        var diff = new TemplateDiffDto(2, 1, [CreateAddedDefinitionDto("tenant.sync", "new_session_field")], [], [], [], [], [], []);

        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key, tenantId, Arg.Any<CancellationToken>()).Returns(10);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxPayloadBytes.Key, tenantId, Arg.Any<CancellationToken>()).Returns(262_144);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerEventSession.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, tenantId, Arg.Any<CancellationToken>()).Returns(10);
        eventSessionRepository.GetById(sessionId).Returns(session, session);
        diffService.ComputeDiffAsync(sessionId, 2, Arg.Any<CancellationToken>()).Returns(diff);
        runtimeRepository.GetTrackedDefinitionsForSession(sessionId, Arg.Any<CancellationToken>()).Returns([existingDefinition]);
        templateRepository.GetPublishedSessionTemplateVersion(sourceTemplateId, session.SourceTemplateKey!, 2, Arg.Any<CancellationToken>()).Returns(template);

        var service = new EventSessionTemplateSyncService(eventSessionRepository, templateRepository, runtimeRepository, diffService, projectionUpdater, auditRepository, quotaResolver, currentUser, unitOfWork);

        var exception = await Assert.ThrowsAsync<QuotaExceededException>(async () =>
            await service.ApplySyncAsync(sessionId, new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, AddedDefinitionKeys = ["tenant.sync/new_session_field"] }, 1, CancellationToken.None));

        await Assert.That(exception.Details.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerEventSession.Key);
        await Assert.That(exception.Details.Limit).IsEqualTo(1);
        await Assert.That(exception.Details.Actual).IsEqualTo(1);
        await Assert.That(exception.Details.Attempted).IsEqualTo(2);
        await Assert.That(exception.Details.Scope).IsEqualTo("event_session_custom_property_definitions");
        await Assert.That(exception.Details.TenantId).IsEqualTo(tenantId);
        await runtimeRepository.DidNotReceiveWithAnyArgs().CreateWithOptions(default!, default!, default, default);
    }

    [Test]
    public async Task ApplySyncAsync_WhenAddedOptionWouldExceedRuntimeOptionQuota_ThrowsQuotaExceededException()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var diffService = Substitute.For<IEventSessionTemplateDiffService>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var unitOfWork = CreateUnitOfWork();

        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sourceTemplateId = Guid.NewGuid();
        var session = new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = tenantId, SourceTemplateId = sourceTemplateId, SourceTemplateKey = "session-template", SourceTemplateVersion = 1 };
        var runtimeDefinition = new EventSessionCustomPropertyDefinition { Id = Guid.NewGuid(), EventSessionId = sessionId, TenantId = tenantId, Namespace = "tenant.sync", Key = "session", DisplayName = "Session", PropertyType = PropertyType.Option, IsActive = true, ExposureLevel = ExposureLevel.Public, SourceTemplateId = sourceTemplateId, SourceTemplateKey = session.SourceTemplateKey, SourceTemplateVersion = 1, SourceTemplateDefinitionId = Guid.NewGuid(), InstantiatedAt = DateTimeOffset.UtcNow, ConcurrencyStamp = Guid.NewGuid() };
        var runtimeOption = CreateRuntimeOption(runtimeDefinition.Id, runtimeDefinition.Namespace, "existing", sourceTemplateOptionId: Guid.NewGuid(), displayName: "Existing");
        SetRuntimeOptions(runtimeDefinition, [runtimeOption]);
        var templateDefinition = new EventSessionTemplateCustomPropertyDefinition { Id = runtimeDefinition.SourceTemplateDefinitionId.Value, EventSessionTemplateId = sourceTemplateId, TenantId = tenantId, Namespace = runtimeDefinition.Namespace, Key = runtimeDefinition.Key, DisplayName = runtimeDefinition.DisplayName, PropertyType = PropertyType.Option, IsActive = true, ExposureLevel = ExposureLevel.Public };
        var templateOption = CreateSessionTemplateOption(templateDefinition.Id, runtimeDefinition.Namespace, "new_option", Guid.NewGuid(), "New Option");
        SetSessionTemplateOptions(templateDefinition, [templateOption]);
        var template = new EventSessionTemplate { Id = sourceTemplateId, EventTemplateId = Guid.NewGuid(), TenantId = tenantId, SessionTemplateKey = session.SourceTemplateKey!, DisplayName = "Session Template", Version = 2, IsPublished = true, IsActive = true };
        SetSessionTemplateDefinitions(template, [templateDefinition]);
        var diff = new TemplateDiffDto(2, 1, [], [], [], [CreateAddedOptionDto(runtimeDefinition.Namespace, "new_option")], [], [], []);

        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key, tenantId, Arg.Any<CancellationToken>()).Returns(10);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxPayloadBytes.Key, tenantId, Arg.Any<CancellationToken>()).Returns(262_144);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerEventSession.Key, tenantId, Arg.Any<CancellationToken>()).Returns(10);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);
        eventSessionRepository.GetById(sessionId).Returns(session, session);
        diffService.ComputeDiffAsync(sessionId, 2, Arg.Any<CancellationToken>()).Returns(diff);
        runtimeRepository.GetTrackedDefinitionsForSession(sessionId, Arg.Any<CancellationToken>()).Returns([runtimeDefinition]);
        templateRepository.GetPublishedSessionTemplateVersion(sourceTemplateId, session.SourceTemplateKey!, 2, Arg.Any<CancellationToken>()).Returns(template);

        var service = new EventSessionTemplateSyncService(eventSessionRepository, templateRepository, runtimeRepository, diffService, projectionUpdater, auditRepository, quotaResolver, currentUser, unitOfWork);

        var exception = await Assert.ThrowsAsync<QuotaExceededException>(async () =>
            await service.ApplySyncAsync(sessionId, new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, AddedOptionKeys = [$"{runtimeDefinition.Namespace}/new_option"] }, 1, CancellationToken.None));

        await Assert.That(exception.Details.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key);
        await Assert.That(exception.Details.Limit).IsEqualTo(1);
        await Assert.That(exception.Details.Actual).IsEqualTo(1);
        await Assert.That(exception.Details.Attempted).IsEqualTo(2);
        await Assert.That(exception.Details.Scope).IsEqualTo("event_session_custom_property_options");
        await Assert.That(exception.Details.TenantId).IsEqualTo(tenantId);
        await runtimeRepository.DidNotReceiveWithAnyArgs().CreateOption(default!, default);
    }

    [Test]
    public async Task ApplySyncAsync_WhenModificationSucceeds_RefreshesProjection()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var diffService = Substitute.For<IEventSessionTemplateDiffService>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(10);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxPayloadBytes.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(262_144);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerEventSession.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(20);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(50);
        currentUser.UserId.Returns(Guid.NewGuid());
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var transaction = call.Arg<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>();
                if (transaction is null)
                    throw new InvalidOperationException("Missing transaction delegate.");

                return transaction(CancellationToken.None);
            });

        var sessionId = Guid.NewGuid();
        var session = new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = Guid.NewGuid(), SourceTemplateId = Guid.NewGuid(), SourceTemplateKey = "session-template", SourceTemplateVersion = 1 };
        var runtimeDefinition = new EventSessionCustomPropertyDefinition { Id = Guid.NewGuid(), EventSessionId = sessionId, TenantId = session.TenantId, Namespace = "tenant.sync", Key = "session", DisplayName = "Old", PropertyType = PropertyType.Text, IsActive = true, ExposureLevel = ExposureLevel.Public, SourceTemplateId = session.SourceTemplateId, SourceTemplateKey = session.SourceTemplateKey, SourceTemplateVersion = 1, SourceTemplateDefinitionId = Guid.NewGuid(), InstantiatedAt = DateTimeOffset.UtcNow, ConcurrencyStamp = Guid.NewGuid() };
        var templateDefinition = new EventSessionTemplateCustomPropertyDefinition { Id = runtimeDefinition.SourceTemplateDefinitionId!.Value, EventSessionTemplateId = session.SourceTemplateId.Value, TenantId = session.TenantId, Namespace = "tenant.sync", Key = "session", DisplayName = "New", PropertyType = PropertyType.Text, IsActive = true, ExposureLevel = ExposureLevel.Public };
        var template = new EventSessionTemplate { Id = session.SourceTemplateId.Value, EventTemplateId = Guid.NewGuid(), TenantId = session.TenantId, SessionTemplateKey = session.SourceTemplateKey!, DisplayName = "Session Template", Version = 2, IsPublished = true, IsActive = true };
        SetSessionTemplateDefinitions(template, [templateDefinition]);

        eventSessionRepository.GetById(sessionId).Returns(session, session);
        diffService.ComputeDiffAsync(sessionId, 2, Arg.Any<CancellationToken>()).Returns(new TemplateDiffDto(2, 1, [], [new ModifiedDefinitionDto("tenant.sync", "session", runtimeDefinition.ConcurrencyStamp, [new FieldChangeDto("DisplayName", "Old", "New", "string")])], [], [], [], [], []));
        runtimeRepository.GetTrackedDefinitionsForSession(sessionId, Arg.Any<CancellationToken>()).Returns([runtimeDefinition]);
        templateRepository.GetPublishedSessionTemplateVersion(session.SourceTemplateId.Value, session.SourceTemplateKey!, 2, Arg.Any<CancellationToken>()).Returns(template);

        var service = new EventSessionTemplateSyncService(eventSessionRepository, templateRepository, runtimeRepository, diffService, projectionUpdater, auditRepository, quotaResolver, currentUser, unitOfWork);
        var result = await service.ApplySyncAsync(sessionId, new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, ModifiedDefinitionKeys = ["tenant.sync/session"] }, 1, CancellationToken.None);

        await Assert.That(result.Applied.Count).IsEqualTo(1);
        await projectionUpdater.Received(1).RefreshForEventSessionAsync(sessionId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplySyncAsync_WhenRetiringOption_PreservesHistoricalValuesAndClearsDefaultOption()
    {
        var eventSessionRepository = Substitute.For<IEventSessionRepository>();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var diffService = Substitute.For<IEventSessionTemplateDiffService>();
        var projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var auditRepository = Substitute.For<IAuditLogRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(10);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxPayloadBytes.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(262_144);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerEventSession.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(20);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(50);
        currentUser.UserId.Returns(Guid.NewGuid());
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var transaction = call.Arg<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>();
                if (transaction is null)
                    throw new InvalidOperationException("Missing transaction delegate.");

                return transaction(CancellationToken.None);
            });

        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sourceTemplateId = Guid.NewGuid();
        var session = new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = tenantId, SourceTemplateId = sourceTemplateId, SourceTemplateKey = "session-template", SourceTemplateVersion = 1 };
        var runtimeDefinition = new EventSessionCustomPropertyDefinition { Id = Guid.NewGuid(), EventSessionId = sessionId, TenantId = tenantId, Namespace = "tenant.sync", Key = "session", DisplayName = "Session", PropertyType = PropertyType.Option, IsActive = true, ExposureLevel = ExposureLevel.Public, SourceTemplateId = sourceTemplateId, SourceTemplateKey = session.SourceTemplateKey, SourceTemplateVersion = 1, SourceTemplateDefinitionId = Guid.NewGuid(), InstantiatedAt = DateTimeOffset.UtcNow, ConcurrencyStamp = Guid.NewGuid() };
        var runtimeOption = CreateRuntimeOption(runtimeDefinition.Id, runtimeDefinition.Namespace, "old_option", sourceTemplateOptionId: Guid.NewGuid(), displayName: "Old Option");
        runtimeDefinition.DefaultOptionId = runtimeOption.Id;
        SetRuntimeOptions(runtimeDefinition, [runtimeOption]);
        SetRuntimeValues(runtimeDefinition,
        [
            new EventSessionCustomPropertyValue
            {
                Id = Guid.NewGuid(),
                EventSessionCustomPropertyDefinitionId = runtimeDefinition.Id,
                EventSessionId = sessionId,
                TenantId = tenantId,
                OptionId = runtimeOption.Id
            }
        ]);

        var templateDefinition = new EventSessionTemplateCustomPropertyDefinition { Id = runtimeDefinition.SourceTemplateDefinitionId.Value, EventSessionTemplateId = sourceTemplateId, TenantId = tenantId, Namespace = runtimeDefinition.Namespace, Key = runtimeDefinition.Key, DisplayName = runtimeDefinition.DisplayName, PropertyType = PropertyType.Option, IsActive = true, ExposureLevel = ExposureLevel.Public };
        var template = new EventSessionTemplate { Id = sourceTemplateId, EventTemplateId = Guid.NewGuid(), TenantId = tenantId, SessionTemplateKey = session.SourceTemplateKey!, DisplayName = "Session Template", Version = 2, IsPublished = true, IsActive = true };
        SetSessionTemplateDefinitions(template, [templateDefinition]);
        var diff = new TemplateDiffDto(2, 1, [], [], [], [], [], [new RetiredOptionDto(runtimeDefinition.Namespace, runtimeOption.Key, runtimeOption.ConcurrencyStamp)], []);

        eventSessionRepository.GetById(sessionId).Returns(session, session);
        diffService.ComputeDiffAsync(sessionId, 2, Arg.Any<CancellationToken>()).Returns(diff);
        runtimeRepository.GetTrackedDefinitionsForSession(sessionId, Arg.Any<CancellationToken>()).Returns([runtimeDefinition]);
        templateRepository.GetPublishedSessionTemplateVersion(sourceTemplateId, session.SourceTemplateKey!, 2, Arg.Any<CancellationToken>()).Returns(template);

        var service = new EventSessionTemplateSyncService(eventSessionRepository, templateRepository, runtimeRepository, diffService, projectionUpdater, auditRepository, quotaResolver, currentUser, unitOfWork);
        var result = await service.ApplySyncAsync(sessionId, new TemplateSyncPlanDto { TargetTemplateVersion = 2, BaseProvenanceVersion = 1, RetiredOptionKeys = [$"{runtimeDefinition.Namespace}/{runtimeOption.Key}"] }, 1, CancellationToken.None);

        await Assert.That(result.Applied.Count).IsEqualTo(1);
        await Assert.That(runtimeOption.IsActive).IsFalse();
        await Assert.That(runtimeDefinition.DefaultOptionId).IsNull();
        await Assert.That(runtimeDefinition.Values.Single().OptionId).IsEqualTo(runtimeOption.Id);
    }

    private static void SetSessionTemplateDefinitions(EventSessionTemplate template, IEnumerable<EventSessionTemplateCustomPropertyDefinition> definitions)
    {
        var field = typeof(EventSessionTemplate).GetField("_definitions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventSessionTemplateCustomPropertyDefinition>)field.GetValue(template)!;
        list.AddRange(definitions);
    }

    private static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var transaction = call.Arg<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>();
                if (transaction is null)
                    throw new InvalidOperationException("Missing transaction delegate.");

                return transaction(CancellationToken.None);
            });
        return unitOfWork;
    }

    private static AddedDefinitionDto CreateAddedDefinitionDto(string ns, string key)
        => new(
            ns,
            key,
            "New Session Field",
            null,
            PropertyType.Text.ToString(),
            false,
            false,
            null,
            ExposureLevel.Public.ToString(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            []);

    private static AddedOptionDto CreateAddedOptionDto(string ns, string key)
        => new(ns, key, "New Option", null, key, false, true, 1, null);

    private static EventSessionTemplateCustomPropertyOption CreateSessionTemplateOption(Guid definitionId, string ns, string key, Guid id, string displayName)
        => new()
        {
            Id = id,
            EventSessionTemplateCustomPropertyDefinitionId = definitionId,
            Namespace = ns,
            Key = key,
            DisplayName = displayName,
            Value = key,
            IsActive = true
        };

    private static void SetSessionTemplateOptions(EventSessionTemplateCustomPropertyDefinition definition, IEnumerable<EventSessionTemplateCustomPropertyOption> options)
    {
        var field = typeof(EventSessionTemplateCustomPropertyDefinition).GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventSessionTemplateCustomPropertyOption>)field.GetValue(definition)!;
        list.AddRange(options);
    }

    private static EventSessionCustomPropertyOption CreateRuntimeOption(Guid definitionId, string ns, string key, Guid? sourceTemplateOptionId, string displayName)
        => new()
        {
            Id = Guid.NewGuid(),
            EventSessionCustomPropertyDefinitionId = definitionId,
            Namespace = ns,
            Key = key,
            DisplayName = displayName,
            Value = key,
            IsActive = true,
            SourceTemplateOptionId = sourceTemplateOptionId,
            SourceTemplateVersion = 1,
            ConcurrencyStamp = Guid.NewGuid()
        };

    private static void SetRuntimeOptions(EventSessionCustomPropertyDefinition definition, IEnumerable<EventSessionCustomPropertyOption> options)
    {
        var field = typeof(EventSessionCustomPropertyDefinition).GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventSessionCustomPropertyOption>)field.GetValue(definition)!;
        list.AddRange(options);
    }

    private static void SetRuntimeValues(EventSessionCustomPropertyDefinition definition, IEnumerable<EventSessionCustomPropertyValue> values)
    {
        var field = typeof(EventSessionCustomPropertyDefinition).GetField("_values", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventSessionCustomPropertyValue>)field.GetValue(definition)!;
        list.AddRange(values);
    }
}
