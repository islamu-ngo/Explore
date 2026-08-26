// ABOUTME: Unit tests for tenant-authoritative Location creation through the public handler.
// ABOUTME: Verifies validation, exact aggregate construction, PII ownership, and tenant mismatch rejection.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Geocoding;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Geocoding;
using Explore.Application.Features.Locations.Handlers.Commands;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Locations.Commands;

public class CreateLocationCommandHandlerTests
{
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IAddressGovernancePolicyResolver _governance = Substitute.For<IAddressGovernancePolicyResolver>();
    private readonly IAddressSelectionProtector _selectionProtector =
        Substitute.For<IAddressSelectionProtector>();
    private readonly CreateLocationCommandHandler _handler;

    public CreateLocationCommandHandlerTests()
    {
        _userContext.GetRequiredUserId().Returns(Guid.Parse("019b0000-0010-7000-8000-000000000001"));
        _governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Allowed(
                AddressCreationMode.OpenWithModeration,
                LocationAddressVisibilityEnum.CreatorPrivate));
        _handler = new CreateLocationCommandHandler(
            _locationRepository,
            _selectionProtector,
            _tenantContext,
            _userContext,
            _governance,
            TimeProvider.System);
    }

    [Test]
    public async Task Handle_WithValidRequest_ConstructsExactLocationAndReturnsSuccess()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid locationId = Guid.CreateVersion7();
        Location? captured = null;
        var persisted = new Location
        {
            Id = locationId,
            TenantId = tenantId,
            FullName = "Persisted Test Location",
            Country = "Belgium",
            City = "Brussels"
        };
        persisted.SetManualAddress("Persisted address", "12345");
        _tenantContext.TenantId.Returns(tenantId);
        _locationRepository.Create(
                Arg.Do<Location>(location => captured = location),
                Arg.Any<CancellationToken>())
            .Returns(persisted);

        var result = await _handler.Handle(CreateCommand(tenantId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(locationId);
        await Assert.That(result.Message).Contains("successfully");
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured?.TenantId).IsEqualTo(tenantId);
        await Assert.That(captured?.FullName).IsEqualTo("Test Location");
        await Assert.That(captured?.Pii?.Address).IsEqualTo("123 Test Street");
        await Assert.That(captured?.Pii?.Postcode).IsEqualTo("12345");
        await Assert.That(captured?.Pii?.Latitude).IsNull();
        await Assert.That(captured?.Pii?.Longitude).IsNull();
        await _locationRepository.Received(1).Create(
            Arg.Any<Location>(),
            CancellationToken.None);
    }

    [Test]
    public async Task Handle_WithValidRequest_ForwardsExactRequestTokenToCreate()
    {
        Guid tenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        using var cancellation = new CancellationTokenSource();
        _locationRepository.Create(Arg.Any<Location>(), cancellation.Token)
            .Returns(new Location
            {
                FullName = "Persisted token venue",
                Country = "Belgium",
                City = "Brussels",
                TenantId = tenantId
            });

        var result = await _handler.Handle(CreateCommand(tenantId), cancellation.Token);

        await Assert.That(result.IsSuccess).IsTrue();
        await _locationRepository.Received(1).Create(
            Arg.Any<Location>(),
            cancellation.Token);
    }

    [Test]
    public async Task Handle_WithMissingFullName_ReturnsValidationError()
    {
        Guid tenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        CreateLocationCommand command = CreateCommand(tenantId, fullName: string.Empty);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRepository.DidNotReceive().Create(
            Arg.Any<Location>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMissingAddress_ReturnsValidationError()
    {
        Guid tenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        CreateLocationCommand command = CreateCommand(tenantId, address: string.Empty);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRepository.DidNotReceive().Create(
            Arg.Any<Location>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenGovernanceDenies_ReturnsGenericFailureBeforeAggregateConstructionOrPersistence()
    {
        Guid tenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Denied(AddressCreationMode.Disabled));

        var result = await _handler.Handle(CreateCommand(tenantId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Location creation failed.");
        await _locationRepository.DidNotReceive().Create(
            Arg.Any<Location>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithAllowedTypedDecisionPersistsOnlyTrustedManualGovernance()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Location? captured = null;
        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _governance.ResolveAsync(Arg.Any<AddressGovernancePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(AddressGovernancePolicyDecision.Allowed(
                AddressCreationMode.AdminOnly,
                LocationAddressVisibilityEnum.TenantApproved));
        _locationRepository.Create(
                Arg.Do<Location>(location => captured = location),
                Arg.Any<CancellationToken>())
            .Returns(new Location
            {
                FullName = "Persisted governance venue",
                Country = "Belgium",
                City = "Brussels",
                TenantId = tenantId
            });

        var result = await _handler.Handle(CreateCommand(tenantId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(captured?.AddressSource).IsEqualTo(LocationAddressSourceEnum.Manual);
        await Assert.That(captured?.AddressVisibility).IsEqualTo(LocationAddressVisibilityEnum.TenantApproved);
        await Assert.That(captured?.AddressOrganizationId).IsNull();
        await Assert.That(captured?.CreatedBy).IsEqualTo(userId);
    }

    [Test]
    public async Task Handle_WithProtectedSelection_UsesAtomicProviderBundleBeforeCreate()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = _userContext.GetRequiredUserId();
        Guid organizationId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _selectionProtector.ConfigurationFingerprint.Returns("configuration-v1");
        var selection = new ProtectedAddressSelection
        {
            DisplayName = "Protected Hall",
            Address = "Protected Street 30",
            Postcode = "1000",
            City = "Brussels",
            Country = "BE",
            Timezone = "Europe/Brussels",
            Latitude = 50.8503,
            Longitude = 4.3517,
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
        Location? captured = null;
        _locationRepository.Create(
                Arg.Do<Location>(location => captured = location),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Location>());
        CreateLocationCommand command = CreateCommand(tenantId) with
        {
            LocationDto = CreateCommand(tenantId).LocationDto with
            {
                AddressSelectionToken = "opaque-selection",
                OrganizationId = organizationId,
                Address = "attacker-controlled duplicate"
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.FullName).IsEqualTo("Protected Hall");
        await Assert.That(captured.Address).IsEqualTo("Protected Street 30");
        await Assert.That(captured.Postcode).IsEqualTo("1000");
        await Assert.That(captured.AddressSource).IsEqualTo(LocationAddressSourceEnum.ProviderSelection);
        await Assert.That(captured.GetCoordinate()?.Latitude).IsEqualTo(50.8503);
        await Assert.That(captured.GetCoordinate()?.Longitude).IsEqualTo(4.3517);
        await _selectionProtector.Received(1).UnprotectAsync(
            "opaque-selection",
            Arg.Is<AddressSelectionContext>(context =>
                context.TenantId == tenantId
                && context.ActorId == userId
                && context.OrganizationId == organizationId
                && context.Purpose == AddressSelectionPurpose.CreateLocation
                && context.Target.LocationId == null
                && context.Target.ExpectedConcurrencyStamp == null
                && context.ConfigurationFingerprint == "configuration-v1"),
            CancellationToken.None);
    }

    [Test]
    public async Task Handle_WithInvalidProtectedSelection_FailsClosedWithoutPersistence()
    {
        Guid tenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(tenantId);
        _selectionProtector.ConfigurationFingerprint.Returns("configuration-v1");
        _selectionProtector.UnprotectAsync(
                Arg.Any<string>(),
                Arg.Any<AddressSelectionContext>(),
                Arg.Any<CancellationToken>())
            .Returns(AddressSelectionUnprotectResult.Failure(AddressSelectionFailureCode.Expired));
        CreateLocationCommand command = CreateCommand(tenantId) with
        {
            LocationDto = CreateCommand(tenantId).LocationDto with
            {
                AddressSelectionToken = "expired-selection"
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.AddressSelectionInvalid);
        await _locationRepository.DidNotReceive().Create(
            Arg.Any<Location>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Constructor_DoesNotAcceptMapperAuthority()
    {
        Type[] constructorParameters = typeof(CreateLocationCommandHandler)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        await Assert.That(constructorParameters.Any(type => type.FullName == "AutoMapper.IMapper")).IsFalse();
    }

    [Test]
    public async Task Handle_WhenCommandTenantDiffersFromContext_FailsBeforePersistence()
    {
        Guid contextTenantId = Guid.CreateVersion7();
        _tenantContext.TenantId.Returns(contextTenantId);

        var result = await _handler.Handle(CreateCommand(Guid.CreateVersion7()), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRepository.DidNotReceive().Create(
            Arg.Any<Location>(),
            Arg.Any<CancellationToken>());
    }

    private static CreateLocationCommand CreateCommand(
        Guid tenantId,
        string fullName = "Test Location",
        string address = "123 Test Street") => new()
    {
        TenantId = tenantId,
        LocationDto = new CreateLocationDto
        {
            FullName = fullName,
            Address = address,
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels"
        }
    };
}
