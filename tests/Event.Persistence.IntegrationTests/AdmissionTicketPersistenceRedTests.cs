// ABOUTME: Specifies Phase 20 admission-ticket, child-credential, and delivery-intent persistence behavior.
// ABOUTME: Proves provider parity, real PostgreSQL collisions, and public Application issuance replay.

using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Configuration;
using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Secrets;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Explore.Infrastructure.Services.Registration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace EventPersistence.IntegrationTests;

[Category("Phase20AdmissionPersistenceCharacterization")]
public sealed class AdmissionTicketPersistenceCharacterizationTests
{
    [Test]
    public async Task DeliveryIntentModelPersistsProtectedRecoveryLifecycleWithoutBearerColumns()
    {
        await using ExploreDbContext context = AdmissionPersistenceSurface.CreateModelContext("PostgreSql");
        IModel model = context.GetService<IDesignTimeModel>().Model;
        IEntityType intent = model.FindEntityType(typeof(AdmissionDeliveryIntent))!;
        string[] properties = intent.GetProperties().Select(property => property.Name).ToArray();

        await Assert.That(properties).Contains(nameof(AdmissionDeliveryIntent.ProtectedCredential));
        await Assert.That(properties).Contains(nameof(AdmissionDeliveryIntent.ProtectionVersion));
        await Assert.That(properties).Contains(nameof(AdmissionDeliveryIntent.RoutedAt));
        await Assert.That(properties).Contains(nameof(AdmissionDeliveryIntent.HandoffCompletedAt));
        await Assert.That(properties).Contains(nameof(AdmissionDeliveryIntent.HandoffReceiptId));
        await Assert.That(properties.Any(property => property.Contains("Plaintext", StringComparison.OrdinalIgnoreCase) ||
            property.Contains("Bearer", StringComparison.OrdinalIgnoreCase))).IsFalse();
        await Assert.That(intent.FindProperty(nameof(AdmissionDeliveryIntent.ProtectedCredential))!.GetMaxLength())
            .IsEqualTo(2048);
        await Assert.That(intent.FindProperty(nameof(AdmissionDeliveryIntent.HandoffReceiptId))!.GetMaxLength())
            .IsEqualTo(200);
        await Assert.That(intent.GetCheckConstraints().Select(constraint => constraint.Name))
            .Contains("ck_admission_delivery_intents_handoff_receipt");
    }

    [Test]
    public async Task CurrentAssignmentsAndFinalizationEffectsPersistWithTenantQualifiedReplayFences()
    {
        await using ExploreDbContext context = AdmissionPersistenceSurface.CreateModelContext("PostgreSql");
        IModel model = context.GetService<IDesignTimeModel>().Model;
        IEntityType assignment = model.FindEntityType(typeof(RegistrationTicketAssignment))!;
        IEntityType effect = model.FindEntityType(typeof(RegistrationFinalizationEffect))!;

        await Assert.That(assignment.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(effect.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(assignment.GetKeys().Any(key => AdmissionPersistenceSurface.HasProperties(
            key.Properties,
            nameof(RegistrationTicketAssignment.TenantId),
            nameof(RegistrationTicketAssignment.RegistrationOrderId),
            nameof(RegistrationTicketAssignment.Id),
            nameof(RegistrationTicketAssignment.RegistrationOrderLineId)))).IsTrue();
        await Assert.That(assignment.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties,
            nameof(RegistrationTicketAssignment.TenantId),
            nameof(RegistrationTicketAssignment.RegistrationOrderLineId),
            nameof(RegistrationTicketAssignment.Ordinal)))).IsTrue();
        await Assert.That(effect.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties,
            nameof(RegistrationFinalizationEffect.TenantId),
            nameof(RegistrationFinalizationEffect.RegistrationOrderId)))).IsTrue();
        await Assert.That(effect.GetForeignKeys().Any(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(RegistrationOrder) &&
            AdmissionPersistenceSurface.HasProperties(
                foreignKey.Properties,
                nameof(RegistrationFinalizationEffect.TenantId),
                nameof(RegistrationFinalizationEffect.EventId),
                nameof(RegistrationFinalizationEffect.RegistrationOrderId)) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict)).IsTrue();
    }
}

