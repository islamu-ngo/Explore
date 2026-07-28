// ABOUTME: Quota regression tests for event-session template definition and option limits.
// ABOUTME: Verifies session-template handlers fail before governance/mapping/persistence when quotas are exceeded.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionTemplate;
using Explore.Application.Features.EventSessionTemplates.Handlers.Commands;
using Explore.Application.Features.EventSessionTemplates.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionTemplates.Commands;

public class EventSessionTemplateDefinitionQuotaTests
{
    [Test]
    public async Task CreateHandle_WhenDefinitionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var eventTemplateId = Guid.NewGuid();
        var repository = Substitute.For<IEventSessionTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var handler = CreateCreateHandler(repository, quotaResolver, governancePolicy, tenantId);

        repository.ExistsSessionTemplateKey(eventTemplateId, "session-track", 1).Returns(false);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await handler.Handle(
            new CreateEventSessionTemplateCommand { SessionTemplateDto = CreateSessionTemplateDto(eventTemplateId, definitionCount: 2) },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_session_template_definitions");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        governancePolicy.DidNotReceiveWithAnyArgs().EvaluateDefinition(default!, default!);
        await repository.DidNotReceiveWithAnyArgs().CreateWithDefinitions(default!, default!, default);
    }

    [Test]
    public async Task CreateHandle_WhenOptionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var eventTemplateId = Guid.NewGuid();
        var repository = Substitute.For<IEventSessionTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var handler = CreateCreateHandler(repository, quotaResolver, governancePolicy, tenantId);

