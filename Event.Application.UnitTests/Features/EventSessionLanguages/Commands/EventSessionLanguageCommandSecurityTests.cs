using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessionLanguages.Commands;

public class EventSessionLanguageCommandSecurityTests
{
    [Test]
    public async Task CreateCommand_ResourceAttributes_ShouldCarryTenantAndEventContext()
    {
        var eventSessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        ISecureRequest command = new CreateEventSessionLanguageCommand
        {
            TenantId = tenantId,
            EventId = eventId,
            EventSessionLanguageDto = new CreateEventSessionLanguageDto
            {
                EventSessionId = eventSessionId,
                LanguageId = 1
            }
        };

        await Assert.That(command.ResourceId).IsEqualTo(eventSessionId.ToString());
        await Assert.That(command.ResourceAttributes).IsNotNull();
        await Assert.That(command.ResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(command.ResourceAttributes["eventId"]).IsEqualTo(eventId.ToString());
    }

    [Test]
    public async Task DeleteCommand_ResourceId_ShouldUseParentSessionAndCarryTenantAndEventContext()
    {
        var eventSessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        ISecureRequest command = new DeleteEventSessionLanguageCommand
        {
            Id = 42,
            EventSessionId = eventSessionId,
            TenantId = tenantId,
            EventId = eventId
        };

        await Assert.That(command.ResourceId).IsEqualTo(eventSessionId.ToString());
        await Assert.That(command.ResourceAttributes).IsNotNull();
        await Assert.That(command.ResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(command.ResourceAttributes["eventId"]).IsEqualTo(eventId.ToString());
    }
}
