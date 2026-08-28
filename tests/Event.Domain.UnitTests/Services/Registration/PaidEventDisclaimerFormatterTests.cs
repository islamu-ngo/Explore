// ABOUTME: Specifies the canonical white-label paid-event directory disclaimer.
// ABOUTME: Proves branding is normalized without changing the legal statement.

using Explore.Domain.Services.Registration;

namespace Event.Domain.UnitTests.Services.Registration;

public sealed class PaidEventDisclaimerFormatterTests
{
    [Test]
    public async Task Format_UsesNormalizedTenantBrandInCanonicalDisclaimer()
    {
        string disclaimer = PaidEventDisclaimerFormatter.Format("  Community Events  ");

        await Assert.That(disclaimer).IsEqualTo(
            "Community Events provides an event discovery and management directory only. " +
            "Community Events does not process ticket sales or act as event organizer. " +
            "Any financial transaction or contract is strictly between the attendee and the external organizer.");
    }

    [Test]
    public async Task Format_WhenBrandIsMissing_UsesWhiteLabelDefault()
    {
        string disclaimer = PaidEventDisclaimerFormatter.Format(" ");

        await Assert.That(disclaimer).StartsWith("ISLAMU provides an event discovery and management directory only.");
    }
}
