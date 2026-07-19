// ABOUTME: Covers the terminal policy-version boundary before the aggregate increments its version.
// ABOUTME: Prevents a syntactically valid command from overflowing the contiguous audit sequence.

using Explore.Application.Features.EventLocations.Requests.Commands;
using Explore.Application.Features.EventLocations.Validators;
using Explore.Domain.Enums;

namespace ApplicationUnitTests.Features.EventLocations.Commands;

[Category("EventLocationPrivacy")]
public sealed class EventLocationPolicyVersionBoundaryTests
{
    [Test]
    public async Task MaximumExpectedPolicyVersionIsRejectedBeforeMutation()
    {
        var command = new UpdateEventLocationPolicyCommand
        {
            EventId = Guid.CreateVersion7(),
            EventLocationId = Guid.CreateVersion7(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            ExpectedPolicyVersion = int.MaxValue,
            SelectedFields = EventLocationDisclosureFields.None,
            FullDetailsAudience = LocationDisclosureAudienceEnum.Never
        };

        var result = await new UpdateEventLocationPolicyCommandValidator()
            .ValidateAsync(command, CancellationToken.None);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.PropertyName))
            .Contains(nameof(command.ExpectedPolicyVersion));
    }
}