[Category("Phase20AdmissionPersistenceModelRed")]
public sealed class AdmissionTicketPersistenceModelRedTests
{
    [Test]
    public async Task AdmissionModelsOwnTenantQualifiedKeyedDigestsWithoutPlaintextCapabilities()
    {
        await using ExploreDbContext context = AdmissionPersistenceSurface.CreateModelContext("PostgreSql");
        (IEntityType ticket, IEntityType credential) =
            AdmissionPersistenceSurface.RequireAdmissionEntities(context.GetService<IDesignTimeModel>().Model);

        await Assert.That(ticket.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(credential.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(ticket.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties, "TenantId", "RegistrationTicketAssignmentId"))).IsTrue();
        await Assert.That(credential.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties, "TenantId", "AdmissionTicketId", "CredentialVersion"))).IsTrue();
        await Assert.That(credential.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties, "TenantId", "AdmissionTicketId", "ActiveUniquenessSlot") &&
            string.IsNullOrWhiteSpace(index.GetFilter()))).IsTrue();
        await Assert.That(credential.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties, "TenantId", "LookupKeyVersion", "LookupDigest"))).IsTrue();
        await Assert.That(credential.GetForeignKeys().Any(foreignKey =>
            foreignKey.PrincipalEntityType == ticket &&
            AdmissionPersistenceSurface.HasProperties(foreignKey.Properties, "TenantId", "AdmissionTicketId") &&
            AdmissionPersistenceSurface.HasProperties(foreignKey.PrincipalKey.Properties, "TenantId", "Id") &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict)).IsTrue();

        string[] forbiddenFragments = ["Plaintext", "Bearer", "RawCredential", "CapabilityToken", "Secret"];
        string[] persistedNames = new[] { ticket, credential }
            .SelectMany(entity => entity.GetProperties())
            .SelectMany(property => new[] { property.Name, property.GetColumnName() })
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();
        await Assert.That(persistedNames.Any(name => forbiddenFragments.Any(fragment =>
            name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))).IsFalse();
        await Assert.That(credential.FindProperty("LookupDigest")!.ClrType).IsEqualTo(typeof(string));
        await Assert.That(ticket.FindProperty("CredentialDigest")).IsNull();
        await Assert.That(ticket.FindProperty("CredentialPlaintext")).IsNull();
        await Assert.That(ticket.FindProperty("Id")!.ValueGenerated).IsEqualTo(ValueGenerated.Never);
        await Assert.That(credential.FindProperty("Id")!.ValueGenerated).IsEqualTo(ValueGenerated.Never);
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task EveryProviderMapsTheSameChildCredentialAndCollisionConstraints(string provider)
    {
        await using ExploreDbContext context = AdmissionPersistenceSurface.CreateModelContext(provider);
        (IEntityType ticket, IEntityType credential) =
            AdmissionPersistenceSurface.RequireAdmissionEntities(context.GetService<IDesignTimeModel>().Model);

        await Assert.That(ticket.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties, "TenantId", "RegistrationTicketAssignmentId"))).IsTrue();
        await Assert.That(credential.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties, "TenantId", "LookupKeyVersion", "LookupDigest"))).IsTrue();
        await Assert.That(credential.GetIndexes().Any(index => index.IsUnique && AdmissionPersistenceSurface.HasProperties(
            index.Properties, "TenantId", "AdmissionTicketId", "ActiveUniquenessSlot"))).IsTrue();
        await Assert.That(new[] { ticket, credential }.All(entity =>
            entity.FindProperty("Id")!.GetDefaultValueSql() is null)).IsTrue();
    }
}

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
[Category("Phase20AdmissionPersistencePostgreSqlRed")]
public sealed class AdmissionTicketPersistencePostgreSqlRedTests(PostgreSqlContainerFixture fixture)
{
    private static readonly DateTime UtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task SameKeyVersionAndDigestPersistAcrossTenantsAndRepositoryLookupCannotCrossTenant()
    {
        AdmissionPersistenceSurface surface = AdmissionPersistenceSurface.RequirePublicSurface();
        await fixture.ResetAsync();
        SeededAssignment tenantA = await SeedAssignmentAsync("digest-a");
        SeededAssignment tenantB = await SeedAssignmentAsync("digest-b");
        const int keyVersion = 7;
        string sharedDigest = Digest(7);
        TicketGraph graphA = surface.IssueTicketGraph(tenantA, Guid.CreateVersion7(), Guid.CreateVersion7(), 1, keyVersion, sharedDigest);
        TicketGraph graphB = surface.IssueTicketGraph(tenantB, Guid.CreateVersion7(), Guid.CreateVersion7(), 1, keyVersion, sharedDigest);

        await PersistTicketAsync(tenantA.TenantId, graphA, CancellationToken.None);
        await PersistTicketAsync(tenantB.TenantId, graphB, CancellationToken.None);

        await using ExploreDbContext tenantAContext = TenantContext(tenantA.TenantId);
        dynamic tenantARepository = surface.CreateTicketRepository(tenantAContext);
        object? foundA = await tenantARepository.GetByCredentialDigestAsync(
            tenantA.TenantId, keyVersion, sharedDigest, CancellationToken.None);
        object? blockedBFromTenantA = await tenantARepository.GetByCredentialDigestAsync(
            tenantB.TenantId, keyVersion, sharedDigest, CancellationToken.None);
        object? absent = await tenantARepository.GetByCredentialDigestAsync(
            Guid.CreateVersion7(), keyVersion, sharedDigest, CancellationToken.None);
        object? replayA = await tenantARepository.GetByAssignmentAsync(
            tenantA.TenantId, tenantA.AssignmentId, CancellationToken.None);

        await using ExploreDbContext tenantBContext = TenantContext(tenantB.TenantId);
        dynamic tenantBRepository = surface.CreateTicketRepository(tenantBContext);
        object? foundB = await tenantBRepository.GetByCredentialDigestAsync(
            tenantB.TenantId, keyVersion, sharedDigest, CancellationToken.None);

        await Assert.That(foundA).IsNotNull();
        await Assert.That(foundB).IsNotNull();
        await Assert.That(AdmissionPersistenceSurface.Read<Guid>(foundA!, "Id")).IsEqualTo(graphA.TicketId);
        await Assert.That(AdmissionPersistenceSurface.Read<Guid>(foundB!, "Id")).IsEqualTo(graphB.TicketId);
        await Assert.That(AdmissionPersistenceSurface.Read<Guid>(foundA!, "TenantId")).IsEqualTo(tenantA.TenantId);
        await Assert.That(AdmissionPersistenceSurface.Read<Guid>(foundB!, "TenantId")).IsEqualTo(tenantB.TenantId);
        await Assert.That(blockedBFromTenantA).IsNull();
        await Assert.That(absent).IsNull();
        await Assert.That(replayA).IsNotNull();
        await Assert.That(AdmissionPersistenceSurface.Read<Guid>(replayA!, "Id")).IsEqualTo(graphA.TicketId);
        await Assert.That(graphA.TicketId.Version).IsEqualTo(7);
        await Assert.That(graphB.TicketId.Version).IsEqualTo(7);
    }

