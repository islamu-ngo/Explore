// ABOUTME: Covers Application-layer promotion apply/remove orchestration without persistence or API dependencies.
// ABOUTME: Verifies generic failure responses and exact order repricing through Domain promotion primitives.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Features.Promotions.Handlers.Commands;
using Explore.Application.Features.Promotions.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;

namespace ApplicationUnitTests.Features.Promotions.Commands;

public sealed class PromotionRedemptionCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IRegistrationInventoryRepository _inventory = Substitute.For<IRegistrationInventoryRepository>();
    private readonly IPromotionRedemptionRepository _promotions = Substitute.For<IPromotionRedemptionRepository>();
    private readonly IPlatformFeePolicyRepository _feePolicies = Substitute.For<IPlatformFeePolicyRepository>();
    private readonly IPromotionCodeDigestService _digests = Substitute.For<IPromotionCodeDigestService>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly TimeProvider _time = new FixedTimeProvider(UtcNow);

    public PromotionRedemptionCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task ApplyAsyncWhenCodeIsValidDiscountsBeforeFeeAndDoesNotLeakLookupSecrets()
    {
        PromotionScenario scenario = CreateScenario(accountUserId: Guid.CreateVersion7(), includeVerifiedEmail: false);
        PromotionCodeDigest digest = new(7, "digest-v7");

        _inventory.GetOrderForUpdateWithPiiAsync(scenario.Order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(scenario.Order);
        _promotions.GetDistinctLookupKeyVersionsAsync(_tenantId, scenario.Order.EventId, scenario.Order.TicketCatalogVersionId, Arg.Any<CancellationToken>()).Returns([7]);
        _digests.ComputeCandidatesAsync(_tenantId, scenario.Order.EventId, "SAVE10", Arg.Is<IReadOnlyCollection<int>>(versions => versions.Contains(7)), Arg.Any<CancellationToken>())
            .Returns([digest]);
        _promotions.GetCodeForUpdateAsync(
                _tenantId,
                scenario.Order.EventId,
                scenario.Order.TicketCatalogVersionId,
                Arg.Is<IReadOnlyCollection<PromotionCodeDigest>>(candidates => candidates.Single().Value == "digest-v7"),
                Arg.Any<CancellationToken>())
            .Returns(new PromotionCodeMatch(scenario.Code, scenario.Definition));
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, scenario.Order.Id, Arg.Any<CancellationToken>()).Returns((PromotionReservation?)null);
        _promotions.GetTotalActiveOrConsumedCountAsync(_tenantId, scenario.Definition.Id, Arg.Any<CancellationToken>()).Returns(0);
        _promotions.GetVerifiedPurchaserActiveOrConsumedCountAsync(
                _tenantId,
                scenario.Definition.Id,
                Arg.Is<VerifiedPurchaserIdentity>(identity => identity.Kind == nameof(VerifiedPurchaserIdentity.Account) && identity.Value == scenario.Order.AccountUserId!.Value.ToString("D")),
                Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = CreateApplyHandler();

        PromotionRedemptionResponseDto response = await handler.Handle(
            new ApplyPromotionCodeToRegistrationOrderCommand(scenario.Order.Id, "SAVE10"),
            CancellationToken.None);

        await _promotions.Received(1).AddReservationAsync(Arg.Is<PromotionReservation>(reservation => reservation.RegistrationOrderId == scenario.Order.Id), Arg.Any<CancellationToken>());
        await _inventory.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _digests.DidNotReceive().ComputeActiveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.PromotionDiscountTotalMinor).IsEqualTo(1_000);
        await Assert.That(response.TotalDueMinor).IsEqualTo(9_000);
        await Assert.That(response.AppliedPromotionDisplayLabel).IsEqualTo(scenario.Code.DisplayLabel);
        await Assert.That(response.Message).DoesNotContain("SAVE10");
        await Assert.That(response.Message).DoesNotContain("digest");
        await Assert.That(response.FailureCode).IsNull();
        await Assert.That(scenario.Order.ActivePromotionReservationId).IsNotNull();
    }

    [Test]
    public async Task ApplyAsyncWhenCodeCannotBeMatchedReturnsGenericUnavailableFailure()
    {
        PromotionScenario scenario = CreateScenario(accountUserId: Guid.CreateVersion7(), includeVerifiedEmail: false);
        _inventory.GetOrderForUpdateWithPiiAsync(scenario.Order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(scenario.Order);
        _promotions.GetDistinctLookupKeyVersionsAsync(_tenantId, scenario.Order.EventId, scenario.Order.TicketCatalogVersionId, Arg.Any<CancellationToken>()).Returns([3]);
        _digests.ComputeCandidatesAsync(_tenantId, scenario.Order.EventId, "WRONG", Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns([new PromotionCodeDigest(3, "digest")]);
        _promotions.GetCodeForUpdateAsync(_tenantId, scenario.Order.EventId, scenario.Order.TicketCatalogVersionId, Arg.Any<IReadOnlyCollection<PromotionCodeDigest>>(), Arg.Any<CancellationToken>())
            .Returns((PromotionCodeMatch?)null);

        PromotionRedemptionResponseDto response = await CreateApplyHandler().Handle(
            new ApplyPromotionCodeToRegistrationOrderCommand(scenario.Order.Id, "WRONG"),
            CancellationToken.None);

        await _promotions.DidNotReceive().AddReservationAsync(Arg.Any<PromotionReservation>(), Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo(PromotionRedemptionFailureCodes.Unavailable);
        await Assert.That(response.Errors).Contains(PromotionRedemptionFailureCodes.Unavailable);
        await Assert.That(response.Message).DoesNotContain("WRONG");
        await Assert.That(response.Message).DoesNotContain("not found");
        await Assert.That(response.Message).DoesNotContain("expired");
        await Assert.That(response.Message).DoesNotContain("exhausted");
    }

    [Test]
    public async Task ApplyAsync_InvalidExhaustedAndWrongTenantCodesHaveIdenticalSafeObservable()
    {
        PromotionScenario scenario = CreateScenario(accountUserId: Guid.CreateVersion7(), includeVerifiedEmail: false);
        var foreignTenantId = Guid.CreateVersion7();
        const string invalidCode = "INVALID-RAW-CODE";
        const string exhaustedCode = "EXHAUSTED-RAW-CODE";
        const string wrongTenantCode = "WRONG-TENANT-RAW-CODE";
        const string foreignDigest = "foreign-tenant-digest";
        _inventory.GetOrderForUpdateWithPiiAsync(scenario.Order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(scenario.Order);
        _promotions.GetDistinctLookupKeyVersionsAsync(
                _tenantId,
                scenario.Order.EventId,
                scenario.Order.TicketCatalogVersionId,
                Arg.Any<CancellationToken>())
            .Returns([7]);
        _digests.ComputeCandidatesAsync(
                _tenantId,
                scenario.Order.EventId,
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<int>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string digest = call.ArgAt<string>(2) switch
                {
                    exhaustedCode => "exhausted-digest",
                    wrongTenantCode => foreignDigest,
                    _ => "invalid-digest"
                };
                return Task.FromResult<IReadOnlyList<PromotionCodeDigest>>([new PromotionCodeDigest(7, digest)]);
            });
        _promotions.GetCodeForUpdateAsync(
                _tenantId,
                scenario.Order.EventId,
                scenario.Order.TicketCatalogVersionId,
                Arg.Any<IReadOnlyCollection<PromotionCodeDigest>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<IReadOnlyCollection<PromotionCodeDigest>>(3).Single().Value == "exhausted-digest"
                ? new PromotionCodeMatch(scenario.Code, scenario.Definition)
                : null);
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, scenario.Order.Id, Arg.Any<CancellationToken>())
            .Returns((PromotionReservation?)null);
        _promotions.GetTotalActiveOrConsumedCountAsync(_tenantId, scenario.Definition.Id, Arg.Any<CancellationToken>())
            .Returns(10);
        _promotions.GetVerifiedPurchaserActiveOrConsumedCountAsync(
                _tenantId,
                scenario.Definition.Id,
                Arg.Any<VerifiedPurchaserIdentity>(),
                Arg.Any<CancellationToken>())
            .Returns(0);
        ApplyPromotionCodeToRegistrationOrderCommandHandler handler = CreateApplyHandler();

        PromotionRedemptionResponseDto invalid = await handler.Handle(
            new ApplyPromotionCodeToRegistrationOrderCommand(scenario.Order.Id, invalidCode),
            CancellationToken.None);
        PromotionRedemptionResponseDto exhausted = await handler.Handle(
            new ApplyPromotionCodeToRegistrationOrderCommand(scenario.Order.Id, exhaustedCode),
            CancellationToken.None);
        PromotionRedemptionResponseDto wrongTenant = await handler.Handle(
            new ApplyPromotionCodeToRegistrationOrderCommand(scenario.Order.Id, wrongTenantCode),
            CancellationToken.None);

        string invalidJson = JsonSerializer.Serialize(invalid);
        string exhaustedJson = JsonSerializer.Serialize(exhausted);
        string wrongTenantJson = JsonSerializer.Serialize(wrongTenant);
        await Assert.That(new[] { invalid.Success, exhausted.Success, wrongTenant.Success })
            .IsEquivalentTo([false, false, false]);
        await Assert.That(new[] { invalidJson, exhaustedJson, wrongTenantJson })
            .IsEquivalentTo([invalidJson, invalidJson, invalidJson]);
        await Assert.That(invalidJson).DoesNotContain(invalidCode);
        await Assert.That(invalidJson).DoesNotContain(exhaustedCode);
        await Assert.That(invalidJson).DoesNotContain(wrongTenantCode);
        await Assert.That(invalidJson).DoesNotContain(foreignDigest);
        await Assert.That(invalidJson).DoesNotContain(_tenantId.ToString("D"));
        await Assert.That(invalidJson).DoesNotContain(foreignTenantId.ToString("D"));
        await _promotions.Received(3).GetCodeForUpdateAsync(
            _tenantId,
            scenario.Order.EventId,
            scenario.Order.TicketCatalogVersionId,
            Arg.Any<IReadOnlyCollection<PromotionCodeDigest>>(),
            Arg.Any<CancellationToken>());
        await _promotions.DidNotReceive().GetCodeForUpdateAsync(
            foreignTenantId,
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<PromotionCodeDigest>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyAsyncUsesVerifiedEmailBeforeActorWhenNoAccountUserExists()
    {
        PromotionScenario scenario = CreateScenario(accountUserId: null, includeVerifiedEmail: true);
        _inventory.GetOrderForUpdateWithPiiAsync(scenario.Order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(scenario.Order);
        _promotions.GetDistinctLookupKeyVersionsAsync(_tenantId, scenario.Order.EventId, scenario.Order.TicketCatalogVersionId, Arg.Any<CancellationToken>()).Returns([2]);
        _digests.ComputeCandidatesAsync(_tenantId, scenario.Order.EventId, "EMAIL", Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns([new PromotionCodeDigest(2, "digest")]);
        _promotions.GetCodeForUpdateAsync(_tenantId, scenario.Order.EventId, scenario.Order.TicketCatalogVersionId, Arg.Any<IReadOnlyCollection<PromotionCodeDigest>>(), Arg.Any<CancellationToken>())
            .Returns(new PromotionCodeMatch(scenario.Code, scenario.Definition));
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, scenario.Order.Id, Arg.Any<CancellationToken>()).Returns((PromotionReservation?)null);
        _promotions.GetTotalActiveOrConsumedCountAsync(_tenantId, scenario.Definition.Id, Arg.Any<CancellationToken>()).Returns(0);

        await CreateApplyHandler().Handle(new ApplyPromotionCodeToRegistrationOrderCommand(scenario.Order.Id, "EMAIL"), CancellationToken.None);

        await _promotions.Received(1).GetVerifiedPurchaserActiveOrConsumedCountAsync(
            _tenantId,
            scenario.Definition.Id,
            Arg.Is<VerifiedPurchaserIdentity>(identity => identity.Kind == nameof(VerifiedPurchaserIdentity.Email) && identity.Value == "BUYER@EXAMPLE.TEST"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoveAsyncWhenPromotionIsActiveReleasesReservationAndRestoresTotals()
    {
        PromotionScenario scenario = CreateScenario(accountUserId: Guid.CreateVersion7(), includeVerifiedEmail: false);
        PromotionReservation reservation = PromotionReservation.Reserve(Guid.CreateVersion7(), scenario.Order, scenario.Definition, scenario.Code, UtcNow);
        scenario.Order.ApplyPromotion(reservation, scenario.Definition, scenario.Code, UtcNow, 0, 0, feePolicy: null);

        _inventory.GetOrderForUpdateWithLinesAsync(scenario.Order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(scenario.Order);
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, scenario.Order.Id, Arg.Any<CancellationToken>()).Returns(reservation);

        PromotionRedemptionResponseDto response = await CreateRemoveHandler().Handle(
            new RemovePromotionFromRegistrationOrderCommand(scenario.Order.Id),
            CancellationToken.None);

        await _inventory.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.PromotionDiscountTotalMinor).IsEqualTo(0);
        await Assert.That(response.TotalDueMinor).IsEqualTo(10_000);
        await Assert.That(scenario.Order.ActivePromotionReservationId).IsNull();
        await Assert.That(reservation.PromotionReservationStatusId).IsEqualTo((int)PromotionReservationStatusEnum.Released);
    }

    private ApplyPromotionCodeToRegistrationOrderCommandHandler CreateApplyHandler(IUnitOfWork? unitOfWork = null) =>
        new(_inventory, _promotions, _feePolicies, _digests, _tenant, _time, unitOfWork ?? new InlineUnitOfWork());

    private RemovePromotionFromRegistrationOrderCommandHandler CreateRemoveHandler(IUnitOfWork? unitOfWork = null) =>
        new(_inventory, _promotions, _feePolicies, _tenant, _time, unitOfWork ?? new InlineUnitOfWork());

    private PromotionScenario CreateScenario(Guid? accountUserId, bool includeVerifiedEmail)
    {
        Guid eventId = Guid.CreateVersion7();
        Guid ticketTypeId = Guid.CreateVersion7();
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, eventId, "EUR", 4);
        EventTicketType ticketType = EventTicketType.Create(
            ticketTypeId,
            _tenantId,
            catalog.Id,
            "Admission",
            "EUR",
            TicketPricingModeEnum.Fixed,
            fixedPriceMinor: 10_000,
            minimumPriceMinor: null,
            suggestedPriceMinor: null,
            ParticipantDataCollectionModeEnum.LeadBookerOnly,
            capacityPoolId: null,
            minimumAge: null,
            maximumAge: null,
            requiresGuardian: false,
            requiresApproval: false,
            perOrderLimit: null,
            perAccountLimit: null,
            perVerifiedContactLimit: null,
            perBookingPartyLimit: null);
        catalog.AddTicketType(ticketType, capacityPool: null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEvent(ticketType.Id, _tenantId, eventId, 1));
        catalog.UpdateCommercialDisclosures("merchant", "refund", "support");
        catalog.Publish();

        RegistrationOrder order = RegistrationOrder.Create(
            Guid.CreateVersion7(),
            _tenantId,
            eventId,
            accountUserId,
            purchaserActorId: Guid.CreateVersion7(),
            BookingPartyTypeEnum.Individual,
            catalog.Id,
            RegistrationParticipationSnapshot.Create(
                Guid.CreateVersion7(),
                (int)ParticipationHandlingModeEnum.PlatformManaged,
                (int)AdvanceRegistrationObligationEnum.Required,
                (int)IdentityAccessModeEnum.CapabilityTokenAllowed,
                GuestRecoveryPolicyEnum.CapabilityLinkOnly),
            registrationWorkflowVersionId: null,
            guestAccessTokenHash: CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])),
            "EUR",
            UtcNow,
            UtcNow.AddMinutes(30));
        order.SetPii(includeVerifiedEmail
            ? RegistrationOrderPii.CreateFromVerifiedContact(order.Id, _tenantId, "Buyer", "buyer@example.test", null, null, "buyer@example.test", (int)RegistrationRetentionPolicyEnum.StandardOperational, UtcNow)
            : RegistrationOrderPii.Create(order.Id, _tenantId, "Buyer", "buyer@example.test", null, null, (int)RegistrationRetentionPolicyEnum.StandardOperational, UtcNow));
        order.AddLine(RegistrationOrderLine.Create(Guid.CreateVersion7(), catalog, ticketType, order.Id, quantity: 1, chosenUnitPriceAmount: null, platformFeePolicy: null));
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("EUR", 10_000, 0, 10_000, 0));

        PromotionScopeMetadata scope = PromotionScopeMetadata.Create(_tenantId, eventId, catalog.Id, catalog.VersionNumber, "EUR");
        PromotionDefinition definition = PromotionDefinition.CreateDraft(
            scope,
            "Launch discount",
            PromotionEligibility.AllTickets(),
            PromotionDiscountRule.FixedMinor("EUR", fixedDiscountMinor: 1_000, maximumDiscountMinor: null),
            UtcNow.AddDays(-1),
            UtcNow.AddDays(1),
            totalRedemptionLimit: 10,
            perVerifiedPurchaserLimit: 1);
        definition.Publish(UtcNow.AddHours(-1));
        PromotionCode code = PromotionCode.Create(definition, "E10", scope);
        return new PromotionScenario(order, definition, code);
    }

    private sealed record PromotionScenario(RegistrationOrder Order, PromotionDefinition Definition, PromotionCode Code);

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) => operation(cancellationToken);

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) => operation(cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
