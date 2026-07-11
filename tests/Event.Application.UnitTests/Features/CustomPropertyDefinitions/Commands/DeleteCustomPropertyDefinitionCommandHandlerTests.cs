// ABOUTME: Unit tests for shared custom-property definition deletion behavior.
// ABOUTME: Verifies missing-row handling and feature-specific delete delegation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.CustomPropertyDefinitions.Handlers.Commands;
using Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.CustomPropertyDefinitions.Commands;

public class DeleteCustomPropertyDefinitionCommandHandlerTests
{
    private readonly ICustomPropertyDefinitionRepository _customPropertyDefinitionRepository;
    private readonly HybridCache _cache;
    private readonly DeleteCustomPropertyDefinitionCommandHandler _handler;

    public DeleteCustomPropertyDefinitionCommandHandlerTests()
    {
        _customPropertyDefinitionRepository = Substitute.For<ICustomPropertyDefinitionRepository>();
        _cache = Substitute.For<HybridCache>();
        _handler = new DeleteCustomPropertyDefinitionCommandHandler(_customPropertyDefinitionRepository, _cache);
    }

    [Test]
    public async Task Handle_WhenDefinitionMissing_ReturnsFalse()
    {
        var command = new DeleteCustomPropertyDefinitionCommand { Id = Guid.NewGuid() };
        _customPropertyDefinitionRepository.GetDefinitionWithDetails(command.Id).Returns((CustomPropertyDefinition?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result).IsFalse();
        await _customPropertyDefinitionRepository.DidNotReceiveWithAnyArgs().DeleteDefinition(default, default);
    }

    [Test]
    public async Task Handle_WhenDefinitionExists_DeletesDefinition()
    {
        var definition = new CustomPropertyDefinition
        {
            Id = Guid.NewGuid(),
            Tenant = null,
            TenantId = Guid.NewGuid(),
            EntityTypeName = EntityTypeName.Group,
            Namespace = "tenant.community",
            Key = "prayer_notes",
            DisplayName = "Prayer Notes",
            PropertyType = PropertyType.Text,
            ExposureLevel = ExposureLevel.Internal,
        };
        var command = new DeleteCustomPropertyDefinitionCommand { Id = definition.Id };

        _customPropertyDefinitionRepository.GetDefinitionWithDetails(definition.Id).Returns(definition);
        _customPropertyDefinitionRepository.DeleteDefinition(definition.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result).IsTrue();
        await _customPropertyDefinitionRepository.Received(1).DeleteDefinition(definition.Id, Arg.Any<CancellationToken>());
    }
}
