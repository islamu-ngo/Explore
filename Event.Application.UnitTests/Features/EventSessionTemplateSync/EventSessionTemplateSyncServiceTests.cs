// ABOUTME: Unit tests for EventSessionTemplateSyncService covering stale-base conflicts and projection refresh on success.
// ABOUTME: Mirrors the event-template sync orchestration tests for the session scope.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionTemplateSync;
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
        currentUser.UserId.Returns(Guid.NewGuid());
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<TemplateSyncOutcomeDto>>>()(CancellationToken.None));

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

    private static void SetSessionTemplateDefinitions(EventSessionTemplate template, IEnumerable<EventSessionTemplateCustomPropertyDefinition> definitions)
    {
        var field = typeof(EventSessionTemplate).GetField("_definitions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventSessionTemplateCustomPropertyDefinition>)field.GetValue(template)!;
        list.AddRange(definitions);
    }
}
