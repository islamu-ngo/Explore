// ABOUTME: Verifies an event can expose zero actions but never multiple primary participation actions.
// ABOUTME: Keeps public call-to-action ordering deterministic before persistence constraints run.

using Explore.Domain;
using Explore.Domain.Services.Registration;

namespace Event.Domain.UnitTests.Services.Registration;

public sealed class EventPublicActionRulesTests
{
    [Test]
    public async Task EnsureValid_ZeroActions_IsAllowed()
    {
        EventPublicActionRules.EnsureValid([]);

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task EnsureValid_TwoPrimaryActions_Throws()
    {
        EventPublicAction[] actions =
        [
            new() { IsPrimary = true },
            new() { IsPrimary = true }
        ];

        await Assert.That(() => EventPublicActionRules.EnsureValid(actions)).Throws<InvalidOperationException>();
    }
}
