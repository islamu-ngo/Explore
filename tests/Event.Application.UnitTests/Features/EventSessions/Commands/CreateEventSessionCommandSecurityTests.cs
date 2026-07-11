using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Commands;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessions.Commands;

public class CreateEventSessionCommandSecurityTests
{
    [Test]
    public async Task ResourceAttributes_ShouldCarryTenantAndEventContext()
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
        await Assert.That(command.ResourceAttributes).IsNotNull();
        await Assert.That(command.ResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(command.ResourceAttributes["eventId"]).IsEqualTo(eventId.ToString());
    }
}
