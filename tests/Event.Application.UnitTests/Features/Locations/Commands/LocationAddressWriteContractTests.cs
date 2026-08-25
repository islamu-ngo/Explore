// ABOUTME: Failing public-contract specifications for governed Location address writes.
// ABOUTME: Locks raw-coordinate contraction, atomic finite pairs, tenancy, construction, consent, and erasure.

using System.Reflection;
using System.Runtime.ExceptionServices;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Locations.Handlers.Commands;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Profiles;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Locations.Commands;

public sealed class LocationAddressWriteContractTests
{
    private const string ManualTransitionName = "SetManualAddress";
    private const string ProviderTransitionName = "SetProviderAddress";

    [Test]
    public async Task DirectLocationWriteContractsDoNotExposeRawCoordinateOrTenantMembers()
    {
        string[] forbiddenMembers =
        [
            .. NamedPublicProperties(typeof(CreateLocationDto), "TenantId", "Latitude", "Longitude"),
            .. NamedPublicProperties(typeof(UpdateLocationDto), "TenantId", "Latitude", "Longitude"),
            .. NamedPublicProperties(typeof(UpdateLocationLatitudeDto), "Latitude", "Longitude"),
            .. NamedPublicProperties(typeof(UpdateLocationLongitudeDto), "Latitude", "Longitude"),
            .. typeof(Location).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.Name is "Latitude" or "Longitude" && property.SetMethod?.IsPublic == true)
                .Select(property => $"{nameof(Location)}.{property.Name} setter")
        ];

