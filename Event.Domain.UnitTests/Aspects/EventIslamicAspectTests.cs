namespace Event.Domain.UnitTests.Aspects;

using Explore.Domain;
using Explore.Domain.Interfaces;

public class EventIslamicAspectTests
{
    [Test]
    public async Task Constructor_DefaultValues_AreExpected()
    {
        var aspect = new EventIslamicAspect();

        await Assert.That(aspect.GenderMode).IsEqualTo(GenderSegregationMode.Mixed);
        await Assert.That(aspect.IncludesQuranRecitation).IsFalse();
        await Assert.That(aspect.MadhabId).IsNull();
        await Assert.That(aspect.ReferencePrayer).IsNull();
        await Assert.That(aspect.PrayerTimeOffset).IsNull();
        await Assert.That(aspect.PrimaryLanguageId).IsNull();
    }

    [Test]
    public async Task ReferencePrayer_AllValues_CanBeSet()
    {
        var values = new[]
        {
            PrayerTime.Fajr,
            PrayerTime.Sunrise,
            PrayerTime.Dhuhr,
            PrayerTime.Asr,
            PrayerTime.Maghrib,
            PrayerTime.Isha
        };

        var aspect = new EventIslamicAspect();

        foreach (var value in values)
        {
            aspect.ReferencePrayer = value;
            await Assert.That(aspect.ReferencePrayer).IsEqualTo(value);
        }
    }

    [Test]
    public async Task GenderMode_AllValues_CanBeSet()
    {
        var values = new[]
        {
            GenderSegregationMode.Mixed,
            GenderSegregationMode.MenOnly,
            GenderSegregationMode.WomenOnly,
            GenderSegregationMode.Segregated,
            GenderSegregationMode.Family
        };

        var aspect = new EventIslamicAspect();

        foreach (var value in values)
        {
            aspect.GenderMode = value;
            await Assert.That(aspect.GenderMode).IsEqualTo(value);
        }
    }

    [Test]
    public async Task NavigationProperties_DefaultValue_AreNull()
    {
        var aspect = new EventIslamicAspect();

        await Assert.That(aspect.Event).IsNull();
        await Assert.That(aspect.Madhab).IsNull();
        await Assert.That(aspect.PrimaryLanguage).IsNull();
    }

    [Test]
    public async Task NullableIdentifiers_WhenSet_AreReadBack()
    {
        var aspect = new EventIslamicAspect
        {
            MadhabId = 3,
            PrayerTimeOffset = -15,
            PrimaryLanguageId = 1
        };

        await Assert.That(aspect.MadhabId).IsEqualTo(3);
        await Assert.That(aspect.PrayerTimeOffset).IsEqualTo(-15);
        await Assert.That(aspect.PrimaryLanguageId).IsEqualTo(1);
    }
}
