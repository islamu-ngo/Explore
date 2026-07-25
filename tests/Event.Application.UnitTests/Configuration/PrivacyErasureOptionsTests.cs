// ABOUTME: Verifies the configured authority-retention duration remains the backup horizon plus safety margin.
// ABOUTME: Guards the finite authority append adapters from deriving an independent retention policy.

using Explore.Application.Configuration;

namespace Event.Application.UnitTests.Configuration;

public sealed class PrivacyErasureOptionsTests
{
    [Test]
    public async Task AuthorityRetention_IsDerivedFromConfiguredHorizonAndSafetyMargin()
    {
        var options = new PrivacyErasureOptions
        {
            MaximumBackupHorizon = TimeSpan.FromDays(14),
            AuthorityRetentionSafetyMargin = TimeSpan.FromHours(12),
        };

        options.Validate();

        await Assert.That(options.AuthorityRetention).IsEqualTo(TimeSpan.FromDays(14.5));
    }
}
