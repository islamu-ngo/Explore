// ABOUTME: Contract tests for event registration read DTO privacy.
// ABOUTME: Verifies generic registration responses do not serialize user identity fields.

using System.Text.Json;
using Explore.Application.DTOs.EventRegistration;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventRegistrations.Queries;

public class EventRegistrationReadDtoPrivacyTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task EventRegistrationDto_DoesNotSerializeUserIdentityFields()
    {
        var dto = new EventRegistrationDto
        {
            Id = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            EventSessionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        await Assert.That(json).DoesNotContain("userId");
        await Assert.That(json).DoesNotContain("userFullName");
        await Assert.That(json).DoesNotContain("userEmail");
    }

    [Test]
    public async Task EventRegistrationListDto_DoesNotSerializeUserIdentityFields()
    {
        var dto = new EventRegistrationListDto
        {
            Id = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            EventSessionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        await Assert.That(json).DoesNotContain("userId");
        await Assert.That(json).DoesNotContain("userFullName");
    }
}
