// ABOUTME: Quota regression tests for event template definition cardinality limits.
// ABOUTME: Verifies create and update handlers fail before governance, mapping, or persistence when templates exceed tenant quotas.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventTemplate;
using Explore.Application.Features.EventTemplates.Handlers.Commands;
using Explore.Application.Features.EventTemplates.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Definitions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventTemplates.Commands;

public class EventTemplateDefinitionQuotaTests
{
    [Test]
    public async Task CreateHandle_WhenDefinitionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IEventTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var handler = CreateCreateHandler(repository, quotaResolver, governancePolicy, tenantId);

        repository.ExistsTemplateKey(tenantId, "ramadan-program", 1).Returns(false);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await handler.Handle(
            new CreateEventTemplateCommand { TemplateDto = CreateTemplateDto(definitionCount: 2) },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_template_definitions");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.Errors!.Single()).Contains(FailureCodes.QuotaExceeded);
        governancePolicy.DidNotReceiveWithAnyArgs().EvaluateDefinition(default!, default!);
        await repository.DidNotReceiveWithAnyArgs().CreateWithDefinitions(default!, default!, default);
    }

    [Test]
    public async Task CreateHandle_WhenDefinitionOptionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var repository = Substitute.For<IEventTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var handler = CreateCreateHandler(repository, quotaResolver, governancePolicy, tenantId);

