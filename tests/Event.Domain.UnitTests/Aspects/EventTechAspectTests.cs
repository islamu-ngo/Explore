namespace Event.Domain.UnitTests.Aspects;

using Explore.Domain;
using Explore.Domain.Interfaces;

public class EventTechAspectTests
{
    [Test]
    public async Task Constructor_DefaultValues_AreExpected()
    {
        var aspect = new EventTechAspect();

        await Assert.That(aspect.SkillLevel).IsEqualTo(SkillLevel.AllLevels);
        await Assert.That(aspect.RequiresLaptop).IsFalse();
        await Assert.That(aspect.IsCodingCompetition).IsFalse();
        await Assert.That(aspect.Event).IsNull();
        await Assert.That(aspect.GithubRepoUrl).IsNull();
        await Assert.That(aspect.HackathonTrack).IsNull();
        await Assert.That(aspect.TechStackTags).IsNull();
        await Assert.That(aspect.MaxTeamSize).IsNull();
        await Assert.That(aspect.PrizePool).IsNull();
        await Assert.That(aspect.PrizeCurrencyCode).IsNull();
    }

    [Test]
    public async Task SkillLevel_AllValues_CanBeSet()
    {
        var values = new[]
        {
            SkillLevel.AllLevels,
            SkillLevel.Beginner,
            SkillLevel.Intermediate,
            SkillLevel.Advanced
        };

        var aspect = new EventTechAspect();

        foreach (var value in values)
        {
            aspect.SkillLevel = value;
            await Assert.That(aspect.SkillLevel).IsEqualTo(value);
        }
    }

    [Test]
    public async Task StringAndDecimalProperties_WhenSet_AreReadBack()
    {
        var aspect = new EventTechAspect
        {
            GithubRepoUrl = "https://github.com/islamu-ngo/Explore",
            HackathonTrack = "AI",
            TechStackTags = ".NET,Blazor,PostgreSQL",
            PrizePool = 5000.75m,
            PrizeCurrencyCode = "USD"
        };

        await Assert.That(aspect.GithubRepoUrl).IsEqualTo("https://github.com/islamu-ngo/Explore");
        await Assert.That(aspect.HackathonTrack).IsEqualTo("AI");
        await Assert.That(aspect.TechStackTags).IsEqualTo(".NET,Blazor,PostgreSQL");
        await Assert.That(aspect.PrizePool).IsEqualTo(5000.75m);
        await Assert.That(aspect.PrizeCurrencyCode).IsEqualTo("USD");
    }

    [Test]
    public async Task CompetitionFields_WhenSet_AreReadBack()
    {
        var aspect = new EventTechAspect
        {
            RequiresLaptop = true,
            IsCodingCompetition = true,
            MaxTeamSize = 4
        };

        await Assert.That(aspect.RequiresLaptop).IsTrue();
        await Assert.That(aspect.IsCodingCompetition).IsTrue();
        await Assert.That(aspect.MaxTeamSize).IsEqualTo(4);
    }
}