        repository.ExistsSessionTemplateKey(eventTemplateId, "session-track", 1).Returns(false);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key, tenantId, Arg.Any<CancellationToken>()).Returns(5);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await handler.Handle(
            new CreateEventSessionTemplateCommand { SessionTemplateDto = CreateSessionTemplateDtoWithOptionDefinition(eventTemplateId, optionCount: 2) },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_session_template_definition_options");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        governancePolicy.DidNotReceiveWithAnyArgs().EvaluateDefinition(default!, default!);
        await repository.DidNotReceiveWithAnyArgs().CreateWithDefinitions(default!, default!, default);
    }

    [Test]
    public async Task UpdateHandle_WhenDefinitionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var eventTemplateId = Guid.NewGuid();
        var sessionTemplateId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var repository = Substitute.For<IEventSessionTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var handler = CreateUpdateHandler(repository, quotaResolver, governancePolicy);

        repository.GetTrackedSessionTemplateWithDefinitions(sessionTemplateId, Arg.Any<CancellationToken>())
            .Returns(CreateSessionTemplate(sessionTemplateId, eventTemplateId, tenantId, concurrencyStamp));
        repository.ExistsSessionTemplateKey(eventTemplateId, "session-track", 1, sessionTemplateId).Returns(false);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await handler.Handle(
            new UpdateEventSessionTemplateCommand
            {
                SessionTemplateId = sessionTemplateId,
                TenantId = tenantId,
                ExpectedConcurrencyStamp = concurrencyStamp,
                SessionTemplateDto = CreateUpdateSessionTemplateDto(definitionCount: 2)
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_session_template_definitions");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        governancePolicy.DidNotReceiveWithAnyArgs().EvaluateDefinition(default!, default!);
        await repository.DidNotReceiveWithAnyArgs().UpdateWithDefinitions(default!, default!, default);
    }

    [Test]
    public async Task UpdateHandle_WhenOptionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var eventTemplateId = Guid.NewGuid();
        var sessionTemplateId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var repository = Substitute.For<IEventSessionTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var handler = CreateUpdateHandler(repository, quotaResolver, governancePolicy);

        repository.GetTrackedSessionTemplateWithDefinitions(sessionTemplateId, Arg.Any<CancellationToken>())
            .Returns(CreateSessionTemplate(sessionTemplateId, eventTemplateId, tenantId, concurrencyStamp));
        repository.ExistsSessionTemplateKey(eventTemplateId, "session-track", 1, sessionTemplateId).Returns(false);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key, tenantId, Arg.Any<CancellationToken>()).Returns(5);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await handler.Handle(
            new UpdateEventSessionTemplateCommand
            {
                SessionTemplateId = sessionTemplateId,
                TenantId = tenantId,
                ExpectedConcurrencyStamp = concurrencyStamp,
                SessionTemplateDto = CreateUpdateSessionTemplateDtoWithOptionDefinition(optionCount: 2)
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_session_template_definition_options");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        governancePolicy.DidNotReceiveWithAnyArgs().EvaluateDefinition(default!, default!);
        await repository.DidNotReceiveWithAnyArgs().UpdateWithDefinitions(default!, default!, default);
    }

    [Test]
    public async Task UpdateHandle_WhenDefinitionsOmitted_UpdatesMetadataWithoutReplacingDefinitions()
    {
        var tenantId = Guid.NewGuid();
        var eventTemplateId = Guid.NewGuid();
        var sessionTemplateId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var repository = Substitute.For<IEventSessionTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        var handler = CreateUpdateHandler(repository, quotaResolver, Substitute.For<ICustomPropertyGovernancePolicy>(), unitOfWork);
        var template = CreateSessionTemplate(sessionTemplateId, eventTemplateId, tenantId, concurrencyStamp);
        repository.GetTrackedSessionTemplateWithDefinitions(sessionTemplateId, Arg.Any<CancellationToken>()).Returns(template);
        repository.ExistsSessionTemplateKey(eventTemplateId, template.SessionTemplateKey, template.Version, sessionTemplateId).Returns(false);

        var result = await handler.Handle(
            new UpdateEventSessionTemplateCommand
            {
                SessionTemplateId = sessionTemplateId,
                TenantId = tenantId,
                ExpectedConcurrencyStamp = concurrencyStamp,
                SessionTemplateDto = new UpdateEventSessionTemplateDto
                {
                    Metadata = new UpdateEventSessionTemplateMetadataDto { DisplayName = "Updated Session" }
                }
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(template.DisplayName).IsEqualTo("Updated Session");
        await repository.Received(1).Update(template);
        await repository.DidNotReceiveWithAnyArgs().UpdateWithDefinitions(default!, default!, default);
    }

    private static CreateEventSessionTemplateCommandHandler CreateCreateHandler(
        IEventSessionTemplateRepository repository,
        ICustomPropertyQuotaResolver quotaResolver,
        ICustomPropertyGovernancePolicy governancePolicy,
        Guid tenantId)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);

        return new CreateEventSessionTemplateCommandHandler(
            repository,
            governancePolicy,
            quotaResolver,
            tenantContext,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IMapper>(),
            Substitute.For<HybridCache>(),
            Substitute.For<IUnitOfWork>());
    }

    private static UpdateEventSessionTemplateCommandHandler CreateUpdateHandler(
        IEventSessionTemplateRepository repository,
        ICustomPropertyQuotaResolver quotaResolver,
        ICustomPropertyGovernancePolicy governancePolicy,
        IUnitOfWork? unitOfWork = null)
    {
        return new UpdateEventSessionTemplateCommandHandler(
            repository,
            governancePolicy,
            quotaResolver,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IMapper>(),
            Substitute.For<HybridCache>(),
            unitOfWork ?? Substitute.For<IUnitOfWork>());
    }

    private static EventSessionTemplate CreateSessionTemplate(Guid sessionTemplateId, Guid eventTemplateId, Guid tenantId, Guid concurrencyStamp)
    {
        return new EventSessionTemplate
        {
            Id = sessionTemplateId,
            ConcurrencyStamp = concurrencyStamp,
            EventTemplateId = eventTemplateId,
            TenantId = tenantId,
            SessionTemplateKey = "session-track",
            DisplayName = "Session Track",
            Version = 1,
            IsActive = true,
        };
    }

    private static CreateEventSessionTemplateDto CreateSessionTemplateDto(Guid eventTemplateId, int definitionCount)
    {
        return new CreateEventSessionTemplateDto
        {
            EventTemplateId = eventTemplateId,
            SessionTemplateKey = "session-track",
            DisplayName = "Session Track",
            Version = 1,
            IsActive = true,
            Definitions = CreateDefinitionDtos(definitionCount),
        };
    }

    private static UpdateEventSessionTemplateDto CreateUpdateSessionTemplateDto(int definitionCount)
    {
        return new UpdateEventSessionTemplateDto
        {
            Definitions = new UpdateEventSessionTemplateDefinitionsDto
            {
                Items = CreateDefinitionDtos(definitionCount)
            }
        };
    }

    private static CreateEventSessionTemplateDto CreateSessionTemplateDtoWithOptionDefinition(Guid eventTemplateId, int optionCount)
    {
        return new CreateEventSessionTemplateDto
        {
            EventTemplateId = eventTemplateId,
            SessionTemplateKey = "session-track",
            DisplayName = "Session Track",
            Version = 1,
            IsActive = true,
            Definitions = [CreateOptionDefinitionDto(optionCount)],
        };
    }

    private static UpdateEventSessionTemplateDto CreateUpdateSessionTemplateDtoWithOptionDefinition(int optionCount)
    {
        return new UpdateEventSessionTemplateDto
        {
            Definitions = new UpdateEventSessionTemplateDefinitionsDto
            {
                Items = [CreateOptionDefinitionDto(optionCount)]
            }
        };
    }

    private static List<CreateEventSessionTemplateDefinitionDto> CreateDefinitionDtos(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new CreateEventSessionTemplateDefinitionDto
            {
                Namespace = "tenant.community",
                Key = $"session_field_{index}",
                DisplayName = $"Session Field {index}",
                PropertyType = PropertyType.Text,
                ExposureLevel = ExposureLevel.OrganizerOnly,
                IsActive = true,
            })
            .ToList();
    }

    private static CreateEventSessionTemplateDefinitionDto CreateOptionDefinitionDto(int optionCount)
    {
        return new CreateEventSessionTemplateDefinitionDto
        {
            Namespace = "tenant.community",
            Key = "delivery_mode",
            DisplayName = "Delivery Mode",
            PropertyType = PropertyType.Option,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
            Options = Enumerable.Range(1, optionCount)
                .Select(index => new CreateEventSessionTemplateOptionDto
                {
                    Namespace = "tenant.community",
                    Key = $"mode_{index}",
                    DisplayName = $"Mode {index}",
                    Value = $"mode-{index}",
                    IsActive = true,
                    SortOrder = index,
                })
                .ToList(),
        };
    }
}