        await Assert.That(forbiddenMembers).IsEmpty();
    }

    [Test]
    public async Task CreateWithRepositoryNativeRealMapperConstructsTheLocationPiiAggregate()
    {
        Guid tenantId = Guid.CreateVersion7();
        var locations = Substitute.For<ILocationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        Location? persisted = null;
        locations.Create(Arg.Do<Location>(location =>
            {
                persisted = location;
                location.Id = Guid.CreateVersion7();
            }))
            .Returns(call => call.Arg<Location>()
                ?? throw new InvalidOperationException("The location repository received a null entity."));
        var handler = new CreateLocationCommandHandler(locations, tenantContext, CreateRealMapper());
        Exception? constructionFailure = null;

        try
        {
            await handler.Handle(new CreateLocationCommand
            {
                TenantId = tenantId,
                LocationDto = ManualCreateDto()
            }, CancellationToken.None);
        }
        catch (Exception exception)
        {
            constructionFailure = exception;
        }

        var violations = new List<string>();
        if (constructionFailure is not null)
        {
            violations.Add($"real aggregate construction threw {constructionFailure.GetType().Name}");
        }
        if (persisted?.Pii is not { Address: "Rue Manual 20", Postcode: "1000" })
        {
            violations.Add("the persisted Location was not constructed with its required PII child");
        }
        if (persisted?.TenantId != tenantId)
        {
            violations.Add("the persisted Location did not use the trusted tenant");
        }

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task CreateWhenTrustedCommandTenantDiffersFromContextFailsClosedBeforePersistence()
    {
        Guid contextTenantId = Guid.CreateVersion7();
        var locations = Substitute.For<ILocationRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var mapper = Substitute.For<IMapper>();
        tenantContext.TenantId.Returns(contextTenantId);
        mapper.Map<Location>(Arg.Any<CreateLocationDto>()).Returns(NewLocation(contextTenantId));
        locations.Create(Arg.Any<Location>()).Returns(call => call.Arg<Location>()
            ?? throw new InvalidOperationException("The location repository received a null entity."));
        var handler = new CreateLocationCommandHandler(locations, tenantContext, mapper);

        var response = await handler.Handle(new CreateLocationCommand
        {
            TenantId = Guid.CreateVersion7(),
            LocationDto = ManualCreateDto()
        }, CancellationToken.None);

        var violations = new List<string>();
        if (response.IsSuccess)
        {
            violations.Add("a command tenant that disagreed with trusted context was accepted");
        }
        if (locations.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ILocationRepository.Create)))
        {
            violations.Add("the mismatched-tenant request reached persistence");
        }

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task PublicAddressTransitionContractExposesManualAndProviderAtomicOperations()
    {
        var violations = new List<string>();
        if (FindManualTransition() is null)
        {
            violations.Add($"Location is missing public {ManualTransitionName}(string address, string postcode)");
        }
        if (FindProviderTransition() is null)
        {
            violations.Add($"Location is missing public {ProviderTransitionName}(string address, string postcode, double? latitude, double? longitude)");
        }

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task ManualAddressTransitionOnActivePrivateHomeReplacesManualFieldsAndClearsStaleCoordinates()
    {
        MethodInfo? transition = FindManualTransition();
        await Assert.That(transition).IsNotNull().Because($"Location must expose public atomic {ManualTransitionName}");
        Guid ownerId = Guid.CreateVersion7();
        Location location = NewLocation(Guid.CreateVersion7(), 50.8503, 4.3517);
        location.ClassifyAsPrivateHome(ownerId);

        InvokeTransition(transition!, location, "Rue Manual 21", "2000");

        await Assert.That(location.Pii?.Address).IsEqualTo("Rue Manual 21");
        await Assert.That(location.Pii?.Postcode).IsEqualTo("2000");
        await Assert.That(location.Pii?.Latitude).IsNull();
        await Assert.That(location.Pii?.Longitude).IsNull();
        await Assert.That(location.LocationPrivacyStateId).IsEqualTo((int)LocationPrivacyStateEnum.Active);
        await Assert.That(location.LocationKindId).IsEqualTo((int)LocationKindEnum.PrivateHome);
        await Assert.That(location.OwnerUserId).IsEqualTo(ownerId);
    }

    [Test]
    public async Task ProviderAddressTransitionWithNormalFinitePairPersistsThePairExactly()
    {
        MethodInfo? transition = FindProviderTransition();
        await Assert.That(transition).IsNotNull().Because($"Location must expose public atomic {ProviderTransitionName}");
        Location location = NewLocation(Guid.CreateVersion7());

        InvokeTransition(transition!, location, "Rue Provider 30", "1000", 50.8503, 4.3517);

        await Assert.That(location.Pii?.Address).IsEqualTo("Rue Provider 30");
        await Assert.That(location.Pii?.Postcode).IsEqualTo("1000");
        await Assert.That(location.Pii?.Latitude).IsEqualTo(50.8503);
        await Assert.That(location.Pii?.Longitude).IsEqualTo(4.3517);
    }

    [Test]
    public async Task ProviderAddressTransitionWithPartialOrNonFinitePairRejectsWithoutMutation()
    {
        MethodInfo? transition = FindProviderTransition();
        await Assert.That(transition).IsNotNull().Because($"Location must expose public atomic {ProviderTransitionName}");
        (double? Latitude, double? Longitude, string Case)[] invalidPairs =
        [
            (50.8503, null, "partial pair missing longitude"),
            (null, 4.3517, "partial pair missing latitude"),
            (double.NaN, 4.3517, "NaN"),
            (50.8503, double.NaN, "NaN longitude"),
            (double.PositiveInfinity, 4.3517, "positive infinity"),
            (50.8503, double.PositiveInfinity, "positive infinity longitude"),
            (double.NegativeInfinity, 4.3517, "negative infinity"),
            (50.8503, double.NegativeInfinity, "negative infinity longitude")
        ];
        var violations = new List<string>();

        foreach (var pair in invalidPairs)
        {
            Location location = NewLocation(Guid.CreateVersion7(), 1.25, 2.5);
            bool rejected = InvokeExpectedRejection(transition!, location, "Mutated address", "9999", pair.Latitude, pair.Longitude);
            if (!rejected)
            {
                violations.Add($"{pair.Case} was accepted");
            }
            if (location.Pii is not { Address: "Rue Existing 10", Postcode: "1000", Latitude: 1.25, Longitude: 2.5 })
            {
                violations.Add($"{pair.Case} mutated the aggregate before rejection");
            }
        }

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task LegacySequentialCoordinateSettersWhenPresentExposePartialIntermediateStateDebt()
    {
        PropertyInfo? latitude = typeof(Location).GetProperty("Latitude");
        PropertyInfo? longitude = typeof(Location).GetProperty("Longitude");
        if (latitude?.SetMethod?.IsPublic != true || longitude?.SetMethod?.IsPublic != true)
        {
            return;
        }

        Location location = NewLocation(Guid.CreateVersion7());
        latitude.SetValue(location, 50.8503);
        bool exposedPartialIntermediateState = location.Pii?.Latitude == 50.8503 && location.Pii?.Longitude is null;

        await Assert.That(exposedPartialIntermediateState)
            .IsFalse()
            .Because("legacy sequential coordinate setters create a partial intermediate state and remain contract debt");
    }

    [Test]
    public async Task PatchWhenManualAddressChangesClearsStaleCoordinatesAndPreservesPrivateHomeConsent()
    {
        Guid ownerId = Guid.CreateVersion7();
        Location location = NewLocation(Guid.CreateVersion7(), 50.8503, 4.3517);
        location.ClassifyAsPrivateHome(ownerId);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id).Returns(location);
        var handler = new UpdateLocationCommandHandler(locations);

        var response = await handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                Address = new UpdateLocationAddressDto { Value = "Rue Manual 21" }
            }
        }, CancellationToken.None);

        var violations = new List<string>();
        if (!response.IsSuccess)
        {
            violations.Add("the valid manual address PATCH was rejected");
        }
        if (location.Pii?.Address != "Rue Manual 21")
        {
            violations.Add("the manual address PATCH did not persist the exact requested address");
        }
        if (location.Pii?.Postcode != "1000")
        {
            violations.Add("the address-only PATCH did not retain the existing postcode");
        }
        if (location.Pii?.Latitude is not null || location.Pii?.Longitude is not null)
        {
            violations.Add("manual address mutation retained stale coordinates");
        }
        if (location.LocationKindId != (int)LocationKindEnum.PrivateHome || location.OwnerUserId != ownerId)
        {
            violations.Add("manual address mutation changed consent-backed Private Home ownership");
        }
        var updateCalls = locations.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILocationRepository.Update))
            .ToArray();
        if (updateCalls.Length != 1 || !ReferenceEquals(updateCalls[0].GetArguments()[0], location))
        {
            violations.Add("the manual address PATCH did not update the same aggregate exactly once");
        }

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task ErasureRemainsAuthoritativeAgainstAllPublicAddressTransitions()
    {
        Location location = NewLocation(Guid.CreateVersion7(), 50.8503, 4.3517);
        location.ClassifyAsPrivateHome(Guid.CreateVersion7());
        location.EraseOwnedPii(DateTime.UtcNow, LocationPrivacyErasureReasonEnum.OwnerErasureRequest);
        MethodInfo? manual = FindManualTransition();
        MethodInfo? provider = FindProviderTransition();
        var violations = new List<string>();

        if (manual is null || provider is null)
        {
            violations.Add("public manual/provider address transitions are missing, so anti-resurrection cannot be exercised");
        }
        else
        {
            if (!InvokeExpectedRejection(manual, location, "Resurrected address", "9999"))
            {
                violations.Add("manual address transition resurrected erased PII");
            }
            if (!InvokeExpectedRejection(provider, location, "Resurrected address", "9999", 50.8503, 4.3517))
            {
                violations.Add("provider address transition resurrected erased PII");
            }
        }
        if (location.Pii is not null || location.LocationPrivacyStateId != (int)LocationPrivacyStateEnum.Erased)
        {
            violations.Add("erasure tombstone or PII absence changed during a public address transition");
        }

        await Assert.That(violations).IsEmpty();
    }

    private static IEnumerable<string> NamedPublicProperties(Type type, params string[] names) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => names.Contains(property.Name, StringComparer.Ordinal))
            .Select(property => $"{type.Name}.{property.Name}");

    private static MethodInfo? FindManualTransition() => typeof(Location).GetMethod(
        ManualTransitionName,
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        [typeof(string), typeof(string)],
        modifiers: null);

    private static MethodInfo? FindProviderTransition() => typeof(Location).GetMethod(
        ProviderTransitionName,
        BindingFlags.Instance | BindingFlags.Public,
        binder: null,
        [typeof(string), typeof(string), typeof(double?), typeof(double?)],
        modifiers: null);

    private static void InvokeTransition(MethodInfo transition, Location location, params object?[] arguments)
    {
        try
        {
            transition.Invoke(location, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static bool InvokeExpectedRejection(MethodInfo transition, Location location, params object?[] arguments)
    {
        try
        {
            transition.Invoke(location, arguments);
            return false;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException or InvalidOperationException)
        {
            return true;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static CreateLocationDto ManualCreateDto() => new()
    {
        FullName = "Manual venue",
        Address = "Rue Manual 20",
        Postcode = "1000",
        Country = "Belgium",
        City = "Brussels"
    };

    private static Location NewLocation(Guid tenantId, double? latitude = null, double? longitude = null)
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            FullName = "Manual venue",
            Country = "Belgium",
            City = "Brussels",
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        location.AttachPii(new LocationPii
        {
            Address = "Rue Existing 10",
            Postcode = "1000",
            Latitude = latitude,
            Longitude = longitude
        });
        return location;
    }

    private static IMapper CreateRealMapper()
    {
#if USE_COMMERCIAL_LUCKYPENNY_LIBS
        var configuration = new MapperConfiguration(
            expression => expression.AddProfile<LookupMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
#else
        var configuration = new MapperConfiguration(expression => expression.AddProfile<LookupMappingProfile>());
#endif
        return configuration.CreateMapper();
    }
}
