// ABOUTME: Unit tests for registration AutoMapper profile display-field projections.
// ABOUTME: Protects My Registrations event card data from losing event/session fields.

using AutoMapper;
using Explore.Application.Profiles;
using Explore.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace Event.Application.UnitTests.Profiles;

public sealed class RegistrationMappingProfileTests
{
    private readonly IMapper _mapper;

    public RegistrationMappingProfileTests()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<RegistrationMappingProfile>(),
            NullLoggerFactory.Instance);
        _mapper = configuration.CreateMapper();
    }

    [Test]
    public async Task EventRegistrationListMapping_ProjectsEventDisplayFields()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionStart = new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
        var registration = new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            UserId = Guid.NewGuid(),
            User = null!,
            EventSessionId = Guid.NewGuid(),
            EventSession = new EventSession
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Event = new Explore.Domain.Event
                {
                    Id = eventId,
                    Title = "Annual Conference",
                    Actor = null!,
                    Tenant = null!,
                    VisibilityType = null!,
                    EventStatus = null!,
                    EventFormat = null!,
                    FeaturedImage = new StorageObject
                    {
                        Id = Guid.NewGuid(),
                        FileType = null!,
                        Uri = "https://example.test/event.png",
                        Provider = "local",
                        FullName = "event.png",
                        SafeDisplayName = "event.png",
                        Extension = ".png",
                        Visibility = "public",
                        Purpose = "event_featured_image",
                        LifecycleState = "active",
                        Tenant = null!
                    }
                },
                Tenant = null!,
                StartTime = sessionStart
            },
            ApprovalStatus = new ApprovalStatus
            {
                Id = 2,
                FullName = "Approved",
                MasterCode = "APPROVED"
            },
            TenantId = tenantId,
            Tenant = null!
        };

        var dto = _mapper.Map<Explore.Application.DTOs.EventRegistration.EventRegistrationListDto>(registration);

        await Assert.That(dto.EventId).IsEqualTo(eventId);
        await Assert.That(dto.EventTitle).IsEqualTo("Annual Conference");
        await Assert.That(dto.EventFeaturedImageUri).IsEqualTo("https://example.test/event.png");
        await Assert.That(dto.EventStartTime).IsEqualTo(sessionStart);
        await Assert.That(dto.ApprovalStatusFullName).IsEqualTo("Approved");
        await Assert.That(dto.ApprovalStatusMasterCode).IsEqualTo("APPROVED");
    }
}
