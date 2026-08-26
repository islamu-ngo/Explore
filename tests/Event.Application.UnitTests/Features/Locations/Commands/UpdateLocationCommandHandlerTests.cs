// ABOUTME: Unit tests for grouped Location update command handling.
// ABOUTME: Covers validation, optimistic concurrency, PII-backed fields, and explicit clear semantics.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Geocoding;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Exceptions;
using Explore.Application.Features.Geocoding;
using Explore.Application.Features.Locations.Handlers.Commands;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Domain.ValueObjects;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Locations.Commands;

public class UpdateLocationCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("019b0000-0020-7000-8000-000000000001");
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IAddressGovernancePolicyResolver _governance = Substitute.For<IAddressGovernancePolicyResolver>();
    private readonly IAddressSelectionProtector _selectionProtector =
        Substitute.For<IAddressSelectionProtector>();
    private readonly UpdateLocationCommandHandler _handler;

    public UpdateLocationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _userContext.GetRequiredUserId().Returns(Guid.Parse("019b0000-0020-7000-8000-000000000002"));
        _governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Allowed(
                AddressCreationMode.OpenWithModeration,
                LocationAddressVisibilityEnum.CreatorPrivate));
        _handler = new UpdateLocationCommandHandler(
            _locationRepository,
            _selectionProtector,
            _tenantContext,
            _userContext,
            _governance,
            TimeProvider.System);
    }

    [Test]
    public async Task Handle_WhenWrapperHasNoGroups_ReturnsValidationFailureAndDoesNotSave()
    {
        var result = await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateLocationDto = new UpdateLocationDto()
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSingleFieldGroupIsPresent_UpdatesOnlyThatField()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var result = await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                FullName = new UpdateLocationFullNameDto { Value = "Updated Venue" }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.FullName).IsEqualTo("Updated Venue");
        await Assert.That(location.Address).IsEqualTo("Existing address");
        await _locationRepository.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenManualAddressChanges_ClearsProviderCoordinatePair()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var result = await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                Address = new UpdateLocationAddressDto { Value = "Updated manual address" }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.Address).IsEqualTo("Updated manual address");
        await Assert.That(location.GetCoordinate()).IsNull();
        await Assert.That(location.AddressSource).IsEqualTo(LocationAddressSourceEnum.Manual);
        await Assert.That(location.AddressVisibility).IsEqualTo(LocationAddressVisibilityEnum.CreatorPrivate);
        await Assert.That(location.AddressOrganizationId).IsNull();
        await Assert.That(location.UpdatedBy).IsEqualTo(_userContext.GetRequiredUserId());
        await _locationRepository.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenManualBundleIsExactNoOp_DoesNotResolveRotateAuditOrPersist()
    {
        var location = CreateLocation();
        location.SetManualAddress("Existing address", "1000");
        location.ApplyAddressGovernance(
            Guid.CreateVersion7(),
            LocationAddressSourceEnum.Manual,
            LocationAddressVisibilityEnum.TenantApproved,
            null);
        Guid stamp = location.ConcurrencyStamp;
        DateTime? updatedAt = location.UpdatedAt;
        Guid? updatedBy = location.UpdatedBy;
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var result = await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = stamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                Address = new UpdateLocationAddressDto { Value = "Existing address" },
                Postcode = new UpdateLocationPostcodeDto { Value = "1000" }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.AddressVisibility).IsEqualTo(LocationAddressVisibilityEnum.TenantApproved);
        await Assert.That(location.ConcurrencyStamp).IsEqualTo(stamp);
        await Assert.That(location.UpdatedAt).IsEqualTo(updatedAt);
        await Assert.That(location.UpdatedBy).IsEqualTo(updatedBy);
        await _governance.DidNotReceive().ResolveAsync(
            Arg.Any<AddressGovernancePolicyRequest>(),
            Arg.Any<CancellationToken>());
        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAddressGovernanceDenies_DoesNotPartiallyMutateApprovedProviderLocation()
    {
        var location = CreateLocation();
        Guid organizationId = Guid.CreateVersion7();
        location.ApplyAddressGovernance(
            Guid.CreateVersion7(),
            LocationAddressSourceEnum.ProviderSelection,
            LocationAddressVisibilityEnum.TenantApproved,
            organizationId);
        Guid stamp = location.ConcurrencyStamp;
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        _governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Denied(AddressCreationMode.Disabled));

        var result = await _handler.Handle(AddressPatch(location, "Denied replacement"), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Location update failed.");
        await Assert.That(location.Address).IsEqualTo("Existing address");
        await Assert.That(location.GetCoordinate()).IsNotNull();
        await Assert.That(location.AddressSource).IsEqualTo(LocationAddressSourceEnum.ProviderSelection);
        await Assert.That(location.AddressVisibility).IsEqualTo(LocationAddressVisibilityEnum.TenantApproved);
        await Assert.That(location.AddressOrganizationId).IsEqualTo(organizationId);
        await Assert.That(location.ConcurrencyStamp).IsEqualTo(stamp);
        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenAddressResolverCancels_DoesNotMutateOrPersist()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        using var cancellation = new CancellationTokenSource();
        _governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<AddressGovernancePolicyDecision>(cancellation.Token);
            });

        await Assert.That(async () => await _handler.Handle(
            AddressPatch(location, "Cancelled replacement"),
            cancellation.Token)).Throws<OperationCanceledException>();

        await Assert.That(location.Address).IsEqualTo("Existing address");
        await Assert.That(location.GetCoordinate()).IsNotNull();
        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRepositoryReturnsForeignTenant_ReturnsNotFoundWithoutGovernanceMutationOrSave()
    {
        var location = CreateLocation();
        location.TenantId = Guid.Parse("019b0000-0020-7000-8000-000000000099");
        string? address = location.Address;
        string? postcode = location.Postcode;
        var coordinate = location.GetCoordinate();
        LocationAddressSourceEnum source = location.AddressSource;
        LocationAddressVisibilityEnum visibility = location.AddressVisibility;
        Guid stamp = location.ConcurrencyStamp;
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var result = await _handler.Handle(
            AddressPatch(location, "Foreign tenant replacement"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(Explore.Application.Responses.FailureCodes.NotFound);
        await Assert.That(result.Message).IsEqualTo("Location not found.");
        await Assert.That(location.Address).IsEqualTo(address);
        await Assert.That(location.Postcode).IsEqualTo(postcode);
        await Assert.That(location.GetCoordinate()).IsEqualTo(coordinate);
        await Assert.That(location.AddressSource).IsEqualTo(source);
        await Assert.That(location.AddressVisibility).IsEqualTo(visibility);
        await Assert.That(location.ConcurrencyStamp).IsEqualTo(stamp);
        await _governance.DidNotReceive().ResolveAsync(
            Arg.Any<AddressGovernancePolicyRequest>(),
            Arg.Any<CancellationToken>());
        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithValidPatch_ForwardsExactRequestTokenToLoadAndUpdate()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        using var cancellation = new CancellationTokenSource();

        var result = await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                FullName = new UpdateLocationFullNameDto { Value = "Token venue" }
            }
        }, cancellation.Token);

        await Assert.That(result.IsSuccess).IsTrue();
        await _locationRepository.Received(1).GetById(location.Id, cancellation.Token);
        await _locationRepository.Received(1).Update(location, cancellation.Token);
    }

    [Test]
    public async Task Handle_WithProtectedSelection_BindsTargetAndAppliesAtomicProviderBundle()
    {
        Location location = CreateLocation();
        Guid userId = _userContext.GetRequiredUserId();
        Guid organizationId = Guid.CreateVersion7();
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        _selectionProtector.ConfigurationFingerprint.Returns("configuration-v1");
        var selection = new ProtectedAddressSelection
        {
            DisplayName = "Updated Provider Hall",
            Address = "Updated Provider Street 30",
            Postcode = "2000",
            City = "Antwerp",
            Country = "BE",
            Timezone = "Europe/Brussels",
            Latitude = 51.2194,
            Longitude = 4.4025,
            Attribution = "Provider attribution",
            Provenance = new ProtectedAddressProvenance
            {
                Provider = "Photon",
                ProviderRecordId = "provider-record",
                DatasetVersion = "dataset-v1"
            }
        };
        _selectionProtector.UnprotectAsync(
                "opaque-selection",
                Arg.Any<AddressSelectionContext>(),
                Arg.Any<CancellationToken>())
            .Returns(AddressSelectionUnprotectResult.Success(selection));
        var command = new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                AddressSelectionToken = "opaque-selection",
                OrganizationId = organizationId
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.FullName).IsEqualTo("Updated Provider Hall");
        await Assert.That(location.Address).IsEqualTo("Updated Provider Street 30");
        await Assert.That(location.Postcode).IsEqualTo("2000");
        await Assert.That(location.AddressSource).IsEqualTo(LocationAddressSourceEnum.ProviderSelection);
        await Assert.That(location.GetCoordinate()?.Latitude).IsEqualTo(51.2194);
        await Assert.That(location.GetCoordinate()?.Longitude).IsEqualTo(4.4025);
        await _selectionProtector.Received(1).UnprotectAsync(
            "opaque-selection",
            Arg.Is<AddressSelectionContext>(context =>
                context.TenantId == TenantId
                && context.ActorId == userId
                && context.OrganizationId == organizationId
                && context.Purpose == AddressSelectionPurpose.UpdateLocation
                && context.Target.LocationId == location.Id
                && context.Target.ExpectedConcurrencyStamp == command.ExpectedConcurrencyStamp
                && context.ConfigurationFingerprint == "configuration-v1"),
            CancellationToken.None);
        await _locationRepository.Received(1).Update(location, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenCancellationArrivesImmediatelyBeforeWrite_PropagatesWithoutSave()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        using var cancellation = new CancellationTokenSource();
        var handler = new UpdateLocationCommandHandler(
            _locationRepository,
            _selectionProtector,
            _tenantContext,
            _userContext,
            _governance,
            new CancellingTimeProvider(cancellation));

        await Assert.That(async () => await handler.Handle(
            AddressPatch(location, "Cancelled before write"),
            cancellation.Token)).Throws<OperationCanceledException>();

        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConflictAndDoesNotSave()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        ConcurrencyConflictException? exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            _handler.Handle(new UpdateLocationCommand
            {
                LocationId = location.Id,
                ExpectedConcurrencyStamp = Guid.NewGuid(),
                UpdateLocationDto = new UpdateLocationDto
                {
                    FullName = new UpdateLocationFullNameDto { Value = "Updated Venue" }
                }
            }, CancellationToken.None));

        await Assert.That(exception!.EntityId).IsNull();
        await Assert.That(exception.ToString()).DoesNotContain(location.Id.ToString("D"));
        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    private static UpdateLocationCommand AddressPatch(Location location, string address) => new()
    {
        LocationId = location.Id,
        ExpectedConcurrencyStamp = location.ConcurrencyStamp,
        UpdateLocationDto = new UpdateLocationDto
        {
            Address = new UpdateLocationAddressDto { Value = address }
        }
    };

    private static Location CreateLocation()
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            FullName = "Existing Venue",
            Country = "BE",
            City = "Brussels",
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        location.SetProviderAddress(
            "Existing address",
            "1000",
            GeoCoordinate.Create(50.8503, 4.3517));
        return location;
    }

    private sealed class CancellingTimeProvider(CancellationTokenSource cancellation) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            cancellation.Cancel();
            return DateTimeOffset.UtcNow;
        }
    }
}
