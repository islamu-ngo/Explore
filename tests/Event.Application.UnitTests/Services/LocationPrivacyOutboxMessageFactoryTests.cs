// ABOUTME: Unit tests for PII-free location-erasure and external-correction outbox payloads.
// ABOUTME: Proves UUIDv7 message identity and excludes identifying venue, address, and room values.

using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using TUnit.Core;

namespace Event.Application.UnitTests.Services;

[Category("EventLocationPrivacy")]
public sealed class LocationPrivacyOutboxMessageFactoryTests
{
    private static readonly DateTime Now =
        new(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task CreateLocationErased_ContainsOnlyOpaqueIdentityAndVersionFacts()
    {
        Location home = CreateHome();
        LocationPrivacyErasureAuthorityIntent intent = CreateIntent(home.Id);

        OutboxMessage message = LocationPrivacyOutboxMessageFactory.CreateLocationErased(
            Guid.CreateVersion7(),
            intent,
            home,
            Now);

        await Assert.That(message.EventType)
            .IsEqualTo(LocationPrivacyOutboxMessageFactory.LocationPiiErasedEventType);
        await Assert.That(message.Id.Version).IsEqualTo(7);
        await Assert.That(message.Payload).Contains(intent.IntentId.ToString());
        await Assert.That(message.Payload).Contains(home.Id.ToString());
        await Assert.That(message.Payload).DoesNotContain("PRIVATE-HOME-NAME-CANARY");
        await Assert.That(message.Payload).DoesNotContain("ADDRESS-CANARY");
        await Assert.That(message.Payload).DoesNotContain("ROOM-CANARY");
    }

    [Test]
    public async Task CreateCorrectionRequested_ContainsOnlyOpaqueIdsAndPolicyVersion()
    {
        Location home = CreateHome();
        LocationPrivacyErasureAuthorityIntent intent = CreateIntent(home.Id);
        EventLocation eventLocation = EventLocation.CreatePhysical(
            home.TenantId,
            Guid.CreateVersion7(),
            home.Id,
            intent.OwnerUserId,
            Now);

        OutboxMessage message = LocationPrivacyOutboxMessageFactory.CreateCorrectionRequested(
            Guid.CreateVersion7(),
            intent,
            eventLocation,
            Now);

        await Assert.That(message.EventType)
            .IsEqualTo(LocationPrivacyOutboxMessageFactory.LocationPrivacyCorrectionRequestedEventType);
        await Assert.That(message.Payload).Contains(eventLocation.Id.ToString());
        await Assert.That(message.Payload).Contains("\"PolicyVersion\":1");
        await Assert.That(message.Payload).DoesNotContain("PRIVATE-HOME-NAME-CANARY");
        await Assert.That(message.Payload).DoesNotContain("ADDRESS-CANARY");
        await Assert.That(message.Payload).DoesNotContain("ROOM-CANARY");
    }

    private static LocationPrivacyErasureAuthorityIntent CreateIntent(Guid locationId) =>
        LocationPrivacyErasureAuthorityIntent.Record(
            Guid.CreateVersion7(),
            1,
            Guid.CreateVersion7(),
            [locationId],
            LocationPrivacyErasureReasonEnum.AccountDeletion,
            Now,
            Now);

    private static Location CreateHome()
    {
        var home = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            Tenant = null!,
            FullName = "PRIVATE-HOME-NAME-CANARY",
            Country = "BE",
            City = "Brussels",
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        home.ClassifyAsPrivateHome(Guid.CreateVersion7());
        home.AttachPii(new LocationPii
        {
            LocationId = home.Id,
            Address = "ADDRESS-CANARY",
            Postcode = "1000",
        });
        home.Rooms =
        [
            new LocationRoom
            {
                Id = Guid.CreateVersion7(),
                TenantId = home.TenantId,
                Tenant = null!,
                LocationId = home.Id,
                Location = home,
                Name = "ROOM-CANARY",
                ConcurrencyStamp = Guid.CreateVersion7(),
            }
        ];
        return home;
    }
}
