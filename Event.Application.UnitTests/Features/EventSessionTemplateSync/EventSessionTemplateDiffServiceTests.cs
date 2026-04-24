// ABOUTME: Unit tests for EventSessionTemplateDiffService covering explicit add/retire/local-warning behavior.
// ABOUTME: Keeps session-template diff coverage aligned with the event-template sync family.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionTemplateSync;

public class EventSessionTemplateDiffServiceTests
{
    [Test]
    public async Task ComputeDiffAsync_WhenSessionTemplateAddsDefinition_ReturnsAddedDefinition()
    {
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var service = new EventSessionTemplateDiffService(templateRepository, sessionRepository, runtimeRepository);

        var session = new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = tenantId, SourceTemplateId = Guid.NewGuid(), SourceTemplateKey = "session-template", SourceTemplateVersion = 1 };
        var templateDefinition = new EventSessionTemplateCustomPropertyDefinition { Id = Guid.NewGuid(), EventSessionTemplateId = Guid.NewGuid(), TenantId = tenantId, Namespace = "tenant.sync", Key = "session_field", DisplayName = "Session Field", PropertyType = PropertyType.Text, IsActive = true, ExposureLevel = ExposureLevel.Public };
        var template = new EventSessionTemplate { Id = session.SourceTemplateId.Value, EventTemplateId = Guid.NewGuid(), TenantId = tenantId, SessionTemplateKey = "session-template", DisplayName = "Session Template", Version = 2, IsPublished = true, IsActive = true };
        SetSessionTemplateDefinitions(template, [templateDefinition]);

        sessionRepository.GetById(sessionId).Returns(session);
        runtimeRepository.GetAllDefinitionsForSession(sessionId).Returns([]);
        templateRepository.GetPublishedSessionTemplateVersion(session.SourceTemplateId.Value, session.SourceTemplateKey!, 2, Arg.Any<CancellationToken>()).Returns(template);

        var result = await service.ComputeDiffAsync(sessionId, 2, CancellationToken.None);

        await Assert.That(result.AddedDefinitions.Count).IsEqualTo(1);
        await Assert.That(result.AddedDefinitions[0].Key).IsEqualTo("session_field");
    }

    [Test]
    public async Task ComputeDiffAsync_WhenSessionDefinitionIsLocal_ReturnsUntouchedWarning()
    {
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var templateRepository = Substitute.For<IEventSessionTemplateRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var runtimeRepository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var service = new EventSessionTemplateDiffService(templateRepository, sessionRepository, runtimeRepository);

        var session = new EventSession { Id = sessionId, Event = null!, Tenant = null!, TenantId = tenantId, SourceTemplateId = Guid.NewGuid(), SourceTemplateKey = "session-template", SourceTemplateVersion = 1 };
        var localDefinition = new EventSessionCustomPropertyDefinition { Id = Guid.NewGuid(), EventSessionId = sessionId, TenantId = tenantId, Namespace = "tenant.local", Key = "notes", DisplayName = "Notes", PropertyType = PropertyType.Text, IsActive = true, ExposureLevel = ExposureLevel.Public };
        var template = new EventSessionTemplate { Id = session.SourceTemplateId.Value, EventTemplateId = Guid.NewGuid(), TenantId = tenantId, SessionTemplateKey = "session-template", DisplayName = "Session Template", Version = 2, IsPublished = true, IsActive = true };

        sessionRepository.GetById(sessionId).Returns(session);
        runtimeRepository.GetAllDefinitionsForSession(sessionId).Returns([localDefinition]);
        templateRepository.GetPublishedSessionTemplateVersion(session.SourceTemplateId.Value, session.SourceTemplateKey!, 2, Arg.Any<CancellationToken>()).Returns(template);

        var result = await service.ComputeDiffAsync(sessionId, 2, CancellationToken.None);

        await Assert.That(result.UntouchedLocalDefinitions.Count).IsEqualTo(1);
        await Assert.That(result.UntouchedLocalDefinitions[0].Reason).IsEqualTo("LocallyAdded");
    }

    private static void SetSessionTemplateDefinitions(EventSessionTemplate template, IEnumerable<EventSessionTemplateCustomPropertyDefinition> definitions)
    {
        var field = typeof(EventSessionTemplate).GetField("_definitions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<EventSessionTemplateCustomPropertyDefinition>)field.GetValue(template)!;
        list.AddRange(definitions);
    }
}
