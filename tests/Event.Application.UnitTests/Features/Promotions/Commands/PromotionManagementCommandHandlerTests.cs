// ABOUTME: Covers Application-layer promotion management command orchestration without Persistence or API dependencies.
// ABOUTME: Verifies one-time code issuance, digest-only persistence, and safe command responses.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services.Registration;
using Explore.Application.Features.Promotions;
using Explore.Application.Features.Promotions.Handlers.Commands;
using Explore.Application.Features.Promotions.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace ApplicationUnitTests.Features.Promotions.Commands;

public sealed class PromotionManagementCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IPromotionManagementRepository _promotions = Substitute.For<IPromotionManagementRepository>();
    private readonly IPromotionCodeDigestService _digests = Substitute.For<IPromotionCodeDigestService>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly TimeProvider _time = new FixedTimeProvider(UtcNow);
    private readonly InlineUnitOfWork _unitOfWork = new();

    public PromotionManagementCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _digests.NormalizeCode(Arg.Any<string>()).Returns(call => call.ArgAt<string>(0).Trim().ToUpperInvariant());
        _digests.ComputeActiveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new PromotionCodeDigest(9, $"digest:{call.ArgAt<string>(2)}"));
    }

    [Test]
    public async Task CreateAsyncPersistsDraftOnlyAndReturnsIssuedCodeOnce()
    {
        PromotionManagementScenario scenario = CreateScenario(publishDefinition: false);
        _events.GetAuthorizationTargetByIdAsync(scenario.Event.Id, Arg.Any<CancellationToken>()).Returns(scenario.Event);
        _catalogs.GetManagementCatalogAsync(scenario.Event.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(scenario.Catalog);

        PromotionCodeIssuedCommandResponseDto response = await new CreatePromotionDraftCommandHandler(
                _events,
                _catalogs,
                _promotions,
                _digests,
                _tenant,
                _time,
                _unitOfWork)
            .Handle(CreateDraftCommand(scenario, "LAUNCH-ONE"), CancellationToken.None);

        await _promotions.Received(1).AddDefinitionAsync(
            Arg.Is<PromotionDefinition>(definition => definition.PromotionDefinitionStatusId == (int)PromotionDefinitionStatusEnum.Draft),
            Arg.Any<CancellationToken>());
        await _digests.Received(1).ComputeActiveAsync(_tenantId, scenario.Event.Id, "LAUNCH-ONE", Arg.Any<CancellationToken>());
        await _promotions.DidNotReceive().AddPublishedCodeAsync(Arg.Any<PromotionCode>(), Arg.Any<PromotionCodeDigest>(), Arg.Any<CancellationToken>());
        await Assert.That(response.IsSuccess).IsTrue();
        await Assert.That(response.IssuedCode).IsEqualTo("LAUNCH-ONE");
        await Assert.That(response.Promotion!.PromotionCodeDisplayLabel).IsNull();
        await Assert.That(_unitOfWork.SerializableCount).IsEqualTo(1);
    }

    [Test]
    public async Task PublishAsyncComputesDigestAndReturnsOnlyMaskedCodeProjection()
    {
        PromotionManagementScenario scenario = CreateScenario(publishDefinition: false);
        _events.GetAuthorizationTargetByIdAsync(scenario.Event.Id, Arg.Any<CancellationToken>()).Returns(scenario.Event);
        _promotions.GetDefinitionForUpdateAsync(_tenantId, scenario.Event.Id, scenario.Definition.Id, Arg.Any<CancellationToken>()).Returns(scenario.Definition);

        PromotionManagementCommandResponseDto response = await new PublishPromotionCommandHandler(
                _events,
                _promotions,
                _digests,
                _tenant,
                _time,
                _unitOfWork)
            .Handle(new PublishPromotionCommand(scenario.Event.Id, scenario.Definition.Id, "SUPER-SECRET"), CancellationToken.None);

        await _promotions.Received(1).AddPublishedCodeAsync(
            Arg.Is<PromotionCode>(code => code.DisplayLabel == "****R-SECRET"),
            Arg.Is<PromotionCodeDigest>(digest => digest.KeyVersion == 9 && digest.Value == "digest:SUPER-SECRET"),
            Arg.Any<CancellationToken>());
        await Assert.That(response.IsSuccess).IsTrue();
        string json = JsonSerializer.Serialize(response);
        await Assert.That(json).DoesNotContain(nameof(PromotionCodeIssuedCommandResponseDto.IssuedCode));
        await Assert.That(json).DoesNotContain("SUPER-SECRET");
        await Assert.That(response.Promotion!.StatusId).IsEqualTo((int)PromotionDefinitionStatusEnum.Published);
        await Assert.That(response.Promotion.PromotionCodeDisplayLabel).IsEqualTo("****R-SECRET");
        await Assert.That(response.Message).DoesNotContain("SUPER-SECRET");
        await Assert.That(response.Message).DoesNotContain("digest");
    }

    [Test]
    public async Task RotateAsyncReplacesActiveCodeAndIssuesCodeOnce()
    {
        PromotionManagementScenario scenario = CreateScenario(publishDefinition: true);
        _events.GetAuthorizationTargetByIdAsync(scenario.Event.Id, Arg.Any<CancellationToken>()).Returns(scenario.Event);
        _promotions.GetDefinitionForUpdateAsync(_tenantId, scenario.Event.Id, scenario.Definition.Id, Arg.Any<CancellationToken>()).Returns(scenario.Definition);

        PromotionCodeIssuedCommandResponseDto response = await new RotatePromotionCodeCommandHandler(
                _events,
                _promotions,
                _digests,
                _tenant,
                _time,
                _unitOfWork)
            .Handle(new RotatePromotionCodeCommand(scenario.Event.Id, scenario.Definition.Id, "ROTATE-123456789"), CancellationToken.None);

        await _promotions.Received(1).ReplaceActiveCodeAsync(
            scenario.Definition,
            Arg.Is<PromotionCode>(code => code.DisplayLabel == "****23456789"),
            Arg.Is<PromotionCodeDigest>(digest => digest.Value == "digest:ROTATE-123456789"),
            UtcNow,
            Arg.Any<CancellationToken>());
        await Assert.That(response.IsSuccess).IsTrue();
        await Assert.That(response.IssuedCode).IsEqualTo("ROTATE-123456789");
        await Assert.That(response.Promotion!.PromotionCodeDisplayLabel).IsEqualTo("****23456789");
    }

    [Test]
    public async Task ReviseAndRevokeAsyncUsePublishedDefinitionLifecycleRules()
    {
        PromotionManagementScenario scenario = CreateScenario(publishDefinition: true);
        _events.GetAuthorizationTargetByIdAsync(scenario.Event.Id, Arg.Any<CancellationToken>()).Returns(scenario.Event);
        _promotions.GetDefinitionForUpdateAsync(_tenantId, scenario.Event.Id, scenario.Definition.Id, Arg.Any<CancellationToken>()).Returns(scenario.Definition);

        PromotionManagementCommandResponseDto revisionResponse = await new RevisePromotionCommandHandler(_events, _promotions, _tenant, _unitOfWork)
            .Handle(new RevisePromotionCommand(
                scenario.Event.Id,
                scenario.Definition.Id,
                "Revised label",
                "basis_points",
                FixedDiscountMinor: null,
                BasisPointDiscount: 1500,
                MaximumDiscountMinor: 2_000,
                UtcNow.AddDays(2),
                UtcNow.AddDays(5),
                TotalRedemptionLimit: 20,
                PerVerifiedPurchaserLimit: 2,
                EligibleTicketTypeIds: []), CancellationToken.None);

        await _promotions.Received(1).AddDefinitionAsync(
            Arg.Is<PromotionDefinition>(definition => definition.DefinitionGroupId == scenario.Definition.DefinitionGroupId && definition.VersionNumber == 2),
            Arg.Any<CancellationToken>());
        await Assert.That(revisionResponse.IsSuccess).IsTrue();
        await Assert.That(revisionResponse.Promotion!.StatusId).IsEqualTo((int)PromotionDefinitionStatusEnum.Draft);
        await Assert.That(_unitOfWork.SerializableCount).IsEqualTo(1);

        PromotionManagementCommandResponseDto revokeResponse = await new RevokePromotionCommandHandler(_events, _promotions, _tenant, _time, _unitOfWork)
            .Handle(new RevokePromotionCommand(scenario.Event.Id, scenario.Definition.Id), CancellationToken.None);

        await Assert.That(revokeResponse.IsSuccess).IsTrue();
        await Assert.That(revokeResponse.Promotion!.StatusId).IsEqualTo((int)PromotionDefinitionStatusEnum.Revoked);
        await Assert.That(scenario.Definition.RevokedAtUtc).IsEqualTo(UtcNow);
    }

    private CreatePromotionDraftCommand CreateDraftCommand(PromotionManagementScenario scenario, string code) => new(
        scenario.Event.Id,
        scenario.Catalog.Id,
        "Launch discount",
        code,
        "fixed",
        FixedDiscountMinor: 1_000,
        BasisPointDiscount: null,
        MaximumDiscountMinor: null,
        UtcNow.AddDays(1),
        UtcNow.AddDays(10),
        TotalRedemptionLimit: 10,
        PerVerifiedPurchaserLimit: 1,
        EligibleTicketTypeIds: []);

    private PromotionManagementScenario CreateScenario(bool publishDefinition)
    {
        Guid eventId = Guid.CreateVersion7();
        Explore.Domain.Event eventTarget = CreatePlatformEvent(eventId);
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, eventId, "EUR", 1);
        PromotionScopeMetadata scope = PromotionScopeMetadata.Create(_tenantId, eventId, catalog.Id, catalog.VersionNumber, catalog.CurrencyCode);
        PromotionDefinition definition = PromotionDefinition.CreateDraft(
            scope,
            "Launch discount",
            PromotionEligibility.AllTickets(),
            PromotionDiscountRule.FixedMinor("EUR", 1_000, maximumDiscountMinor: null),
            UtcNow.AddDays(1),
            UtcNow.AddDays(10),
            totalRedemptionLimit: 10,
            perVerifiedPurchaserLimit: 1);
        if (publishDefinition)
        {
            definition.Publish(UtcNow.AddHours(-1));
        }

        return new PromotionManagementScenario(eventTarget, catalog, definition);
    }

    private Explore.Domain.Event CreatePlatformEvent(Guid eventId) => new()
    {
        Id = eventId,
        TenantId = _tenantId,
        Title = "Managed event",
        ActorId = Guid.CreateVersion7(),
        Actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
            UserId = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "Publisher" }
        },
        OrganizerActorId = Guid.CreateVersion7(),
        OrganizerActor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorType = new ActorType { FullName = "Organization", MasterCode = "ORG" },
            OrganizationId = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "Organizer" }
        },
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        ParticipationConfiguration = EventParticipationConfiguration.Create(
            eventId,
            _tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            guestRecoveryPolicy: null,
            UtcNow)
    };

    private sealed record PromotionManagementScenario(Explore.Domain.Event Event, EventTicketCatalogVersion Catalog, PromotionDefinition Definition);

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public int SerializableCount { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) => operation(cancellationToken);

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        {
            SerializableCount++;
            return operation(cancellationToken);
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