        repository.ExistsTemplateKey(tenantId, "ramadan-program", 1).Returns(false);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key, tenantId, Arg.Any<CancellationToken>()).Returns(5);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await handler.Handle(
            new CreateEventTemplateCommand { TemplateDto = CreateTemplateDtoWithOptionDefinition(optionCount: 2) },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_template_definition_options");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        governancePolicy.DidNotReceiveWithAnyArgs().EvaluateDefinition(default!, default!);
        await repository.DidNotReceiveWithAnyArgs().CreateWithDefinitions(default!, default!, default);
    }

    [Test]
    public async Task UpdateHandle_WhenDefinitionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var repository = Substitute.For<IEventTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var handler = CreateUpdateHandler(repository, quotaResolver, governancePolicy);

        repository.GetTrackedTemplateWithDefinitions(templateId, Arg.Any<CancellationToken>())
            .Returns(new EventTemplate
            {
                Id = templateId,
                TenantId = tenantId,
                TemplateKey = "ramadan-program",
                DisplayName = "Ramadan Program",
                Version = 1,
                IsActive = true,
            });
        repository.ExistsTemplateKey(tenantId, "ramadan-program", 1, templateId).Returns(false);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await handler.Handle(
            new UpdateEventTemplateCommand { TemplateDto = CreateUpdateTemplateDto(templateId, definitionCount: 2) },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_template_definitions");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.Errors!.Single()).Contains(FailureCodes.QuotaExceeded);
        governancePolicy.DidNotReceiveWithAnyArgs().EvaluateDefinition(default!, default!);
        await repository.DidNotReceiveWithAnyArgs().UpdateWithDefinitions(default!, default!, default);
    }

    [Test]
    public async Task UpdateHandle_WhenDefinitionOptionQuotaExceeded_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var repository = Substitute.For<IEventTemplateRepository>();
        var quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        var governancePolicy = Substitute.For<ICustomPropertyGovernancePolicy>();
        var handler = CreateUpdateHandler(repository, quotaResolver, governancePolicy);

        repository.GetTrackedTemplateWithDefinitions(templateId, Arg.Any<CancellationToken>())
            .Returns(new EventTemplate
            {
                Id = templateId,
                TenantId = tenantId,
                TemplateKey = "ramadan-program",
                DisplayName = "Ramadan Program",
                Version = 1,
                IsActive = true,
            });
        repository.ExistsTemplateKey(tenantId, "ramadan-program", 1, templateId).Returns(false);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxDefinitionsPerTemplate.Key, tenantId, Arg.Any<CancellationToken>()).Returns(5);
        quotaResolver.GetIntAsync(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key, tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await handler.Handle(
            new UpdateEventTemplateCommand { TemplateDto = CreateUpdateTemplateDtoWithOptionDefinition(templateId, optionCount: 2) },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.MaxOptionsPerDefinition.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(1);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(2);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_template_definition_options");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        governancePolicy.DidNotReceiveWithAnyArgs().EvaluateDefinition(default!, default!);
        await repository.DidNotReceiveWithAnyArgs().UpdateWithDefinitions(default!, default!, default);
    }

    private static CreateEventTemplateCommandHandler CreateCreateHandler(
        IEventTemplateRepository repository,
        ICustomPropertyQuotaResolver quotaResolver,
        ICustomPropertyGovernancePolicy governancePolicy,
        Guid tenantId)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);

        return new CreateEventTemplateCommandHandler(
            repository,
            governancePolicy,
            quotaResolver,
            tenantContext,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IMapper>(),
            Substitute.For<HybridCache>(),
            Substitute.For<IUnitOfWork>());
    }

    private static UpdateEventTemplateCommandHandler CreateUpdateHandler(
        IEventTemplateRepository repository,
        ICustomPropertyQuotaResolver quotaResolver,
        ICustomPropertyGovernancePolicy governancePolicy)
    {
        return new UpdateEventTemplateCommandHandler(
            repository,
            governancePolicy,
            quotaResolver,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IMapper>(),
            Substitute.For<HybridCache>(),
            Substitute.For<IUnitOfWork>());
    }

    private static CreateEventTemplateDto CreateTemplateDto(int definitionCount)
    {
        return new CreateEventTemplateDto
        {
            TemplateKey = "ramadan-program",
            DisplayName = "Ramadan Program",
            Version = 1,
            IsActive = true,
            Definitions = CreateDefinitionDtos(definitionCount),
        };
    }

    private static UpdateEventTemplateDto CreateUpdateTemplateDto(Guid templateId, int definitionCount)
    {
        return new UpdateEventTemplateDto
        {
            Id = templateId,
            TemplateKey = "ramadan-program",
            DisplayName = "Ramadan Program",
            Version = 1,
            IsActive = true,
            Definitions = CreateDefinitionDtos(definitionCount),
        };
    }

    private static CreateEventTemplateDto CreateTemplateDtoWithOptionDefinition(int optionCount)
    {
        return new CreateEventTemplateDto
        {
            TemplateKey = "ramadan-program",
            DisplayName = "Ramadan Program",
            Version = 1,
            IsActive = true,
            Definitions = [CreateOptionDefinitionDto(optionCount)],
        };
    }

    private static UpdateEventTemplateDto CreateUpdateTemplateDtoWithOptionDefinition(Guid templateId, int optionCount)
    {
        return new UpdateEventTemplateDto
        {
            Id = templateId,
            TemplateKey = "ramadan-program",
            DisplayName = "Ramadan Program",
            Version = 1,
            IsActive = true,
            Definitions = [CreateOptionDefinitionDto(optionCount)],
        };
    }

    private static List<CreateEventTemplateDefinitionDto> CreateDefinitionDtos(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new CreateEventTemplateDefinitionDto
            {
                Namespace = "tenant.community",
                Key = $"template_field_{index}",
                DisplayName = $"Template Field {index}",
                PropertyType = PropertyType.Text,
                ExposureLevel = ExposureLevel.OrganizerOnly,
                IsActive = true,
            })
            .ToList();
    }

    private static CreateEventTemplateDefinitionDto CreateOptionDefinitionDto(int optionCount)
    {
        return new CreateEventTemplateDefinitionDto
        {
            Namespace = "tenant.community",
            Key = "attendance_tier",
            DisplayName = "Attendance Tier",
            PropertyType = PropertyType.Option,
            ExposureLevel = ExposureLevel.OrganizerOnly,
            IsActive = true,
            Options = Enumerable.Range(1, optionCount)
                .Select(index => new CreateEventTemplateOptionDto
                {
                    Namespace = "tenant.community",
                    Key = $"tier_{index}",
                    DisplayName = $"Tier {index}",
                    Value = $"tier-{index}",
                    IsActive = true,
                    SortOrder = index,
                })
                .ToList(),
        };
    }
}
