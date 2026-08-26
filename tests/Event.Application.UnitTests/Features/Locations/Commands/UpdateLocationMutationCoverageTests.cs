// ABOUTME: Focused mutation coverage for grouped Location update behavior and address-bundle semantics.
// ABOUTME: Pins policy calls, cancellation, provenance revocation, nullable bundle inputs, and field application.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Geocoding;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Geocoding;
using Explore.Application.Features.Locations.Handlers.Commands;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Locations.Commands;

public sealed class UpdateLocationMutationCoverageTests
{
    private static readonly Guid TenantId = Guid.Parse("019b0000-0031-7000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("019b0000-0031-7000-8000-000000000002");
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Handle_WhenLocationIsMissing_ReturnsNotFoundFailure()
    {
        TestContext context = CreateContext();

        BaseCommandResponse<Guid> result = await context.Handler.Handle(new UpdateLocationCommand
        {
            LocationId = Guid.Parse("019b0000-0031-7000-8000-000000000099"),
            ExpectedConcurrencyStamp = Guid.Parse("019b0000-0031-7000-8000-000000000098"),
            UpdateLocationDto = new UpdateLocationDto
            {
                FullName = new UpdateLocationFullNameDto { Value = "Missing venue" }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.NotFound);
        await context.Locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenOnlyAddressChanges_ResolvesPolicyAndPreservesPostcode()
    {
        TestContext context = CreateContext();
        Location location = CreateManualLocation();
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        BaseCommandResponse<Guid> result = await context.Handler.Handle(
            Command(location, address: "Changed address"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.Address).IsEqualTo("Changed address");
        await Assert.That(location.Postcode).IsEqualTo("1000");
        await AssertPolicyRequest(context, CancellationToken.None);
        await context.Locations.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenOnlyPostcodeChanges_ResolvesPolicyAndPreservesAddress()
    {
        TestContext context = CreateContext();
        Location location = CreateManualLocation();
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        BaseCommandResponse<Guid> result = await context.Handler.Handle(
            Command(location, postcode: "2000"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.Address).IsEqualTo("Existing address");
        await Assert.That(location.Postcode).IsEqualTo("2000");
        await AssertPolicyRequest(context, CancellationToken.None);
        await context.Locations.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenOnlyProviderCoordinatesAreStale_RevokesApprovalAndProviderProvenance()
    {
        TestContext context = CreateContext();
        Location location = CreateProviderLocation();
        Guid staleOrganizationId = Guid.Parse("019b0000-0031-7000-8000-000000000004");
        location.ApplyAddressGovernance(
            Guid.Parse("019b0000-0031-7000-8000-000000000005"),
            LocationAddressSourceEnum.ProviderSelection,
            LocationAddressVisibilityEnum.TenantApproved,
            staleOrganizationId);
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        BaseCommandResponse<Guid> result = await context.Handler.Handle(
            Command(location, address: "Existing address", postcode: "1000"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.GetCoordinate()).IsNull();
        await Assert.That(location.AddressSource).IsEqualTo(LocationAddressSourceEnum.Manual);
        await Assert.That(location.AddressVisibility).IsEqualTo(LocationAddressVisibilityEnum.CreatorPrivate);
        await Assert.That(location.AddressOrganizationId).IsNull();
        await Assert.That(location.UpdatedBy).IsEqualTo(UserId);
        await Assert.That(location.UpdatedAt).IsEqualTo(Now.UtcDateTime);
        await AssertPolicyRequest(context, CancellationToken.None);
        await context.Locations.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenAddressAndPostcodeAreExactWithoutCoordinates_SkipsPolicyAndPersistence()
    {
        TestContext context = CreateContext();
        Location location = CreateManualLocation();
        Guid originalStamp = location.ConcurrencyStamp;
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        BaseCommandResponse<Guid> result = await context.Handler.Handle(
            Command(location, address: "Existing address", postcode: "1000"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.ConcurrencyStamp).IsEqualTo(originalStamp);
        await context.Governance.DidNotReceive().ResolveAsync(
            Arg.Any<AddressGovernancePolicyRequest>(),
            Arg.Any<CancellationToken>());
        await context.Locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPolicyResolverFails_ReturnsFailureWithoutMutation()
    {
        TestContext context = CreateContext();
        Location location = CreateProviderLocation();
        Guid originalStamp = location.ConcurrencyStamp;
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        context.Governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AddressGovernancePolicyDecision>(
                new InvalidOperationException("Synthetic policy failure.")));

        BaseCommandResponse<Guid> result = await context.Handler.Handle(
            Command(location, address: "Changed address"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(location.Address).IsEqualTo("Existing address");
        await Assert.That(location.GetCoordinate()).IsNotNull();
        await Assert.That(location.ConcurrencyStamp).IsEqualTo(originalStamp);
        await context.Locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPolicyResolverCancels_PropagatesWithoutMutation()
    {
        TestContext context = CreateContext();
        Location location = CreateProviderLocation();
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        using var cancellation = new CancellationTokenSource();
        context.Governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<AddressGovernancePolicyDecision>(cancellation.Token);
            });

        await Assert.That(async () => await context.Handler.Handle(
            Command(location, address: "Changed address"),
            cancellation.Token)).Throws<OperationCanceledException>();
        await Assert.That(location.Address).IsEqualTo("Existing address");
        await Assert.That(location.GetCoordinate()).IsNotNull();
        await context.Locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenNonAddressFieldChanges_DoesNotTreatOmittedBundleAsAddressChange()
    {
        TestContext context = CreateContext();
        Location location = CreateProviderLocation();
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        BaseCommandResponse<Guid> result = await context.Handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                FullName = new UpdateLocationFullNameDto { Value = "Changed venue" }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.FullName).IsEqualTo("Changed venue");
        await Assert.That(location.GetCoordinate()).IsNotNull();
        await Assert.That(location.AddressSource).IsEqualTo(LocationAddressSourceEnum.ProviderSelection);
        await context.Governance.DidNotReceive().ResolveAsync(
            Arg.Any<AddressGovernancePolicyRequest>(),
            Arg.Any<CancellationToken>());
        await context.Locations.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenAddressIsSuppliedWithoutExistingPostcode_ThrowsBeforePolicy()
    {
        TestContext context = CreateContext();
        Location location = CreateLocationWithoutAddress();
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        await Assert.That(async () => await context.Handler.Handle(
            Command(location, address: "New address"),
            CancellationToken.None)).Throws<InvalidOperationException>();
        await context.Governance.DidNotReceive().ResolveAsync(
            Arg.Any<AddressGovernancePolicyRequest>(),
            Arg.Any<CancellationToken>());
        await context.Locations.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCountryChanges_AppliesCountry()
    {
        TestContext context = CreateContext();
        Location location = CreateManualLocation();
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        BaseCommandResponse<Guid> result = await context.Handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                Country = new UpdateLocationCountryDto { Value = "NL" }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.Country).IsEqualTo("NL");
        await context.Locations.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenCityChanges_AppliesCity()
    {
        TestContext context = CreateContext();
        Location location = CreateManualLocation();
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        BaseCommandResponse<Guid> result = await context.Handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                City = new UpdateLocationCityDto { Value = "Rotterdam" }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.City).IsEqualTo("Rotterdam");
        await context.Locations.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenTimezoneHasAValue_AppliesTimezone()
    {
        TestContext context = CreateContext();
        Location location = CreateManualLocation();
        context.Locations.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        BaseCommandResponse<Guid> result = await context.Handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                Timezone = new UpdateLocationTimezoneDto
                {
                    Value = OptionalUpdate<string?>.Set("Europe/Amsterdam")
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.Timezone).IsEqualTo("Europe/Amsterdam");
        await context.Locations.Received(1).Update(location, CancellationToken.None);
    }

    private static async Task AssertPolicyRequest(TestContext context, CancellationToken expectedToken)
    {
        await context.Governance.Received(1).ResolveAsync(
            Arg.Is<AddressGovernancePolicyRequest>(request =>
                request != null
                && request.TenantId == TenantId
                && request.ActorId == UserId
                && request.UserId == UserId
                && request.OrganizationId == null),
            expectedToken);
    }

    private static TestContext CreateContext()
    {
        var locations = Substitute.For<ILocationRepository>();
        var tenant = Substitute.For<ITenantContext>();
        var user = Substitute.For<IUserContext>();
        var governance = Substitute.For<IAddressGovernancePolicyResolver>();
        tenant.TenantId.Returns(TenantId);
        user.GetRequiredUserId().Returns(UserId);
        governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Allowed(
                AddressCreationMode.OpenWithModeration,
                LocationAddressVisibilityEnum.CreatorPrivate));
        var handler = new UpdateLocationCommandHandler(
            locations,
            Substitute.For<IAddressSelectionProtector>(),
            tenant,
            user,
            governance,
            new FixedTimeProvider(Now));
        return new TestContext(handler, locations, governance);
    }

    private static UpdateLocationCommand Command(
        Location location,
        string? address = null,
        string? postcode = null) => new()
    {
        LocationId = location.Id,
        ExpectedConcurrencyStamp = location.ConcurrencyStamp,
        UpdateLocationDto = new UpdateLocationDto
        {
            Address = address is null ? null : new UpdateLocationAddressDto { Value = address },
            Postcode = postcode is null ? null : new UpdateLocationPostcodeDto { Value = postcode }
        }
    };

    private static Location CreateManualLocation()
    {
        Location location = CreateLocationWithoutAddress();
        location.SetManualAddress("Existing address", "1000");
        location.ApplyAddressGovernance(
            UserId,
            LocationAddressSourceEnum.Manual,
            LocationAddressVisibilityEnum.CreatorPrivate,
            null);
        return location;
    }

    private static Location CreateProviderLocation()
    {
        Location location = CreateLocationWithoutAddress();
        location.SetProviderAddress(
            "Existing address",
            "1000",
            GeoCoordinate.Create(50.8503, 4.3517));
        return location;
    }

    private static Location CreateLocationWithoutAddress() => new()
    {
        Id = Guid.Parse("019b0000-0031-7000-8000-000000000010"),
        TenantId = TenantId,
        FullName = "Existing venue",
        Country = "BE",
        City = "Brussels",
        Timezone = "Europe/Brussels",
        ConcurrencyStamp = Guid.Parse("019b0000-0031-7000-8000-000000000011")
    };

    private sealed record TestContext(
        UpdateLocationCommandHandler Handler,
        ILocationRepository Locations,
        IAddressGovernancePolicyResolver Governance);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
