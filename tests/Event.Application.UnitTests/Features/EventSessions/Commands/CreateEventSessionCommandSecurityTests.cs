using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Commands;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessions.Commands;

public class CreateEventSessionCommandSecurityTests
{
    [Test]
    public async Task AuthorizationFacts_ShouldCarryPreCreateTenantAndEventContext()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        ISecureRequest command = new CreateEventSessionCommand
        {
            TenantId = tenantId,
            EventSessionDto = new CreateEventSessionDto
            {
                EventId = eventId
            }
        };

        await Assert.That(command.ResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(command.AuthorizationFacts)
            .IsEqualTo(new PreCreateAuthorizationFacts(tenantId, eventId));
    }
}
