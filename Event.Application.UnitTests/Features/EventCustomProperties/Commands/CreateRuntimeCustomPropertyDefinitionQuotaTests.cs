// ABOUTME: Quota regression tests for event and session runtime custom-property definition creation.
// ABOUTME: Verifies create handlers fail before persistence when definition or option quotas are exceeded.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.DTOs.EventSessionCustomProperty;
using Explore.Application.Features.EventCustomProperties.Handlers.Commands;
using Explore.Application.Features.EventCustomProperties.Requests.Commands;
using Explore.Application.Features.EventSessionCustomProperties.Handlers.Commands;
using Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventCustomProperties.Commands;

public class CreateRuntimeCustomPropertyDefinitionQuotaTests
{
    [Test]
    public async Task EventHandle_WhenDefinitionQuotaReached_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var repository = Substitute.For<IEventCustomPropertyRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var handler = CreateEventHandler(repository, quotaResolver, tenantId);

        repository.ExistsDefinitionKey(eventId, "tenant.community", "prayer_notes").Returns(false);
        repository.CountDefinitionsForEvent(eventId, Arg.Any<CancellationToken>()).Returns(3);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerEvent.Key, tenantId, Arg.Any<CancellationToken>()).Returns(3);

        var result = await handler.Handle(
            new CreateEventCustomPropertyDefinitionCommand { DefinitionDto = CreateEventDto(eventId) },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors!.Any(error => error.Contains("quota_exceeded", StringComparison.Ordinal))).IsTrue();
        await repository.DidNotReceiveWithAnyArgs().CreateWithOptions(default!, default!, default, default);
    }

    [Test]
    public async Task EventSessionHandle_WhenDefinitionQuotaReached_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var repository = Substitute.For<IEventSessionCustomPropertyRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var handler = CreateSessionHandler(repository, quotaResolver, tenantId);

        repository.ExistsDefinitionKey(sessionId, "tenant.community", "prayer_notes").Returns(false);
        repository.CountDefinitionsForSession(sessionId, Arg.Any<CancellationToken>()).Returns(2);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerEventSession.Key, tenantId, Arg.Any<CancellationToken>()).Returns(2);

        var result = await handler.Handle(
            new CreateEventSessionCustomPropertyDefinitionCommand { DefinitionDto = CreateSessionDto(sessionId) },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors!.Any(error => error.Contains("quota_exceeded", StringComparison.Ordinal))).IsTrue();
        await repository.DidNotReceiveWithAnyArgs().CreateWithOptions(default!, default!, default, default);
    }

    private static CreateEventCustomPropertyDefinitionCommandHandler CreateEventHandler(
        IEventCustomPropertyRepository repository,
        ICustomPropertyQuotaResolver quotaResolver,
        Guid tenantId)
    {
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var mapper = Substitute.For<IMapper>();
        var cache = Substitute.For<HybridCache>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        tenantContext.TenantId.Returns(tenantId);
        quotaResolver.GetIntAsync(Arg.Any<string>(), tenantId, Arg.Any<CancellationToken>()).Returns(500);
        governancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "tenant.community",
                NormalizedKey = "prayer_notes",
            });

        return new CreateEventCustomPropertyDefinitionCommandHandler(
            repository,
            governancePolicy,
            quotaResolver,
            tenantContext,
            currentUserService,
            mapper,
            cache,
            unitOfWork);
    }

    private static CreateEventSessionCustomPropertyDefinitionCommandHandler CreateSessionHandler(
        IEventSessionCustomPropertyRepository repository,
        ICustomPropertyQuotaResolver quotaResolver,
        Guid tenantId)
    {
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var mapper = Substitute.For<IMapper>();
        var cache = Substitute.For<HybridCache>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        tenantContext.TenantId.Returns(tenantId);
        quotaResolver.GetIntAsync(Arg.Any<string>(), tenantId, Arg.Any<CancellationToken>()).Returns(500);
        governancePolicy.EvaluateDefinition(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CustomPropertyGovernanceEvaluation
            {
                NormalizedNamespace = "tenant.community",
                NormalizedKey = "prayer_notes",
            });

        return new CreateEventSessionCustomPropertyDefinitionCommandHandler(
            repository,
            governancePolicy,
            quotaResolver,
            tenantContext,
            currentUserService,
            mapper,
            cache,
            unitOfWork);
    }

    private static CreateEventCustomPropertyDefinitionDto CreateEventDto(Guid eventId)
    {
        return new CreateEventCustomPropertyDefinitionDto
        {
            EventId = eventId,
            Namespace = "Tenant Community",
            Key = "Prayer Notes",
            DisplayName = "Prayer Notes",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
        };
    }

    private static CreateEventSessionCustomPropertyDefinitionDto CreateSessionDto(Guid sessionId)
    {
        return new CreateEventSessionCustomPropertyDefinitionDto
        {
            EventSessionId = sessionId,
            Namespace = "Tenant Community",
            Key = "Prayer Notes",
            DisplayName = "Prayer Notes",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
        };
    }
}
