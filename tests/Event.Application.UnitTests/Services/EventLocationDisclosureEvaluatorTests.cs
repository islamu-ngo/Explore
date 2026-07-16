// ABOUTME: Exhaustive pure-policy tests for contextual EventLocation disclosure decisions.
// ABOUTME: Proves ordered fail-closed gates, server-time reveal, field selection, and Private Home redaction.

using System.Collections.Immutable;
using System.Reflection;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Location;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Event.Application.UnitTests.Services;

public sealed class EventLocationDisclosureEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid EventId = Guid.CreateVersion7();
    private static readonly Guid ActorId = Guid.CreateVersion7();
    private static readonly Guid RequesterId = Guid.CreateVersion7();
    private readonly EventLocationDisclosureEvaluator _evaluator = new();

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationDisclosurePurpose.Public)]
    [Arguments(EventLocationDisclosurePurpose.Attendee)]
    [Arguments(EventLocationDisclosurePurpose.Management)]
    public async Task Evaluate_ExplicitTba_SuppressesEveryPhysicalValue(
        EventLocationDisclosurePurpose purpose)
    {
        var placement = EventLocation.CreateToBeAnnounced(TenantId, EventId, ActorId, Now.UtcDateTime);
        var facts = CreateFacts(
            purpose: purpose,
            placement: placement,
            location: null,
            room: null,
            includeRoom: false,
            accessState: purpose == EventLocationDisclosurePurpose.Attendee
                ? EventLocationRegistrationEffectiveState.Confirmed
                : null,
            managerAuthorized: purpose == EventLocationDisclosurePurpose.Management);

        var result = _evaluator.Evaluate(facts);

        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.ToBeAnnounced);
        await Assert.That(result.Values).IsNull();
        await Assert.That(result.LocationId).IsNull();
        await Assert.That(result.DisclosedFields).IsEmpty();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationDisclosurePurpose.Attendee)]
    [Arguments(EventLocationDisclosurePurpose.Management)]
    public async Task Evaluate_ExplicitTba_DoesNotBypassPrivatePurposeAuthority(
        EventLocationDisclosurePurpose purpose)
    {
        var placement = EventLocation.CreateToBeAnnounced(TenantId, EventId, ActorId, Now.UtcDateTime);
        var result = _evaluator.Evaluate(CreateFacts(
            purpose: purpose,
            placement: placement,
            includeRoom: false,
            access: null,
            managerAuthorized: false));

        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Hidden);
        await Assert.That(result.Values).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_MissingStaleOrMismatchedAssociation_FailsClosed()
    {
        var valid = CreateFacts();
        var deleted = CreatePlacement();
        deleted.DetachFinalReference(ActorId, Now.UtcDateTime);
        var wrongLocation = CreateLocation(locationId: Guid.CreateVersion7());
        var wrongRoom = CreateRoom(valid.Location!, locationId: Guid.CreateVersion7());
        var deletedRoom = CreateRoom(valid.Location!);
        deletedRoom.IsDeleted = true;

        EventLocationDisclosureEvaluationFacts[] invalidFacts =
        [
            valid with { EventLocation = null },
            valid with { Request = valid.Request with { TenantId = Guid.CreateVersion7() } },
            valid with { Request = valid.Request with { EventId = Guid.CreateVersion7() } },
            valid with { Request = valid.Request with { EventLocationId = Guid.CreateVersion7() } },
            valid with { EventLocation = deleted },
            valid with { Location = null },
            valid with { Location = wrongLocation },
            valid with { Location = CreateLocation(tenantId: Guid.CreateVersion7()) },
            valid with { Room = null },
            valid with { Room = wrongRoom },
            valid with { Room = deletedRoom }
        ];

        foreach (var facts in invalidFacts)
        {
            var result = _evaluator.Evaluate(facts);
            await Assert.That(result.Values).IsNull();
            await Assert.That(result.DisclosedFields).IsEmpty();
            await Assert.That(result.LocationId).IsNull();
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationDisclosurePurpose.Public)]
    [Arguments(EventLocationDisclosurePurpose.Management)]
    public async Task Evaluate_UnrequestedRoomFact_IsRejectedForPublicAndManagement(
        EventLocationDisclosurePurpose purpose)
    {
        var facts = CreateFacts(
            purpose: purpose,
            includeRoom: false,
            managerAuthorized: purpose == EventLocationDisclosurePurpose.Management);
        var forgedRoom = CreateRoom(facts.Location!);

        var result = _evaluator.Evaluate(facts with { Room = forgedRoom });

        await Assert.That(result.Values).IsNull();
        await Assert.That(result.DisclosedFields).IsEmpty();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationDisclosurePurpose.Public)]
    [Arguments(EventLocationDisclosurePurpose.Management)]
    public async Task Evaluate_CrossTenantOrCrossLocationRoom_IsRejectedForPublicAndManagement(
        EventLocationDisclosurePurpose purpose)
    {
        var facts = CreateFacts(
            purpose: purpose,
            managerAuthorized: purpose == EventLocationDisclosurePurpose.Management);
        var crossTenant = CreateRoom(facts.Location!);
        crossTenant.TenantId = Guid.CreateVersion7();
        var crossLocation = CreateRoom(facts.Location!, Guid.CreateVersion7());

        foreach (var room in new[] { crossTenant, crossLocation })
        {
            var result = _evaluator.Evaluate(facts with
            {
                Request = facts.Request with { RoomId = room.Id },
                Room = room
            });

            await Assert.That(result.Values).IsNull();
            await Assert.That(result.DisclosedFields).IsEmpty();
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_DeletedOrTombstonedRoom_IsRejectedForManagement()
    {
        var facts = CreateFacts(
            purpose: EventLocationDisclosurePurpose.Management,
            managerAuthorized: true);
        var deletedRoom = CreateRoom(facts.Location!);
        deletedRoom.IsDeleted = true;

        var result = _evaluator.Evaluate(facts with
        {
            Request = facts.Request with { RoomId = deletedRoom.Id },
            Room = deletedRoom
        });

        await Assert.That(result.Values).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(LocationPrivacyStateEnum.NotProvided)]
    [Arguments(LocationPrivacyStateEnum.Erased)]
    public async Task Evaluate_UnusablePrivacyState_IsUnavailable(LocationPrivacyStateEnum state)
    {
        var location = state == LocationPrivacyStateEnum.Erased
            ? CreateErasedPrivateHome()
            : CreateLocation(attachPii: false);
        var facts = CreateFacts(location: location, room: null, roomId: null);

        var result = _evaluator.Evaluate(facts);

        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Unavailable);
        await Assert.That(result.Values).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_NeedsPrivacyReview_NeverMaterializesValues()
    {
        var location = CreateLocation();
        var placement = CreatePlacement(locationId: location.Id, needsPrivacyReview: true);

        var result = _evaluator.Evaluate(CreateFacts(placement: placement, location: location));

        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.NeedsPrivacyReview);
        await Assert.That(result.Values).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_UnresolvedGovernanceAndUnknownInputs_FailClosed()
    {
        var valid = CreateFacts();
        EventLocationDisclosureEvaluationFacts[] invalidFacts =
        [
            valid with { Governance = valid.Governance with { IsResolved = false } },
            valid with { Request = valid.Request with { Purpose = (EventLocationDisclosurePurpose)999 } },
            valid with { Governance = valid.Governance with { MinimumHomeAudience = (LocationDisclosureAudienceEnum)999 } },
            valid with { ServerNowUtc = default }
        ];

        foreach (var facts in invalidFacts)
        {
            var result = _evaluator.Evaluate(facts);
            await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Hidden);
            await Assert.That(result.Values).IsNull();
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_Public_AppliesPurposeGovernancePolicyAndDerivativeCeilings()
    {
        var result = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Public,
            revealFromUtc: Now.AddMinutes(-1),
            derivatives: new("Exact formatted", "https://maps.example/exact", "u151")));

        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Available);
        await Assert.That(result.LocationId).IsNull();
        await Assert.That(result.Values).IsEqualTo(new EventLocationDisclosureValues(
            Country: "BE",
            City: "Brussels",
            VenueName: "Community Hall",
            RoomName: "Main room",
            StreetAddress: "1 Main Street",
            Postcode: "1000",
            Latitude: 50.85,
            Longitude: 4.35,
            FormattedAddress: "Exact formatted",
            MapUrl: "https://maps.example/exact",
            Geohash: "u151"));
        await Assert.That(result.Values!.Timezone).IsNull();
        await Assert.That(result.Values.RoomDescription).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_AttendeeAndManagement_UseSeparateAuthorityAndPurposeCeilings()
    {
        var attendee = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Attendee,
            accessState: EventLocationRegistrationEffectiveState.Confirmed));
        var managementFacts = CreateFacts(
            purpose: EventLocationDisclosurePurpose.Management,
            managerAuthorized: true,
            access: null);
        var management = _evaluator.Evaluate(managementFacts);

        await Assert.That(attendee.State).IsEqualTo(EventLocationDisclosureState.Available);
        await Assert.That(attendee.LocationId).IsNull();
        await Assert.That(attendee.Values!.RoomDescription).IsNull();
        await Assert.That(management.State).IsEqualTo(EventLocationDisclosureState.Available);
        await Assert.That(management.LocationId).IsEqualTo(managementFacts.Location!.Id);
        await Assert.That(management.Values!.RoomDescription).IsEqualTo("Use the north entrance");
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_UnauthorizedPrivatePurposes_AreHidden()
    {
        var attendee = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Attendee,
            access: null));
        var management = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Management,
            managerAuthorized: false,
            access: null));

        await Assert.That(attendee.State).IsEqualTo(EventLocationDisclosureState.Hidden);
        await Assert.That(attendee.Values).IsNull();
        await Assert.That(management.State).IsEqualTo(EventLocationDisclosureState.Hidden);
        await Assert.That(management.Values).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_MismatchedRegistrationFact_DoesNotGrantAttendeeAccess()
    {
        var validFacts = CreateFacts(purpose: EventLocationDisclosurePurpose.Attendee);
        var validAccess = CreateAccessForPlacement(
            EventLocationRegistrationEffectiveState.Confirmed,
            EventId,
            validFacts.EventLocation!.Id);
        var otherPlacement = CreatePlacement(locationId: Guid.CreateVersion7());
        var otherAccess = CreateAccessForPlacement(
            EventLocationRegistrationEffectiveState.Confirmed,
            Guid.CreateVersion7(),
            otherPlacement.Id);

        foreach (var access in new[] { validAccess, otherAccess })
        {
            var facts = validFacts with
            {
                Authority = CreateAuthority(
                    EventLocationDisclosureAuthorityKind.AttendeeRegistration,
                    EventLocationDisclosurePurpose.Attendee,
                    RequesterId,
                    TenantId,
                    EventId,
                    validFacts.EventLocation!.Id,
                    access)
            };
            if (ReferenceEquals(access, validAccess))
            {
                facts = facts with { Request = facts.Request with { EventId = Guid.CreateVersion7() } };
            }

            var result = _evaluator.Evaluate(facts);
            await Assert.That(result.Values).IsNull();
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_AttendeeAuthorityForAlice_CannotBeSubstitutedForBob()
    {
        var aliceFacts = CreateFacts(
            purpose: EventLocationDisclosurePurpose.Attendee,
            accessState: EventLocationRegistrationEffectiveState.Confirmed);

        var result = _evaluator.Evaluate(aliceFacts with
        {
            Request = aliceFacts.Request with { RequesterUserId = Guid.CreateVersion7() }
        });

        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Hidden);
        await Assert.That(result.Values).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_ManagementAuthorityMustMatchEveryScopedDimension()
    {
        var facts = CreateFacts(
            purpose: EventLocationDisclosurePurpose.Management,
            managerAuthorized: true);
        EventLocationDisclosureAuthorityFact[] substitutions =
        [
            CreateAuthority(
                EventLocationDisclosureAuthorityKind.Management,
                EventLocationDisclosurePurpose.Management,
                Guid.CreateVersion7(),
                TenantId,
                EventId,
                facts.EventLocation!.Id,
                null),
            CreateAuthority(
                EventLocationDisclosureAuthorityKind.Management,
                EventLocationDisclosurePurpose.Management,
                RequesterId,
                Guid.CreateVersion7(),
                EventId,
                facts.EventLocation.Id,
                null),
            CreateAuthority(
                EventLocationDisclosureAuthorityKind.Management,
                EventLocationDisclosurePurpose.Management,
                RequesterId,
                TenantId,
                Guid.CreateVersion7(),
                facts.EventLocation.Id,
                null),
            CreateAuthority(
                EventLocationDisclosureAuthorityKind.Management,
                EventLocationDisclosurePurpose.Management,
                RequesterId,
                TenantId,
                EventId,
                Guid.CreateVersion7(),
                null)
        ];

        foreach (var substitution in substitutions)
        {
            var result = _evaluator.Evaluate(facts with { Authority = substitution });
            await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Hidden);
            await Assert.That(result.Values).IsNull();
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_NullOrUnknownAuthorityFact_FailsClosed()
    {
        var facts = CreateFacts(
            purpose: EventLocationDisclosurePurpose.Management,
            managerAuthorized: true);
        var unknown = CreateAuthority(
            (EventLocationDisclosureAuthorityKind)999,
            EventLocationDisclosurePurpose.Management,
            RequesterId,
            TenantId,
            EventId,
            facts.EventLocation!.Id,
            null);

        foreach (var authority in new EventLocationDisclosureAuthorityFact?[] { null, unknown })
        {
            var result = _evaluator.Evaluate(facts with { Authority = authority });
            await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Hidden);
            await Assert.That(result.Values).IsNull();
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task AuthorityFact_HasNoPublicConstructionFactoryOrMutationSurface()
    {
        var type = typeof(EventLocationDisclosureAuthorityFact);
        var publicFactories = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.ReturnType == type)
            .ToArray();
        var writableProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is not null)
            .ToArray();

        await Assert.That(type.IsSealed).IsTrue();
        await Assert.That(type.GetConstructors()).IsEmpty();
        await Assert.That(publicFactories).IsEmpty();
        await Assert.That(writableProperties).IsEmpty();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationRegistrationEffectiveState.Pending, LocationDisclosureAudienceEnum.AnyCurrentRegistrant, true)]
    [Arguments(EventLocationRegistrationEffectiveState.Waitlisted, LocationDisclosureAudienceEnum.AnyCurrentRegistrant, true)]
    [Arguments(EventLocationRegistrationEffectiveState.Pending, LocationDisclosureAudienceEnum.ConfirmedParticipant, false)]
    [Arguments(EventLocationRegistrationEffectiveState.Confirmed, LocationDisclosureAudienceEnum.ConfirmedParticipant, true)]
    public async Task Evaluate_AttendeeExactFields_RespectAudienceCeiling(
        EventLocationRegistrationEffectiveState state,
        LocationDisclosureAudienceEnum audience,
        bool expectsExact)
    {
        var result = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Attendee,
            audience: audience,
            accessState: state));

        await Assert.That(result.Values!.StreetAddress is not null).IsEqualTo(expectsExact);
        await Assert.That(result.Values.Postcode is not null).IsEqualTo(expectsExact);
        await Assert.That(result.Values.Latitude is not null).IsEqualTo(expectsExact);
        await Assert.That(result.Values.Longitude is not null).IsEqualTo(expectsExact);
        await Assert.That(result.Values.VenueName).IsEqualTo("Community Hall");
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_RevealUsesOnlyProvidedServerUtcFact()
    {
        var reveal = Now.AddMinutes(1);
        var before = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Attendee,
            revealFromUtc: reveal,
            serverNowUtc: reveal.AddTicks(-1),
            accessState: EventLocationRegistrationEffectiveState.Confirmed));
        var at = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Attendee,
            revealFromUtc: reveal,
            serverNowUtc: reveal,
            accessState: EventLocationRegistrationEffectiveState.Confirmed));

        await Assert.That(before.Values!.StreetAddress).IsNull();
        await Assert.That(before.Values.VenueName).IsEqualTo("Community Hall");
        await Assert.That(at.Values!.StreetAddress).IsEqualTo("1 Main Street");
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationDisclosureFields.Country, EventLocationDisclosureField.Country)]
    [Arguments(EventLocationDisclosureFields.City, EventLocationDisclosureField.City)]
    [Arguments(EventLocationDisclosureFields.VenueName, EventLocationDisclosureField.VenueName)]
    [Arguments(EventLocationDisclosureFields.RoomName, EventLocationDisclosureField.RoomName)]
    [Arguments(EventLocationDisclosureFields.StreetAddress, EventLocationDisclosureField.StreetAddress)]
    [Arguments(EventLocationDisclosureFields.Postcode, EventLocationDisclosureField.Postcode)]
    public async Task Evaluate_FieldPolicyEmitsOnlySelectedField(
        EventLocationDisclosureFields selection,
        EventLocationDisclosureField expected)
    {
        var result = _evaluator.Evaluate(CreateFacts(fields: selection));

        await Assert.That(result.DisclosedFields).IsEquivalentTo([expected]);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_CoordinatesRequireAValidCompletePairAndDerivativesFollowSourceAuthority()
    {
        var location = CreateLocation();
        location.Pii!.Longitude = null;
        var result = _evaluator.Evaluate(CreateFacts(
            location: location,
            room: null,
            roomId: null,
            fields: EventLocationDisclosureFields.Coordinates,
            derivatives: new(null, "map", "geohash")));

        await Assert.That(result.Values).IsNull();

        location.Pii.Longitude = 181;
        result = _evaluator.Evaluate(CreateFacts(
            location: location,
            room: null,
            roomId: null,
            fields: EventLocationDisclosureFields.Coordinates,
            derivatives: new(null, "map", "geohash")));
        await Assert.That(result.Values).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_WhitespaceSourceValuesAreNotDisclosed()
    {
        var location = CreateLocation(fullName: " ", city: "\t", country: "", address: " ", postcode: " ");
        var result = _evaluator.Evaluate(CreateFacts(location: location, room: null, roomId: null));

        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.Unavailable);
        await Assert.That(result.Values).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_PrivateHomePublic_IsAlwaysGenericAndNeverExact()
    {
        var location = CreatePrivateHome();
        var result = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Public,
            location: location,
            room: CreateRoom(location),
            revealFromUtc: Now.AddDays(-1),
            derivatives: new("private", "private", "private")));

        await Assert.That(result.State).IsEqualTo(EventLocationDisclosureState.PrivateVenue);
        await Assert.That(result.Values).IsEqualTo(new EventLocationDisclosureValues(
            VenueName: EventLocationDisclosureContract.PrivateHomePublicLabel));
        await Assert.That(result.DisclosedFields).IsEquivalentTo([EventLocationDisclosureField.VenueName]);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_PrivateHome_DefaultConfirmedParticipantBehavior()
    {
        var location = CreatePrivateHome();
        var room = CreateRoom(location);
        var pending = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Attendee,
            location: location,
            room: room,
            audience: LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
            minimumHomeAudience: LocationDisclosureAudienceEnum.ConfirmedParticipant,
            accessState: EventLocationRegistrationEffectiveState.Pending));
        var confirmed = _evaluator.Evaluate(CreateFacts(
            purpose: EventLocationDisclosurePurpose.Attendee,
            location: location,
            room: room,
            audience: LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
            minimumHomeAudience: LocationDisclosureAudienceEnum.ConfirmedParticipant,
            accessState: EventLocationRegistrationEffectiveState.Confirmed));

        await Assert.That(pending.State).IsEqualTo(EventLocationDisclosureState.PrivateVenue);
        await Assert.That(pending.Values!.VenueName).IsEqualTo(EventLocationDisclosureContract.PrivateHomePublicLabel);
        await Assert.That(pending.Values.StreetAddress).IsNull();
        await Assert.That(confirmed.State).IsEqualTo(EventLocationDisclosureState.Available);
        await Assert.That(confirmed.Values!.VenueName).IsEqualTo("Family residence");
        await Assert.That(confirmed.Values.StreetAddress).IsEqualTo("9 Household Lane");
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_HomeGovernanceDisabled_FailsClosedForEveryPurpose()
    {
        var location = CreatePrivateHome();
        foreach (var purpose in Enum.GetValues<EventLocationDisclosurePurpose>())
        {
            var result = _evaluator.Evaluate(CreateFacts(
                purpose: purpose,
                location: location,
                room: null,
                roomId: null,
                allowHomeLocations: false,
                accessState: purpose == EventLocationDisclosurePurpose.Attendee
                    ? EventLocationRegistrationEffectiveState.Confirmed
                    : null,
                managerAuthorized: purpose == EventLocationDisclosurePurpose.Management));

            await Assert.That(result.Values).IsNull();
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_PublicExactGovernanceFiltersAddressAndCoordinatesIndependently()
    {
        var noAddress = _evaluator.Evaluate(CreateFacts(
            allowPublicExactAddress: false,
            allowPublicCoordinates: true));
        var noCoordinates = _evaluator.Evaluate(CreateFacts(
            allowPublicExactAddress: true,
            allowPublicCoordinates: false));

        await Assert.That(noAddress.Values!.StreetAddress).IsNull();
        await Assert.That(noAddress.Values.Postcode).IsNull();
        await Assert.That(noAddress.Values.Latitude).IsEqualTo(50.85);
        await Assert.That(noCoordinates.Values!.StreetAddress).IsEqualTo("1 Main Street");
        await Assert.That(noCoordinates.Values.Latitude).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_UnknownKindPrivacyStateAndAudience_NeverMaterializeExactValues()
    {
        var unknownKind = CreateLocation();
        SetPrivateProperty(unknownKind, nameof(Location.LocationKindId), 999);
        var unknownState = CreateLocation();
        SetPrivateProperty(unknownState, nameof(Location.LocationPrivacyStateId), 999);
        var unknownAudienceLocation = CreateLocation();
        var unknownAudience = CreatePlacement(locationId: unknownAudienceLocation.Id);
        SetPrivateProperty(unknownAudience, nameof(EventLocation.FullDetailsAudienceId), 999);

        foreach (var facts in new[]
        {
            CreateFacts(location: unknownKind, room: null, roomId: null),
            CreateFacts(location: unknownState, room: null, roomId: null),
            CreateFacts(placement: unknownAudience, location: unknownAudienceLocation)
        })
        {
            var result = _evaluator.Evaluate(facts);
            await Assert.That(result.Values).IsNull();
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_UnsupportedTimezoneAndOperationalSecretsRemainOutsideEveryResult()
    {
        foreach (var purpose in Enum.GetValues<EventLocationDisclosurePurpose>())
        {
            var result = _evaluator.Evaluate(CreateFacts(
                purpose: purpose,
                accessState: purpose == EventLocationDisclosurePurpose.Attendee
                    ? EventLocationRegistrationEffectiveState.Confirmed
                    : null,
                managerAuthorized: purpose == EventLocationDisclosurePurpose.Management));

            if (result.Values is not null)
            {
                await Assert.That(result.Values.Timezone).IsNull();
            }

            await Assert.That(result.DisclosedFields).DoesNotContain(EventLocationDisclosureField.Timezone);
            await Assert.That(result.DisclosedFields).DoesNotContain(EventLocationDisclosureField.AccessInstructions);
            await Assert.That(result.DisclosedFields).DoesNotContain(EventLocationDisclosureField.EntryDetails);
            await Assert.That(result.DisclosedFields).DoesNotContain(EventLocationDisclosureField.DoorCode);
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Evaluate_SameImmutableFacts_AreDeterministic()
    {
        var facts = CreateFacts(derivatives: new("formatted", "map", "geohash"));

        var first = _evaluator.Evaluate(facts);
        var second = _evaluator.Evaluate(facts);

        await Assert.That(second.EventLocationId).IsEqualTo(first.EventLocationId);
        await Assert.That(second.Purpose).IsEqualTo(first.Purpose);
        await Assert.That(second.State).IsEqualTo(first.State);
        await Assert.That(second.LocationId).IsEqualTo(first.LocationId);
        await Assert.That(second.Values).IsEqualTo(first.Values);
        await Assert.That(second.DisclosedFields).IsEquivalentTo(first.DisclosedFields);
    }

    private static EventLocationDisclosureEvaluationFacts CreateFacts(
        EventLocationDisclosurePurpose purpose = EventLocationDisclosurePurpose.Public,
        EventLocation? placement = null,
        Location? location = null,
        LocationRoom? room = null,
        Guid? roomId = null,
        bool includeRoom = true,
        EventLocationDisclosureFields fields = EventLocationDisclosureFields.All,
        LocationDisclosureAudienceEnum audience = LocationDisclosureAudienceEnum.ConfirmedParticipant,
        DateTimeOffset? revealFromUtc = null,
        DateTimeOffset? serverNowUtc = null,
        EventLocationRegistrationAccess? access = null,
        EventLocationRegistrationEffectiveState? accessState = null,
        bool managerAuthorized = false,
        bool allowHomeLocations = true,
        bool allowPublicExactAddress = true,
        bool allowPublicCoordinates = true,
        LocationDisclosureAudienceEnum minimumHomeAudience = LocationDisclosureAudienceEnum.ConfirmedParticipant,
        EventLocationDisclosureDerivativeValues? derivatives = null)
    {
        location ??= CreateLocation();
        placement ??= CreatePlacement(
            locationId: location.Id,
            fields: fields,
            audience: audience,
            revealFromUtc: revealFromUtc,
            needsPrivacyReview: false);
        room = includeRoom ? room ?? CreateRoom(location) : null;
        var effectiveRoomId = includeRoom ? roomId ?? room?.Id : roomId;
        Guid? requesterId = purpose == EventLocationDisclosurePurpose.Public ? null : RequesterId;
        access ??= accessState.HasValue
            ? CreateAccessForPlacement(accessState.Value, EventId, placement.Id)
            : null;
        EventLocationDisclosureAuthorityFact? authority = purpose switch
        {
            EventLocationDisclosurePurpose.Public => CreateAuthority(
                EventLocationDisclosureAuthorityKind.Public,
                purpose,
                null,
                TenantId,
                EventId,
                placement.Id,
                null),
            EventLocationDisclosurePurpose.Attendee when access is not null => CreateAuthority(
                EventLocationDisclosureAuthorityKind.AttendeeRegistration,
                purpose,
                requesterId,
                TenantId,
                EventId,
                placement.Id,
                access),
            EventLocationDisclosurePurpose.Management when managerAuthorized => CreateAuthority(
                EventLocationDisclosureAuthorityKind.Management,
                purpose,
                requesterId,
                TenantId,
                EventId,
                placement.Id,
                null),
            _ => null
        };
        return new(
            new EventLocationDisclosureRequest(
                TenantId,
                EventId,
                placement.Id,
                effectiveRoomId,
                requesterId,
                purpose),
            placement,
            location,
            room,
            new EventLocationDisclosureGovernanceFact(
                true,
                allowHomeLocations,
                allowPublicExactAddress,
                allowPublicCoordinates,
                minimumHomeAudience),
            authority,
            serverNowUtc ?? Now,
            derivatives);
    }

    private static EventLocationDisclosureAuthorityFact CreateAuthority(
        EventLocationDisclosureAuthorityKind kind,
        EventLocationDisclosurePurpose purpose,
        Guid? requesterUserId,
        Guid tenantId,
        Guid eventId,
        Guid eventLocationId,
        EventLocationRegistrationAccess? registrationAccess)
    {
        var constructor = typeof(EventLocationDisclosureAuthorityFact)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();
        return (EventLocationDisclosureAuthorityFact)constructor.Invoke(
        [
            kind,
            purpose,
            requesterUserId,
            tenantId,
            eventId,
            eventLocationId,
            registrationAccess
        ]);
    }

    private static EventLocation CreatePlacement(
        Guid? locationId = null,
        EventLocationDisclosureFields fields = EventLocationDisclosureFields.All,
        LocationDisclosureAudienceEnum audience = LocationDisclosureAudienceEnum.ConfirmedParticipant,
        DateTimeOffset? revealFromUtc = null,
        bool needsPrivacyReview = false)
    {
        var placement = EventLocation.CreatePhysical(
            TenantId,
            EventId,
            locationId ?? Guid.CreateVersion7(),
            ActorId,
            Now.UtcDateTime);
        placement.ChangeDisclosurePolicy(
            fields,
            audience,
            revealFromUtc?.UtcDateTime,
            1,
            ActorId,
            EventLocationDisclosureAuditReasonEnum.OrganizerPolicyChange,
            Now.UtcDateTime);
        SetPrivateProperty(placement, nameof(EventLocation.NeedsPrivacyReview), needsPrivacyReview);
        return placement;
    }

    private static Location CreateLocation(
        Guid? tenantId = null,
        Guid? locationId = null,
        string fullName = "Community Hall",
        string country = "BE",
        string city = "Brussels",
        string address = "1 Main Street",
        string postcode = "1000",
        bool attachPii = true)
    {
        var location = new Location
        {
            Id = locationId ?? Guid.CreateVersion7(),
            TenantId = tenantId ?? TenantId,
            Tenant = null!,
            FullName = fullName,
            Country = country,
            City = city,
            Timezone = "Europe/Brussels"
        };
        location.ClassifyAs(LocationKindEnum.CommunityVenue);
        if (attachPii)
        {
            location.AttachPii(new LocationPii
            {
                Address = address,
                Postcode = postcode,
                Latitude = 50.85,
                Longitude = 4.35
            });
        }

        return location;
    }

    private static Location CreatePrivateHome()
    {
        var location = CreateLocation(fullName: "Family residence", address: "9 Household Lane");
        location.ClassifyAsPrivateHome(RequesterId);
        return location;
    }

    private static Location CreateErasedPrivateHome()
    {
        var location = CreatePrivateHome();
        location.EraseOwnedPii(Now.UtcDateTime, LocationPrivacyErasureReasonEnum.AccountDeletion);
        return location;
    }

    private static LocationRoom CreateRoom(Location location, Guid? locationId = null)
        => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = location.TenantId,
            Tenant = null!,
            LocationId = locationId ?? location.Id,
            Location = location,
            Name = "Main room",
            Description = "Use the north entrance"
        };

    private static EventLocationRegistrationAccess CreateAccessForPlacement(
        EventLocationRegistrationEffectiveState state,
        Guid eventId,
        Guid eventLocationId)
    {
        var approvalStatusId = state switch
        {
            EventLocationRegistrationEffectiveState.Pending => (int)ApprovalStatusEnum.Pending,
            EventLocationRegistrationEffectiveState.Waitlisted => (int)ApprovalStatusEnum.Waitlisted,
            EventLocationRegistrationEffectiveState.Confirmed => (int)ApprovalStatusEnum.Approved,
            EventLocationRegistrationEffectiveState.Rejected => (int)ApprovalStatusEnum.Rejected,
            EventLocationRegistrationEffectiveState.Cancelled => (int)ApprovalStatusEnum.Cancelled,
            EventLocationRegistrationEffectiveState.Revoked => (int)ApprovalStatusEnum.Revoked,
            _ => 999
        };
        var intentId = Guid.CreateVersion7();
        return new EventLocationRegistrationAccessService().Resolve(new(
            eventLocationId,
            Now,
            new EventLocationRegistrationIntentFact(
                intentId,
                eventId,
                RegistrationScopeEnum.Event,
                null,
                null,
                false,
                Now.AddDays(1)),
            ImmutableArray.Create(new EventLocationRegistrationCoverageFact(
                intentId,
                eventId,
                null,
                Guid.CreateVersion7(),
                eventLocationId,
                approvalStatusId,
                (int)RegistrationModeEnum.Open,
                false,
                Now.AddDays(1)))));
    }

    private static void SetPrivateProperty<T>(T target, string propertyName, object value)
        where T : class
        => typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(target, value);
}
