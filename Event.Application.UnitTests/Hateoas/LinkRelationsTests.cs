using Explore.Application.Hateoas;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Hateoas;

/// <summary>
/// Unit tests for LinkRelations constants.
/// Validates that standard IANA and custom link relations are correctly defined.
/// </summary>
public class LinkRelationsTests
{
    #region IANA Standard Link Relations (RFC 8288)

    [Test]
    public async Task Self_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Self).IsEqualTo("self");
    }

    [Test]
    public async Task Collection_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Collection).IsEqualTo("collection");
    }

    [Test]
    public async Task Item_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Item).IsEqualTo("item");
    }

    [Test]
    public async Task First_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.First).IsEqualTo("first");
    }

    [Test]
    public async Task Last_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Last).IsEqualTo("last");
    }

    [Test]
    public async Task Next_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Next).IsEqualTo("next");
    }

    [Test]
    public async Task Prev_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Prev).IsEqualTo("prev");
    }

    [Test]
    public async Task Edit_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Edit).IsEqualTo("edit");
    }

    [Test]
    public async Task Create_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Create).IsEqualTo("create");
    }

    [Test]
    public async Task Delete_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Delete).IsEqualTo("delete");
    }

    #endregion

    #region Custom Domain Link Relations

    [Test]
    public async Task Events_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Events).IsEqualTo("events");
    }

    [Test]
    public async Task Sessions_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Sessions).IsEqualTo("sessions");
    }

    [Test]
    public async Task Speakers_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Speakers).IsEqualTo("speakers");
    }

    [Test]
    public async Task Categories_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Categories).IsEqualTo("categories");
    }

    [Test]
    public async Task Tags_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Tags).IsEqualTo("tags");
    }

    [Test]
    public async Task Actor_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Actor).IsEqualTo("actor");
    }

    [Test]
    public async Task Event_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Event).IsEqualTo("event");
    }

    [Test]
    public async Task Location_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Location).IsEqualTo("location");
    }

    [Test]
    public async Task Parent_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Parent).IsEqualTo("parent");
    }

    [Test]
    public async Task Children_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Children).IsEqualTo("children");
    }

    [Test]
    public async Task Members_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Members).IsEqualTo("members");
    }

    [Test]
    public async Task Organization_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Organization).IsEqualTo("organization");
    }

    [Test]
    public async Task AgendaItems_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.AgendaItems).IsEqualTo("agenda-items");
    }

    [Test]
    public async Task Registration_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.Registration).IsEqualTo("registration");
    }

    [Test]
    public async Task PublishReadiness_ShouldBeCorrect()
    {
        await Assert.That(LinkRelations.PublishReadiness).IsEqualTo("publish-readiness");
    }

    #endregion

    #region All Relations Should Be Non-Empty

    [Test]
    public async Task AllRelations_ShouldBeNonEmpty()
    {
        // Get all public const string fields
        var fields = typeof(LinkRelations)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral);

        foreach (var field in fields)
        {
            var value = field.GetValue(null) as string;
            await Assert.That(value).IsNotNull();
            await Assert.That(value).IsNotEmpty();
        }
    }

    #endregion
}