    [Test]
    public async Task CredentialRotationPersistsNewDigestAndMakesPreviousDigestUnresolvable()
    {
        AdmissionPersistenceSurface surface = AdmissionPersistenceSurface.RequirePublicSurface();
        await fixture.ResetAsync();
        SeededAssignment seed = await SeedAssignmentAsync("rotation");
        string previousDigest = Digest(11);
        string replacementDigest = Digest(12);
        TicketGraph original = surface.IssueTicketGraph(
            seed, Guid.CreateVersion7(), Guid.CreateVersion7(), 1, 11, previousDigest);
        await PersistTicketAsync(seed.TenantId, original, CancellationToken.None);

        await using ExploreDbContext context = TenantContext(seed.TenantId);
        dynamic repository = surface.CreateTicketRepository(context);
        dynamic managed = await repository.GetByIdForUpdateAsync(seed.TenantId, original.TicketId, CancellationToken.None);
        managed.RotateCredential(
            Guid.CreateVersion7(), 2, 12, replacementDigest, UtcNow.AddMinutes(1));
        await repository.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        object? oldLookup = await repository.GetByCredentialDigestAsync(
            seed.TenantId, 11, previousDigest, CancellationToken.None);
        object? newLookup = await repository.GetByCredentialDigestAsync(
            seed.TenantId, 12, replacementDigest, CancellationToken.None);

        await Assert.That(oldLookup).IsNull();
        await Assert.That(newLookup).IsNotNull();
        await Assert.That(AdmissionPersistenceSurface.Read<Guid>(newLookup!, "Id")).IsEqualTo(original.TicketId);
    }

    [Test]
    public async Task PublicApplicationServiceIssuesOnceAndReplayPreservesTicketCredentialDeliveryAndDispatchCounts()
    {
        await fixture.ResetAsync();
        SeededAssignment seed = await SeedAssignmentAsync("application-qa");
        RegistrationFinalizationEffect effect = RegistrationFinalizationEffect.Create(seed.Order, UtcNow);
        await using (ExploreDbContext setup = fixture.CreateDbContext())
        {
            setup.RegistrationFinalizationEffects.Add(effect);
            await setup.SaveChangesAsync();
        }

        await using ExploreDbContext context = TenantContext(seed.TenantId);
        var dispatcher = new RecordingAdmissionDispatcher(context);
        var service = new AdmissionIssuanceService(
            new AdmissionIssuanceRepository(context),
            new AdmissionCredentialDigestService(
                new AdmissionSecretResolver(),
                Options.Create(new AdmissionCredentialOptions { ActiveKeyVersion = 7 })),
            new AdmissionDeliveryEnvelopeProtector(new EphemeralDataProtectionProvider()),
            dispatcher,
            new EfCoreUnitOfWork(context),
            new FixedAdmissionTimeProvider(UtcNow));
        var request = new AdmissionIssuanceRequest(seed.TenantId, seed.OrderId, effect.Id, "ConfirmedFreeOrder");

        AdmissionIssuanceResult issued = await service.IssueConfirmedAsync(request, CancellationToken.None);
        AdmissionIssuanceResult replay = await service.IssueConfirmedAsync(request, CancellationToken.None);
        int ticketCount = await context.AdmissionTickets.CountAsync();
        int credentialCount = await context.AdmissionTicketCredentials.CountAsync();
        int deliveryCount = await context.AdmissionDeliveryIntents.CountAsync();

        await Assert.That(issued.Outcome).IsEqualTo(AdmissionIssuanceOutcome.Issued);
        await Assert.That(issued.OneTimeCredentials.Count).IsEqualTo(1);
        await Assert.That(issued.DeliveryOutcome).IsEqualTo(AdmissionDeliveryOutcome.Delivered);
        await Assert.That(replay.Outcome).IsEqualTo(AdmissionIssuanceOutcome.AlreadyIssued);
        await Assert.That(replay.ExistingTicketIds).IsEquivalentTo(issued.IssuedTicketIds);
        await Assert.That(replay.OneTimeCredentials).IsEmpty();
        await Assert.That(ticketCount).IsEqualTo(1);
        await Assert.That(credentialCount).IsEqualTo(1);
        await Assert.That(deliveryCount).IsEqualTo(1);
        await Assert.That(dispatcher.DispatchCount).IsEqualTo(1);
        Console.WriteLine($"ADMISSION_ISSUANCE_QA outcome={issued.Outcome} replay={replay.Outcome} tickets={ticketCount} credentials={credentialCount} deliveryIntents={deliveryCount} dispatches={dispatcher.DispatchCount}");
    }

