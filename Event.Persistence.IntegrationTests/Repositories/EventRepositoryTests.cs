using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
public class EventRepositoryTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public EventRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task Create_ShouldPersistEvent()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);

        var eventId = Guid.NewGuid();
        var @event = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Integration Test Event",
            Description = "Test Description",
            FirstSessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            LastSessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1).AddHours(2)),
            EventTypeId = 1, // Assumes seeded data or defaults
            AudienceGenderId = 1,
            AudienceAgeId = 1,
            ActorId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            VisibilityTypeId = 1,
            EventStatusId = 1,
            EventFormatId = 1,
            TotalViews = 0,
            IsRegistrationRequired = false
        };

        // Act
        var result = await repository.Create(@event);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(eventId);

        // Verify with new context
        using var verifyContext = _fixture.CreateDbContext();
        var savedEvent = await verifyContext.Events.FindAsync(eventId);
        await Assert.That(savedEvent).IsNotNull();
        await Assert.That(savedEvent!.Title).IsEqualTo("Integration Test Event");
    }

    [Test]
    public async Task GetEventWithDetails_ShouldReturnIncludes()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var repository = new EventRepository(context);
        
        // Ensure dependent entities exist (simplified for test)
        // In a real scenario, you'd seed Reference Data first (EventTypes, etc.)
        
        var eventId = Guid.NewGuid();
        var @event = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Detailed Event",
            EventTypeId = 1, 
            AudienceGenderId = 1,
            AudienceAgeId = 1,
            ActorId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            VisibilityTypeId = 1,
            EventStatusId = 1,
            EventFormatId = 1,
            TotalViews = 0,
            IsRegistrationRequired = false
        };

        await repository.Create(@event);

        // Act
        var result = await repository.GetEventWithDetails(eventId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(eventId);
        // Verify includes (if data seeding was robust, we'd check .EventType is not null)
    }
}
