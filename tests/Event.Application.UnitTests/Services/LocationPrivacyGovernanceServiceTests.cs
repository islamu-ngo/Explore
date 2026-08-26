// ABOUTME: Verifies fail-closed instance/tenant composition for public EventLocation disclosure governance.
// ABOUTME: Covers exact setting registration, restrictive merges, invalid storage, and the reusable public evaluator boundary.

using System.Reflection;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using Explore.Domain.Settings.Definitions;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class LocationPrivacyGovernanceServiceTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private readonly ISystemSettingRepository _systemSettings = Substitute.For<ISystemSettingRepository>();
    private readonly ITenantSettingRepository _tenantSettings = Substitute.For<ITenantSettingRepository>();

    [Test]
    public async Task Definitions_RegisterExactInstanceToTenantPolicyContract()
    {
        SettingDefinition[] definitions =
        [
            LocationPrivacySettingDefinitions.AllowHomeLocations,
            LocationPrivacySettingDefinitions.AllowPublicExactAddress,
            LocationPrivacySettingDefinitions.AllowPublicCoordinates,
            LocationPrivacySettingDefinitions.MinimumHomeAudience,
            LocationPrivacySettingDefinitions.DefaultRevealOffset
        ];

        await Assert.That(definitions.Select(x => x.Key)).IsEquivalentTo(new[]
        {
            GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations,
            GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress,
            GovernanceSettingKeys.LocationPrivacy.AllowPublicCoordinates,
            GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience,
            GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset
        });

        foreach (SettingDefinition definition in definitions)
        {
            await Assert.That(definition.MinScope).IsEqualTo(SettingScope.Instance);
            await Assert.That(definition.MaxScope).IsEqualTo(SettingScope.Tenant);
            await Assert.That(definition.IsLockable).IsTrue();
            await Assert.That(SettingRegistry.Get(definition.Key)).IsEqualTo(definition);
        }

        await Assert.That(definitions.Take(3).All(x => x.DefaultValue == "false")).IsTrue();
        await Assert.That(LocationPrivacySettingDefinitions.MinimumHomeAudience.DefaultValue).IsEqualTo("\"NEVER\"");
        await Assert.That(LocationPrivacySettingDefinitions.MinimumHomeAudience.AllowedValues)
            .IsEquivalentTo(new[] { "NEVER", "CONFIRMED_PARTICIPANT", "ANY_CURRENT_REGISTRANT" });
        await Assert.That(LocationPrivacySettingDefinitions.DefaultRevealOffset.DefaultValue).IsEqualTo("\"P30D\"");
    }

    [Test]
    public async Task ResolveAsync_ComposesInstanceAndTenantWithMostRestrictiveValuePerField()
    {
        ConfigureSettings(
            system:
            [
                System(GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations, "true"),
                System(GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress, "true"),
                System(GovernanceSettingKeys.LocationPrivacy.AllowPublicCoordinates, "false"),
                System(GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience, "\"ANY_CURRENT_REGISTRANT\""),
                System(GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset, "\"PT2H\"")
            ],
            tenant:
            [
                Tenant(GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations, "false"),
                Tenant(GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress, "true"),
                Tenant(GovernanceSettingKeys.LocationPrivacy.AllowPublicCoordinates, "true"),
                Tenant(GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience, "\"CONFIRMED_PARTICIPANT\""),
                Tenant(GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset, "\"P1D\"")
            ]);

        var result = await CreateService().ResolveAsync(TenantId);

        await Assert.That(result.IsResolved).IsTrue();
        await Assert.That(result.ReasonCode).IsEqualTo(LocationPrivacyGovernanceReasonCode.Resolved);
        await Assert.That(result.AllowHomeLocations).IsFalse();
        await Assert.That(result.AllowPublicExactAddress).IsTrue();
        await Assert.That(result.AllowPublicCoordinates).IsFalse();
        await Assert.That(result.MinimumHomeAudience).IsEqualTo(LocationDisclosureAudienceEnum.ConfirmedParticipant);
        await Assert.That(result.DefaultRevealOffset).IsEqualTo(TimeSpan.FromDays(1));
    }

    [Test]
    public async Task ResolveAsync_MissingRowsUseConservativeInstanceDefaultsAndTenantInheritance()
    {
        ConfigureSettings([], []);

        var result = await CreateService().ResolveAsync(TenantId);

        await Assert.That(result.IsResolved).IsTrue();
        await Assert.That(result.AllowHomeLocations).IsFalse();
        await Assert.That(result.AllowPublicExactAddress).IsFalse();
        await Assert.That(result.AllowPublicCoordinates).IsFalse();
        await Assert.That(result.MinimumHomeAudience).IsEqualTo(LocationDisclosureAudienceEnum.Never);
        await Assert.That(result.DefaultRevealOffset).IsEqualTo(TimeSpan.FromDays(30));
    }

    [Test]
    [Arguments("true", "\"true\"")]
    [Arguments("\"NEVER\"", "\"UNKNOWN\"")]
    [Arguments("\"PT0S\"", "\"P1M\"")]
    [Arguments("\"PT0S\"", "\"P999999999999999999D\"")]
    public async Task ResolveAsync_InvalidTenantJsonOrValueFailsClosed(string validInstance, string invalidTenant)
    {
        string key = validInstance switch
        {
            "true" => GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations,
            "\"NEVER\"" => GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience,
            _ => GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset
        };
        ConfigureSettings([System(key, validInstance)], [Tenant(key, invalidTenant)]);

        var result = await CreateService().ResolveAsync(TenantId);

        await Assert.That(result.IsResolved).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo(LocationPrivacyGovernanceReasonCode.InvalidTenantSetting);
        await Assert.That(result.AllowHomeLocations).IsFalse();
        await Assert.That(result.AllowPublicExactAddress).IsFalse();
        await Assert.That(result.AllowPublicCoordinates).IsFalse();
        await Assert.That(result.MinimumHomeAudience).IsEqualTo(LocationDisclosureAudienceEnum.Never);
        await Assert.That(result.DefaultRevealOffset).IsEqualTo(TimeSpan.FromDays(30));
    }

    [Test]
    public async Task ResolveAsync_ConflictingDuplicateOrRepositoryFailureFailsClosed()
    {
        ConfigureSettings(
            [
                System(GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations, "true"),
                System(GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations, "false")
            ],
            []);

        var duplicate = await CreateService().ResolveAsync(TenantId);
        await Assert.That(duplicate.IsResolved).IsFalse();
        await Assert.That(duplicate.ReasonCode).IsEqualTo(LocationPrivacyGovernanceReasonCode.InvalidInstanceSetting);

        _systemSettings.GetAllSettings("LocationPrivacy", Arg.Any<CancellationToken>())
            .Returns<Task<List<SystemSetting>>>(_ => throw new InvalidOperationException("database details must not escape"));

        var unavailable = await CreateService().ResolveAsync(TenantId);
        await Assert.That(unavailable.IsResolved).IsFalse();
        await Assert.That(unavailable.ReasonCode).IsEqualTo(LocationPrivacyGovernanceReasonCode.RepositoryUnavailable);
    }

    [Test]
    public async Task ResolveAsync_UsesExactFiveKeyTenantReadWithSameCancellationToken()
    {
        ConfigureSettings([], []);
        using var cancellation = new CancellationTokenSource();

        var result = await CreateService().ResolveAsync(TenantId, cancellation.Token);

        await Assert.That(result.IsResolved).IsTrue();
        await _tenantSettings.Received(1).GetByTenantAndKeys(
            TenantId,
            Arg.Is<IReadOnlyCollection<string>>(keys =>
                keys.Count == 5
                && keys.ToHashSet(StringComparer.Ordinal).SetEquals(LocationPrivacyKeys)),
            cancellation.Token);
        await _tenantSettings.DidNotReceive().GetAllForTenant(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveAsync_RejectsUnknownInstanceCategoryKeyBeforeTenantRead()
    {
        ConfigureSettings(
            [System("location_privacy.unknown", "true")],
            []);

        var result = await CreateService().ResolveAsync(TenantId);

        await Assert.That(result.IsResolved).IsFalse();
        await Assert.That(result.ReasonCode).IsEqualTo(LocationPrivacyGovernanceReasonCode.InvalidInstanceSetting);
        await _tenantSettings.DidNotReceive().GetByTenantAndKeys(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveAsync_CanceledExactTenantReadPropagatesCancellation()
    {
        _systemSettings.GetAllSettings("LocationPrivacy", Arg.Any<CancellationToken>()).Returns([]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _tenantSettings.GetByTenantAndKeys(
                TenantId,
                Arg.Any<IReadOnlyCollection<string>>(),
                cancellation.Token)
            .Returns(Task.FromCanceled<List<TenantSetting>>(cancellation.Token));

        await Assert.That(async () => await CreateService().ResolveAsync(TenantId, cancellation.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task PublicBoundary_UsesGovernanceAndStableEventLocationRoomMemoKey()
    {
        var governance = Substitute.For<ILocationPrivacyGovernanceService>();
        governance.ResolveAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(EffectiveLocationPrivacyGovernance.FailClosed(LocationPrivacyGovernanceReasonCode.RepositoryUnavailable));
        var boundary = new PublicEventLocationDisclosureEvaluator(governance, new EventLocationDisclosureEvaluator());
        var eventId = Guid.CreateVersion7();
        var eventLocationId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var rawLocation = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            Tenant = null!,
            FullName = "PRIVATE CANARY",
            Country = "PRIVATE CANARY",
            City = "PRIVATE CANARY"
        };
        var input = new PublicEventLocationDisclosureInput(
            TenantId,
            eventId,
            eventLocationId,
            roomId,
            EventLocation: null,
            Location: rawLocation,
            Room: null,
            ServerNowUtc: new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero),
            Derivatives: null);

        var result = await boundary.EvaluateAsync(input);

        await Assert.That(input.MemoKey).IsEqualTo(new PublicEventLocationDisclosureMemoKey(eventLocationId, roomId));
        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Hidden);
        await Assert.That(result.Values).IsNull();
    }

    [Test]
    public async Task PublicBoundary_EmptyScopedIdentifiersFailClosedWithoutThrowing()
    {
        var governance = Substitute.For<ILocationPrivacyGovernanceService>();
        governance.ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(EffectiveLocationPrivacyGovernance.FailClosed(LocationPrivacyGovernanceReasonCode.InvalidTenantId));
        var boundary = new PublicEventLocationDisclosureEvaluator(governance, new EventLocationDisclosureEvaluator());
        Guid validTenantId = Guid.CreateVersion7();
        Guid validEventId = Guid.CreateVersion7();
        Guid validEventLocationId = Guid.CreateVersion7();
        (Guid TenantId, Guid EventId, Guid EventLocationId)[] invalidIdentifiers =
        [
            (Guid.Empty, validEventId, validEventLocationId),
            (validTenantId, Guid.Empty, validEventLocationId),
            (validTenantId, validEventId, Guid.Empty),
            (Guid.Empty, Guid.Empty, Guid.Empty)
        ];

        foreach ((Guid tenantId, Guid eventId, Guid eventLocationId) in invalidIdentifiers)
        {
            var result = await boundary.EvaluateAsync(new(
                tenantId,
                eventId,
                eventLocationId,
                RoomId: null,
                EventLocation: null,
                Location: null,
                Room: null,
                ServerNowUtc: new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero),
                Derivatives: null));

            await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Hidden);
            await Assert.That(result.EventLocationId).IsEqualTo(eventLocationId);
            await Assert.That(result.Values).IsNull();
            await Assert.That(result.DisclosedFields).IsEmpty();
            await Assert.That(result.LocationId).IsNull();
        }
    }

    [Test]
    public async Task PublicBoundary_DefaultAndExplicitRevealUseTheLaterGovernedServerTimeGate()
    {
        DateTime createdAtUtc = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var location = CreateLocation();
        EventLocation defaultPlacement = CreatePlacement(location.Id, createdAtUtc, explicitRevealUtc: null);
        PublicEventLocationDisclosureEvaluator defaultBoundary = CreateBoundary(TimeSpan.FromDays(30));
        PublicEventLocationDisclosureInput defaultInput = CreateInput(
            defaultPlacement,
            location,
            new DateTimeOffset(createdAtUtc.AddDays(30).AddTicks(-1)));

        EventLocationDisclosureResult beforeDefault = await defaultBoundary.EvaluateAsync(defaultInput);
        EventLocationDisclosureResult atDefault = await defaultBoundary.EvaluateAsync(defaultInput with
        {
            ServerNowUtc = new DateTimeOffset(createdAtUtc.AddDays(30))
        });

        await Assert.That(beforeDefault.Values!.StreetAddress).IsNull();
        await Assert.That(atDefault.Values!.StreetAddress).IsEqualTo("1 Main Street");

        EventLocation earlyExplicit = CreatePlacement(location.Id, createdAtUtc, createdAtUtc.AddDays(1));
        EventLocation laterExplicit = CreatePlacement(location.Id, createdAtUtc, createdAtUtc.AddDays(40));
        EventLocationDisclosureResult earlyCannotWiden = await defaultBoundary.EvaluateAsync(CreateInput(
            earlyExplicit,
            location,
            new DateTimeOffset(createdAtUtc.AddDays(2))));
        EventLocationDisclosureResult laterWinsBefore = await defaultBoundary.EvaluateAsync(CreateInput(
            laterExplicit,
            location,
            new DateTimeOffset(createdAtUtc.AddDays(39))));
        EventLocationDisclosureResult laterWinsAt = await defaultBoundary.EvaluateAsync(CreateInput(
            laterExplicit,
            location,
            new DateTimeOffset(createdAtUtc.AddDays(40))));

        await Assert.That(earlyCannotWiden.Values!.StreetAddress).IsNull();
        await Assert.That(laterWinsBefore.Values!.StreetAddress).IsNull();
        await Assert.That(laterWinsAt.Values!.StreetAddress).IsEqualTo("1 Main Street");
    }

    [Test]
    public async Task PublicBoundary_UsesMaximumInstanceTenantRevealOffset()
    {
        ConfigureSettings(
            [
                System(GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress, "true"),
                System(GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset, "\"PT2H\"")
            ],
            [
                Tenant(GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress, "true"),
                Tenant(GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset, "\"P1D\"")
            ]);
        DateTime createdAtUtc = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var location = CreateLocation();
        EventLocation placement = CreatePlacement(location.Id, createdAtUtc, explicitRevealUtc: null);
        var boundary = new PublicEventLocationDisclosureEvaluator(CreateService(), new EventLocationDisclosureEvaluator());

        EventLocationDisclosureResult before = await boundary.EvaluateAsync(CreateInput(
            placement,
            location,
            new DateTimeOffset(createdAtUtc.AddHours(12))));
        EventLocationDisclosureResult at = await boundary.EvaluateAsync(CreateInput(
            placement,
            location,
            new DateTimeOffset(createdAtUtc.AddDays(1))));

        await Assert.That(before.Values!.StreetAddress).IsNull();
        await Assert.That(at.Values!.StreetAddress).IsEqualTo("1 Main Street");
    }

    private LocationPrivacyGovernanceService CreateService() => new(_systemSettings, _tenantSettings);

    private void ConfigureSettings(List<SystemSetting> system, List<TenantSetting> tenant)
    {
        _systemSettings.GetAllSettings("LocationPrivacy", Arg.Any<CancellationToken>()).Returns(system);
        _tenantSettings.GetByTenantAndKeys(
                TenantId,
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(tenant);
    }

    private static readonly IReadOnlySet<string> LocationPrivacyKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        GovernanceSettingKeys.LocationPrivacy.AllowHomeLocations,
        GovernanceSettingKeys.LocationPrivacy.AllowPublicExactAddress,
        GovernanceSettingKeys.LocationPrivacy.AllowPublicCoordinates,
        GovernanceSettingKeys.LocationPrivacy.MinimumHomeAudience,
        GovernanceSettingKeys.LocationPrivacy.DefaultRevealOffset
    };

    private static PublicEventLocationDisclosureEvaluator CreateBoundary(TimeSpan revealOffset)
    {
        var governance = Substitute.For<ILocationPrivacyGovernanceService>();
        governance.ResolveAsync(TenantId, Arg.Any<CancellationToken>()).Returns(new EffectiveLocationPrivacyGovernance(
            IsResolved: true,
            LocationPrivacyGovernanceReasonCode.Resolved,
            AllowHomeLocations: true,
            AllowPublicExactAddress: true,
            AllowPublicCoordinates: true,
            MinimumHomeAudience: LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
            DefaultRevealOffset: revealOffset));
        return new(governance, new EventLocationDisclosureEvaluator());
    }

    private static PublicEventLocationDisclosureInput CreateInput(
        EventLocation placement,
        Location location,
        DateTimeOffset serverNowUtc)
        => new(
            TenantId,
            placement.EventId,
            placement.Id,
            RoomId: null,
            placement,
            location,
            Room: null,
            serverNowUtc,
            Derivatives: null);

    private static EventLocation CreatePlacement(
        Guid locationId,
        DateTime createdAtUtc,
        DateTime? explicitRevealUtc)
    {
        var placement = EventLocation.CreatePhysical(
            TenantId,
            Guid.CreateVersion7(),
            locationId,
            Guid.CreateVersion7(),
            createdAtUtc);
        placement.ChangeDisclosurePolicy(
            EventLocationDisclosureFields.All,
            LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
            explicitRevealUtc,
            placement.PolicyVersion,
            Guid.CreateVersion7(),
            EventLocationDisclosureAuditReasonEnum.OrganizerPolicyChange,
            createdAtUtc);
        SetPrivateProperty(placement, nameof(EventLocation.NeedsPrivacyReview), false);
        return placement;
    }

    private static Location CreateLocation()
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            Tenant = null!,
            FullName = "Community Hall",
            Country = "BE",
            City = "Brussels"
        };
        location.ClassifyAs(LocationKindEnum.CommunityVenue);
        location.SetProviderAddress(
            "1 Main Street",
            "1000",
            Explore.Domain.ValueObjects.GeoCoordinate.Create(50.85, 4.35));
        return location;
    }

    private static void SetPrivateProperty<T>(object target, string propertyName, T value)
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        property.SetValue(target, value);
    }

    private static SystemSetting System(string key, string value) => new()
    {
        Id = Guid.CreateVersion7(),
        SettingKey = key,
        Value = value,
        SettingValueTypeId = (int)SettingValueType.Json
    };

    private static TenantSetting Tenant(string key, string value) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = TenantId,
        Tenant = null!,
        SettingKey = key,
        Value = value
    };
}
