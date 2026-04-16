using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.RegistrationScope;
using Explore.Application.Features.RegistrationScopes.Handlers.Queries;
using Explore.Application.Features.RegistrationScopes.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.RegistrationScopes.Queries;

public class GetRegistrationScopeListRequestHandlerTests
{
    private readonly IRegistrationScopeRepository _registrationScopeRepository;
    private readonly IMapper _mapper;
    private readonly GetRegistrationScopeListRequestHandler _handler;

    public GetRegistrationScopeListRequestHandlerTests()
    {
        _registrationScopeRepository = Substitute.For<IRegistrationScopeRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetRegistrationScopeListRequestHandler(_registrationScopeRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingScopes_ReturnsMappedList()
    {
        // Arrange
        var request = new GetRegistrationScopeListRequest();

        var scopes = new List<RegistrationScope>
        {
            new() { Id = 1, MasterCode = "EVENT", FullName = "Event" },
            new() { Id = 2, MasterCode = "DAY", FullName = "Day" },
            new() { Id = 3, MasterCode = "SESSION_SELECTION", FullName = "Session Selection" }
        };

        var expectedDtos = new List<RegistrationScopeListDto>
        {
            new() { Id = 1, MasterCode = "EVENT", FullName = "Event" },
            new() { Id = 2, MasterCode = "DAY", FullName = "Day" },
            new() { Id = 3, MasterCode = "SESSION_SELECTION", FullName = "Session Selection" }
        };

        _registrationScopeRepository.GetAll().Returns(scopes);
        _mapper.Map<List<RegistrationScopeListDto>>(scopes).Returns(expectedDtos);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Handle_WithNoScopes_ReturnsEmptyList()
    {
        // Arrange
        var request = new GetRegistrationScopeListRequest();

        _registrationScopeRepository.GetAll().Returns(new List<RegistrationScope>());
        _mapper.Map<List<RegistrationScopeListDto>>(Arg.Any<List<RegistrationScope>>())
            .Returns(new List<RegistrationScopeListDto>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }
}
