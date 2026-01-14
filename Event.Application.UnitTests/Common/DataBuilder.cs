using Bogus;
using Explore.Domain;

namespace Event.Application.UnitTests.Common;

public static class DataBuilder
{
    public static Faker<Explore.Domain.Event> Event => new Faker<Explore.Domain.Event>()
        .RuleFor(e => e.Id, f => Guid.NewGuid())
        .RuleFor(e => e.Title, f => f.Lorem.Sentence())
        .RuleFor(e => e.Description, f => f.Lorem.Paragraph())
        .RuleFor(e => e.FirstSessionDate, f => f.Date.FutureDateOnly())
        .RuleFor(e => e.LastSessionDate, (f, e) => e.FirstSessionDate.Value.AddDays(1))
        .RuleFor(e => e.IsRegistrationRequired, f => f.Random.Bool());

    public static Faker<User> User => new Faker<User>()
        .RuleFor(u => u.Id, f => Guid.NewGuid())
        .RuleFor(u => u.Email, f => f.Internet.Email())
        .RuleFor(u => u.FirstName, f => f.Name.FirstName())
        .RuleFor(u => u.LastName, f => f.Name.LastName());

    // Add more entity builders as needed
}