    [Test]
    public async Task ConcurrentPublicServiceCallsFenceBeforeCredentialGenerationAndConverge()
    {
        await fixture.ResetAsync();
        SeededAssignment seed = await SeedAssignmentAsync("public-service-race");
        RegistrationFinalizationEffect effect = RegistrationFinalizationEffect.Create(seed.Order, UtcNow);
        await using (ExploreDbContext setup = fixture.CreateDbContext())
        {
            setup.RegistrationFinalizationEffects.Add(effect);
            await setup.SaveChangesAsync();
        }

        var secondLock = new FinalizationLockCommandSignal();
        await using ExploreDbContext firstContext = TenantContext(seed.TenantId);
        await using ExploreDbContext secondContext = TenantContext(seed.TenantId, secondLock);
        var gatedDigest = new GatedAdmissionDigestService(
            new AdmissionCredentialDigestService(
                new AdmissionSecretResolver(),
                Options.Create(new AdmissionCredentialOptions { ActiveKeyVersion = 7 })));
        var dataProtection = new EphemeralDataProtectionProvider();
        var request = new AdmissionIssuanceRequest(seed.TenantId, seed.OrderId, effect.Id, "ConfirmedFreeOrder");
        AdmissionIssuanceService firstService = CreateService(firstContext);
        AdmissionIssuanceService secondService = CreateService(secondContext);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        Task<AdmissionIssuanceResult> first = firstService.IssueConfirmedAsync(request, timeout.Token);
        await gatedDigest.FirstGenerationEntered.WaitAsync(timeout.Token);
        Task<AdmissionIssuanceResult> second = secondService.IssueConfirmedAsync(request, timeout.Token);
        await secondLock.CommandStarted.WaitAsync(timeout.Token);
        gatedDigest.ReleaseFirstGeneration();
        AdmissionIssuanceResult[] outcomes = await Task.WhenAll(first, second);

        await Assert.That(gatedDigest.GenerationCount).IsEqualTo(1);
        await Assert.That(outcomes.Count(result => result.Outcome == AdmissionIssuanceOutcome.Issued)).IsEqualTo(1);
        await Assert.That(outcomes.Count(result => result.Outcome == AdmissionIssuanceOutcome.AlreadyIssued)).IsEqualTo(1);
        Guid[] winnerIds = outcomes.Single(result => result.Outcome == AdmissionIssuanceOutcome.Issued).IssuedTicketIds.ToArray();
        await Assert.That(outcomes.Single(result => result.Outcome == AdmissionIssuanceOutcome.AlreadyIssued).ExistingTicketIds)
            .IsEquivalentTo(winnerIds);
        await Assert.That(outcomes.Sum(result => result.OneTimeCredentials.Count)).IsEqualTo(1);
        await using ExploreDbContext verification = TenantContext(seed.TenantId);
        await Assert.That(await verification.AdmissionTickets.CountAsync()).IsEqualTo(1);
        await Assert.That(await verification.AdmissionTicketCredentials.CountAsync()).IsEqualTo(1);
        await Assert.That(await verification.AdmissionDeliveryIntents.CountAsync()).IsEqualTo(1);

        AdmissionIssuanceService CreateService(ExploreDbContext context) => new(
            new AdmissionIssuanceRepository(context),
            gatedDigest,
            new AdmissionDeliveryEnvelopeProtector(dataProtection),
            new RecordingAdmissionDispatcher(context),
            new EfCoreUnitOfWork(context),
            new FixedAdmissionTimeProvider(UtcNow));
    }

    [Test]
    public async Task SameTenantSameAssignmentDistinctDigestsHaveOneTicketWinnerAtMappedTicketInsert()
    {
        AdmissionPersistenceSurface surface = AdmissionPersistenceSurface.RequirePublicSurface();
        await fixture.ResetAsync();
        SeededAssignment seed = await SeedAssignmentAsync("assignment-collision");
        TicketGraph firstGraph = surface.IssueTicketGraph(
            seed, Guid.CreateVersion7(), Guid.CreateVersion7(), 1, 23, Digest(23));
        TicketGraph secondGraph = surface.IssueTicketGraph(
            seed, Guid.CreateVersion7(), Guid.CreateVersion7(), 1, 23, Digest(24));

        await AssertPostgreSqlCollisionAsync(
            seed.TenantId, firstGraph, secondGraph, CollisionBarrierTarget.Ticket);
    }

    [Test]
    public async Task SameTenantDistinctAssignmentsSameDigestHaveOneCredentialWinnerAtMappedCredentialInsert()
    {
        AdmissionPersistenceSurface surface = AdmissionPersistenceSurface.RequirePublicSurface();
        await fixture.ResetAsync();
        SeededAssignment[] assignments = await SeedAssignmentsAsync("credential-collision", assignmentCount: 2);
        const int keyVersion = 29;
        string sharedDigest = Digest(29);
        TicketGraph firstGraph = surface.IssueTicketGraph(
            assignments[0], Guid.CreateVersion7(), Guid.CreateVersion7(), 1, keyVersion, sharedDigest);
        TicketGraph secondGraph = surface.IssueTicketGraph(
            assignments[1], Guid.CreateVersion7(), Guid.CreateVersion7(), 1, keyVersion, sharedDigest);

        await Assert.That(assignments[0].TenantId).IsEqualTo(assignments[1].TenantId);
        await Assert.That(assignments[0].AssignmentId).IsNotEqualTo(assignments[1].AssignmentId);
        await AssertPostgreSqlCollisionAsync(
            assignments[0].TenantId, firstGraph, secondGraph, CollisionBarrierTarget.Credential);
    }

    private async Task AssertPostgreSqlCollisionAsync(
        Guid tenantId,
        TicketGraph firstGraph,
        TicketGraph secondGraph,
        CollisionBarrierTarget barrierTarget)
    {
        AdmissionPersistenceSurface surface = AdmissionPersistenceSurface.RequirePublicSurface();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using ExploreDbContext metadataContext = fixture.CreateDbContext();
        (IEntityType ticketModel, IEntityType credentialModel) =
            AdmissionPersistenceSurface.RequireAdmissionEntities(metadataContext.Model);
        string ticketTableIdentifier =
            AdmissionPersistenceSurface.DelimitedTableIdentifier(metadataContext, ticketModel);
        string credentialTableIdentifier =
            AdmissionPersistenceSurface.DelimitedTableIdentifier(metadataContext, credentialModel);
        string barrierTableIdentifier = barrierTarget == CollisionBarrierTarget.Ticket
            ? ticketTableIdentifier
            : credentialTableIdentifier;
        var barrier = new AdmissionInsertBarrier();
        Guid firstWriter = Guid.CreateVersion7();
        Guid secondWriter = Guid.CreateVersion7();

        Task<Exception?> first = PersistCompetingTicketAsync(firstWriter, firstGraph);
        Task<Exception?> second = PersistCompetingTicketAsync(secondWriter, secondGraph);
        await barrier.AllArrived.WaitAsync(timeout.Token);
        await Assert.That(barrier.Arrivals).IsEqualTo(2);
        await Assert.That(barrier.HasArrived(firstWriter)).IsTrue();
        await Assert.That(barrier.HasArrived(secondWriter)).IsTrue();
        barrier.Release();
        Exception?[] outcomes = await Task.WhenAll(first, second);

        await Assert.That(outcomes.Count(outcome => outcome is null)).IsEqualTo(1);
        await Assert.That(outcomes.Count(outcome => outcome is not null)).IsEqualTo(1);
        Exception collision = outcomes.Single(outcome => outcome is not null)!;
        await Assert.That(collision.GetBaseException()).IsTypeOf<PostgresException>();
        await Assert.That(((PostgresException)collision.GetBaseException()).SqlState)
            .IsEqualTo(PostgresErrorCodes.UniqueViolation);
        await using ExploreDbContext verification = TenantContext(tenantId);
        await Assert.That(AdmissionPersistenceSurface.CountRows(verification, ticketModel.ClrType)).IsEqualTo(1);
        await Assert.That(AdmissionPersistenceSurface.CountRows(verification, credentialModel.ClrType)).IsEqualTo(1);

        async Task<Exception?> PersistCompetingTicketAsync(Guid writerId, TicketGraph graph)
        {
            var interceptor = new AdmissionInsertBarrierInterceptor(barrier, writerId, barrierTableIdentifier);
            await using ExploreDbContext context = TenantContext(tenantId, interceptor);
            dynamic repository = surface.CreateTicketRepository(context);
            try
            {
                await repository.AddAsync((dynamic)graph.Ticket, timeout.Token);
                await repository.SaveChangesAsync(timeout.Token);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }

    private static string Digest(byte fill) =>
        Convert.ToBase64String(Enumerable.Repeat(fill, 32).ToArray());

    private async Task PersistTicketAsync(Guid tenantId, TicketGraph graph, CancellationToken cancellationToken)
    {
        await using ExploreDbContext context = TenantContext(tenantId);
        dynamic repository = AdmissionPersistenceSurface.RequirePublicSurface().CreateTicketRepository(context);
        await repository.AddAsync((dynamic)graph.Ticket, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private ExploreDbContext TenantContext(Guid tenantId, params IInterceptor[] interceptors)
    {
        ExploreDbContext context = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        if (interceptors.Length == 0)
            return context;

        context.Dispose();
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptors)
            .Options;
        return new ExploreDbContext(options) { TenantContext = new TestTenantContext(tenantId) };
    }

    private async Task<SeededAssignment> SeedAssignmentAsync(string suffix) =>
        (await SeedAssignmentsAsync(suffix, assignmentCount: 1)).Single();

    private async Task<SeededAssignment[]> SeedAssignmentsAsync(string suffix, int assignmentCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assignmentCount);

        await using ExploreDbContext context = fixture.CreateDbContext();
        var tenant = new Tenant
        {
            FullName = $"Admission {suffix}",
            Slug = $"admission-{suffix}-{Guid.CreateVersion7():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"admission-{suffix}-{Guid.CreateVersion7():N}@example.test",
                FirstName = "Admission",
                LastName = suffix
            }
        };
        context.AddRange(tenant, user);
        await context.SaveChangesAsync();
        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = $"Admission {suffix}" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();
        var eventEntity = new DomainEvent(EventStatusEnum.Draft)
        {
            Id = Guid.CreateVersion7(),
            Title = $"Admission {suffix}",
            Subtitle = "",
            Description = "",
            FirstSessionDate = DateOnly.FromDateTime(UtcNow.AddDays(1)),
            LastSessionDate = DateOnly.FromDateTime(UtcNow.AddDays(1)),
            EventTypeId = 1,
            AudienceGenderId = 1,
            AudienceAgeId = 1,
            ActorId = actor.Id,
            Actor = null!,
            OrganizerActorId = actor.Id,
            TenantId = tenant.Id,
            Tenant = tenant,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            EventProvenanceTypeId = (int)EventProvenanceTypeEnum.OrganizerCreated,
            TotalViews = 0
        };
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenant.Id, eventEntity.Id, "EUR", 1);
        EventTicketType ticketType = EventTicketType.Create(
            Guid.CreateVersion7(), tenant.Id, catalog.Id, "Admission", "EUR", TicketPricingModeEnum.Free,
            null, null, null, ParticipantDataCollectionModeEnum.None, null, null, null,
            false, false, null, null, null, null);
        catalog.AddTicketType(ticketType, null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEvent(ticketType.Id, tenant.Id, eventEntity.Id, 1));
        catalog.Publish();
        RegistrationOrder order = RegistrationOrder.Create(
            tenant.Id, eventEntity.Id, user.Id, actor.Id, BookingPartyTypeEnum.Individual, catalog.Id,
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 1, 1, 1, null),
            null, null, "EUR", UtcNow, null);
        RegistrationOrderLine line = RegistrationOrderLine.Create(
            catalog, ticketType, order.Id, assignmentCount, null, null);
        order.AddLine(line);
        RegistrationParticipant[] participants = Enumerable.Range(0, assignmentCount)
            .Select(_ => RegistrationParticipant.Create(
                tenant.Id, order.Id, user.Id, ParticipantTypeEnum.Adult, guardian: null))
            .ToArray();
        RegistrationTicketAssignment[] assignments = participants
            .Select((participant, index) => RegistrationTicketAssignment.CreateAssigned(
                Guid.CreateVersion7(), line.Id, index + 1, participant, UtcNow))
            .ToArray();
        for (int index = 0; index < participants.Length; index++)
        {
            order.AddParticipant(participants[index]);
            order.AddAssignment(line, assignments[index], participants[index]);
        }
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("EUR", 0, 0, 0, 0));
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, UtcNow);
        order.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, UtcNow);
        order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, UtcNow);
        context.AddRange(eventEntity, catalog, order);
        await context.SaveChangesAsync();
        return assignments.Select((assignment, index) => new SeededAssignment(
            tenant.Id, eventEntity.Id, order.Id, line.Id, assignment.Id, participants[index].Id,
            order, line, assignment, participants[index], catalog, ticketType)).ToArray();
    }

    private sealed class GatedAdmissionDigestService(IAdmissionCredentialDigestService inner)
        : IAdmissionCredentialDigestService
    {
        private readonly TaskCompletionSource firstGenerationEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseFirstGeneration =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int generationCount;

        internal Task FirstGenerationEntered => firstGenerationEntered.Task;
        internal int GenerationCount => Volatile.Read(ref generationCount);
        internal void ReleaseFirstGeneration() => releaseFirstGeneration.TrySetResult();

        public async Task<AdmissionCredentialMaterial> CreateAsync(
            AdmissionCredentialCreateRequest request,
            CancellationToken cancellationToken)
        {
            int call = Interlocked.Increment(ref generationCount);
            if (call == 1)
            {
                firstGenerationEntered.TrySetResult();
                await releaseFirstGeneration.Task.WaitAsync(cancellationToken);
            }
            return await inner.CreateAsync(request, cancellationToken);
        }

        public Task<AdmissionCredentialVerificationOutcome> VerifyAsync(
            AdmissionCredentialVerificationRequest request,
            CancellationToken cancellationToken) => inner.VerifyAsync(request, cancellationToken);
    }

    private sealed class FinalizationLockCommandSignal : DbCommandInterceptor
    {
        private readonly TaskCompletionSource commandStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task CommandStarted => commandStarted.Task;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("registration_finalization_effects", StringComparison.OrdinalIgnoreCase) &&
                command.CommandText.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                commandStarted.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingAdmissionDispatcher(ExploreDbContext context) : IAdmissionDeliveryDispatcher
    {
        internal int DispatchCount { get; private set; }

        public async Task<AdmissionDeliveryDispatchResult> DispatchAsync(
            AdmissionDeliveryDispatchRequest request,
            CancellationToken cancellationToken)
        {
            AdmissionDeliveryIntent? intent = await context.AdmissionDeliveryIntents.SingleOrDefaultAsync(
                intent => intent.Id == request.DeliveryIntentId, cancellationToken);
            if (intent is null)
            {
                throw new InvalidOperationException("Admission delivery intent was not committed before dispatch.");
            }
            intent.MarkRouted(UtcNow);
            intent.CompleteHandoff($"test:{intent.Id:N}", UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            DispatchCount++;
            return new AdmissionDeliveryDispatchResult(AdmissionDeliveryOutcome.Delivered);
        }
    }

    private sealed class AdmissionSecretResolver : ISecretResolver
    {
        private static readonly string Key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());

        public Task<ResolvedSecret?> ResolveAsync(string settingKey, Guid? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ResolvedSecret?>(null);

        public Task<ResolvedSecret?> ResolveQualifiedAsync(string settingKey, SecretScope scope, Guid? scopeId,
            string qualifier, CancellationToken cancellationToken = default) => Task.FromResult<ResolvedSecret?>(
                settingKey == Explore.Domain.Secrets.SecretDefinitionRegistry.Keys.Admissions.CredentialLookupHmacKey &&
                qualifier == "v7"
                    ? new ResolvedSecret(settingKey, Key, SecretSourceType.EnvironmentVariable, scope, scopeId, UtcNow)
                    : null);

        public Task<ResolvedSecret?> ResolveTenantBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ResolvedSecret?>(null);

        public Task InvalidateAsync(string settingKey, SecretScope scope, Guid? scopeId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FixedAdmissionTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now);
    }

    private enum CollisionBarrierTarget
    {
        Ticket,
        Credential
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}

internal sealed class AdmissionPersistenceSurface
{
    private static readonly DateTime UtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    internal const string TicketTypeName = "Explore.Domain.AdmissionTicket";
    private const string CredentialTypeName = "Explore.Domain.AdmissionTicketCredential";
    private const string TicketRepositoryTypeName = "Explore.Persistence.Repositories.AdmissionTicketRepository";

    private readonly Type ticketRepositoryType;

    private AdmissionPersistenceSurface(Type ticketRepositoryType)
    {
        this.ticketRepositoryType = ticketRepositoryType;
    }

    internal static AdmissionPersistenceSurface RequirePublicSurface()
    {
        Assembly domain = typeof(RegistrationOrder).Assembly;
        Assembly persistence = typeof(ExploreDbContext).Assembly;
        _ = RequireType(domain, TicketTypeName);
        _ = RequireType(domain, CredentialTypeName);
        return new(RequireType(persistence, TicketRepositoryTypeName));
    }

    internal TicketGraph IssueTicketGraph(
        SeededAssignment seed,
        Guid ticketId,
        Guid credentialId,
        int credentialVersion,
        int lookupKeyVersion,
        string lookupDigest)
    {
        AdmissionTicket ticket = AdmissionTicket.Issue(
            seed.Order,
            seed.Line,
            seed.Assignment,
            seed.Participant,
            seed.Catalog,
            seed.TicketType,
            ticketId,
            $"T-{ticketId:N}",
            credentialId,
            credentialVersion,
            lookupKeyVersion,
            lookupDigest,
            UtcNow);
        return new(ticket, ticketId, credentialId);
    }

    internal object CreateTicketRepository(ExploreDbContext context) =>
        CreateRepository(ticketRepositoryType, context);

    internal static (IEntityType Ticket, IEntityType Credential) RequireAdmissionEntities(IModel model) =>
        (RequireEntity(model, TicketTypeName), RequireEntity(model, CredentialTypeName));

    internal static IEntityType RequireEntity(IModel model, string clrTypeName) =>
        model.GetEntityTypes().SingleOrDefault(entity => entity.ClrType.FullName == clrTypeName)
        ?? throw Missing($"EF entity/configuration {clrTypeName}");

    internal static T Read<T>(object value, string propertyName)
    {
        PropertyInfo property = value.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw Missing($"public property {value.GetType().FullName}.{propertyName}");
        return (T)property.GetValue(value)!;
    }

    internal static int CountRows(ExploreDbContext context, Type entityType)
    {
        IQueryable query = (IQueryable)typeof(DbContext).GetMethods()
            .Single(method => method.Name == nameof(DbContext.Set) && method.IsGenericMethod && method.GetParameters().Length == 0)
            .MakeGenericMethod(entityType)
            .Invoke(context, null)!;
        MethodCallExpression count = Expression.Call(typeof(Queryable), nameof(Queryable.Count), [entityType], query.Expression);
        return query.Provider.Execute<int>(count);
    }

    internal static string DelimitedTableIdentifier(ExploreDbContext context, IEntityType entity)
    {
        string table = entity.GetTableName() ?? throw Missing($"table mapping for {entity.ClrType.FullName}");
        string? schema = entity.GetSchema();
        ISqlGenerationHelper sql = context.GetService<ISqlGenerationHelper>();
        return schema is null ? sql.DelimitIdentifier(table) : sql.DelimitIdentifier(table, schema);
    }

    internal static bool HasProperties(IReadOnlyList<IReadOnlyProperty> properties, params string[] names) =>
        properties.Select(property => property.Name).SequenceEqual(names);

    internal static ExploreDbContext CreateModelContext(string provider)
    {
        var builder = new DbContextOptionsBuilder<ExploreDbContext>();
        switch (provider)
        {
            case "PostgreSql":
                builder.UseNpgsql("Host=localhost;Database=phase20_model;Username=unused");
                break;
            case "Sqlite":
                builder.UseSqlite("Data Source=:memory:");
                break;
            case "SqlServer":
                builder.UseSqlServer("Server=localhost;Database=phase20_model;Integrated Security=true;TrustServerCertificate=True");
                break;
            case "MariaDb":
                builder.UseMySql("Server=localhost;Database=phase20_model;User=unused", new MariaDbServerVersion(new Version(10, 11)));
                break;
            case "MySql":
                builder.UseMySql("Server=localhost;Database=phase20_model;User=unused", new MySqlServerVersion(new Version(8, 0)));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
        return new ExploreDbContext(builder.UseSnakeCaseNamingConvention().Options);
    }

    private static Type RequireType(Assembly assembly, string fullName) =>
        assembly.GetType(fullName) ?? throw Missing($"public aggregate/repository {fullName}");

    private static object InvokeFactory(
        Type type,
        string methodName,
        Type[] parameterTypes,
        object?[] arguments)
    {
        MethodInfo factory = type.GetMethod(
            methodName, BindingFlags.Public | BindingFlags.Static, binder: null, parameterTypes, modifiers: null)
            ?? throw Missing($"supported factory {type.FullName}.{methodName}");
        return factory.Invoke(null, arguments)
            ?? throw Missing($"result from supported factory {type.FullName}.{methodName}");
    }

    private static object CreateRepository(Type repositoryType, ExploreDbContext context)
    {
        ConstructorInfo constructor = repositoryType.GetConstructor([typeof(ExploreDbContext)])
            ?? throw Missing($"supported {repositoryType.FullName}(ExploreDbContext) constructor");
        return constructor.Invoke([context]);
    }

    private static InvalidOperationException Missing(string surface) =>
        new($"Phase 20 product RED: missing {surface}.");
}

internal sealed record SeededAssignment(
    Guid TenantId,
    Guid EventId,
    Guid OrderId,
    Guid LineId,
    Guid AssignmentId,
    Guid ParticipantId,
    RegistrationOrder Order,
    RegistrationOrderLine Line,
    RegistrationTicketAssignment Assignment,
    RegistrationParticipant Participant,
    EventTicketCatalogVersion Catalog,
    EventTicketType TicketType);

internal sealed record TicketGraph(object Ticket, Guid TicketId, Guid CredentialId);

internal sealed class AdmissionInsertBarrier
{
    private readonly Lock sync = new();
    private readonly HashSet<Guid> arrivedWriters = [];
    private readonly TaskCompletionSource allArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task AllArrived => allArrived.Task;
    internal int Arrivals { get { lock (sync) return arrivedWriters.Count; } }
    internal bool HasArrived(Guid writerId) { lock (sync) return arrivedWriters.Contains(writerId); }

    internal async ValueTask ArriveAsync(Guid writerId, CancellationToken cancellationToken)
    {
        lock (sync)
        {
            if (!arrivedWriters.Add(writerId))
                throw new InvalidOperationException($"Admission writer {writerId:N} reached the insert barrier more than once.");
            if (arrivedWriters.Count == 2)
                allArrived.TrySetResult();
        }
        await release.Task.WaitAsync(cancellationToken);
    }

    internal void Release() => release.TrySetResult();
}

internal sealed class AdmissionInsertBarrierInterceptor(
    AdmissionInsertBarrier barrier,
    Guid writerId,
    string mappedTableIdentifier) : DbCommandInterceptor
{
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await AwaitAdmissionInsertAsync(command, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await AwaitAdmissionInsertAsync(command, cancellationToken);
        return result;
    }

    private ValueTask AwaitAdmissionInsertAsync(DbCommand command, CancellationToken cancellationToken) =>
        command.CommandText.Contains($"INSERT INTO {mappedTableIdentifier}", StringComparison.OrdinalIgnoreCase)
            ? barrier.ArriveAsync(writerId, cancellationToken)
            : ValueTask.CompletedTask;
}
