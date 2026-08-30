// ABOUTME: Defines prospective PostgreSQL contracts for ticketing restore authority and bearer rotation.
// ABOUTME: Pins manifest validation, recovery-only reopening, tenant fences, replay, and ambiguity preservation.

using System.Reflection;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class TicketingLifecycleRecoveryInvariantTests(
    PostgreSqlContainerFixture fixture)
{
    private const string ManifestTypeName =
        "Explore.Domain.TicketingRecoveryManifest";
    private const string CheckpointTypeName =
        "Explore.Domain.TicketingRecoveryCheckpoint";
    private const string ReissueTypeName =
        "Explore.Domain.TicketingRecoveryReissueIntent";
    private const string RepositoryTypeName =
        "Explore.Persistence.Repositories.TicketingRecoveryRepository";
    private static readonly DateTime UtcNow =
        new(2026, 8, 29, 14, 45, 0, DateTimeKind.Utc);

    [Test]
    public async Task RecoverySurfaceOwnsClosedStatesValidationAndBearerRotation()
    {
        Type? manifest = DomainType(ManifestTypeName);
        Type? checkpoint = DomainType(CheckpointTypeName);
        Type? reissue = DomainType(ReissueTypeName);

        await Assert.That(manifest).IsNotNull()
            .Because("restore facts require one immutable manifest value");
        await Assert.That(checkpoint).IsNotNull()
            .Because("runtime reopening requires one persisted recovery-only authority");
        await Assert.That(reissue).IsNotNull()
            .Because("credential rotation requires durable reissue intent before reopening");
        if (manifest is null || checkpoint is null || reissue is null)
        {
            return;
        }

        await Assert.That(HasProperties(
                manifest,
                "OperationId",
                "TenantId",
                "ReleaseRevision",
                "SchemaRevision",
                "DatabaseCheckpoint",
                "ObjectCutoffUtc",
                "RetainedKeyVersion",
                "AuthorityFloor",
                "ProviderCursor",
                "IdempotencyFloor",
                "WorkerFence",
                "CapabilityGeneration",
                "CredentialGeneration",
                "Digest"))
            .IsTrue();
        await Assert.That(HasMethods(
                checkpoint,
                "Begin",
                "Validate",
                "TryRotateBearerAuthority",
                "TryOpenWorkers",
                "TryOpenSales",
                "Fail"))
            .IsTrue();
        await AssertEnumNamesAsync(
            "Explore.Domain.TicketingRecoveryStatus",
            "RecoveryOnly",
            "Validated",
            "AuthorityRotated",
            "WorkersOpen",
            "SalesOpen",
            "Failed");
        await AssertEnumNamesAsync(
            "Explore.Domain.TicketingRecoveryValidationOutcome",
            "Validated",
            "ReleaseMismatch",
            "SchemaMismatch",
            "MissingKey",
            "StaleAuthority",
            "StaleProviderCursor",
            "MissingIdempotency",
            "StaleWorkerFence");
    }

    [Test]
    public async Task ManifestRejectsMissingKeyCursorFenceIdempotencyAndMixedRevision()
    {
        RecoveryReflectionSurface? surface = await RequireSurfaceAsync();
        if (surface is null)
        {
            return;
        }

        await Assert.That(surface.CaptureManifestFailure(retainedKeyVersion: 0))
            .IsTypeOf<ArgumentOutOfRangeException>();
        await Assert.That(surface.CaptureManifestFailure(providerCursor: -1))
            .IsTypeOf<ArgumentOutOfRangeException>();
        await Assert.That(surface.CaptureManifestFailure(idempotencyFloor: -1))
            .IsTypeOf<ArgumentOutOfRangeException>();
        await Assert.That(surface.CaptureManifestFailure(workerFence: -1))
            .IsTypeOf<ArgumentOutOfRangeException>();

        object manifest = surface.CreateManifest();
        object checkpoint = surface.Begin(manifest);
        await Assert.That(surface.Validate(
                checkpoint,
                runningReleaseRevision: "release-other",
                runningSchemaRevision: "schema-7",
                minimumRetainedKeyVersion: 3,
                minimumAuthorityFloor: 80,
                minimumProviderCursor: 90,
                minimumIdempotencyFloor: 100,
                minimumWorkerFence: 110))
            .IsEqualTo("ReleaseMismatch");
        await Assert.That(surface.Status(checkpoint)).IsEqualTo("RecoveryOnly");
    }

    [Test]
    public async Task PreRevocationRestoreCannotReopenBeforeEveryBearerGenerationRotates()
    {
        RecoveryReflectionSurface? surface = await RequireSurfaceAsync();
        if (surface is null)
        {
            return;
        }

        object manifest = surface.CreateManifest(
            capabilityGeneration: 5,
            credentialGeneration: 8,
            workerFence: 110);
        object checkpoint = surface.Begin(manifest);
        await Assert.That(surface.Validate(
                checkpoint,
                "release-8",
                "schema-7",
                3,
                80,
                90,
                100,
                110))
            .IsEqualTo("Validated");
        await Assert.That(surface.TryOpenWorkers(checkpoint, 111)).IsFalse();
        await Assert.That(surface.TryOpenSales(checkpoint)).IsFalse();
        await Assert.That(
                surface.TryRotateBearerAuthority(
                    checkpoint,
                    capabilityGeneration: 5,
                    credentialGeneration: 9,
                    workerFence: 111))
            .IsFalse();
        await Assert.That(
                surface.TryRotateBearerAuthority(
                    checkpoint,
                    capabilityGeneration: 6,
                    credentialGeneration: 9,
                    workerFence: 111))
            .IsTrue();
        await Assert.That(surface.TryOpenWorkers(checkpoint, 110)).IsFalse();
        await Assert.That(surface.TryOpenWorkers(checkpoint, 111)).IsTrue();
        await Assert.That(surface.TryOpenSales(checkpoint)).IsTrue();
        await Assert.That(surface.Status(checkpoint)).IsEqualTo("SalesOpen");
    }

    [Test]
    public async Task PersistenceModelEnforcesTenantManifestReplayAndUniqueReissue()
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        IModel model = context.GetService<IDesignTimeModel>().Model;
        IEntityType? checkpoint = model.FindEntityType(CheckpointTypeName);
        IEntityType? reissue = model.FindEntityType(ReissueTypeName);

        await Assert.That(checkpoint).IsNotNull();
        await Assert.That(reissue).IsNotNull();
        if (checkpoint is null || reissue is null)
        {
            return;
        }

        await Assert.That(checkpoint.FindDeclaredQueryFilter(QueryFilterNames.Tenant))
            .IsNotNull();
        await Assert.That(reissue.FindDeclaredQueryFilter(QueryFilterNames.Tenant))
            .IsNotNull();
        await Assert.That(checkpoint.FindProperty("ConcurrencyStamp")!.IsConcurrencyToken)
            .IsTrue();
        await Assert.That(HasUniqueIndex(
                checkpoint,
                "TenantId",
                "RecoveryOperationId"))
            .IsTrue();
        await Assert.That(HasUniqueIndex(
                reissue,
                "TenantId",
                "RecoveryOperationId",
                "AdmissionTicketId"))
            .IsTrue();
        await Assert.That(reissue.GetProperties().Any(property =>
                property.Name.Contains("CredentialDigest", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Capability", StringComparison.OrdinalIgnoreCase)))
            .IsFalse();
    }

    [Test]
    public async Task RepositoryExposesAtomicRecoveryFenceAndReplayPrimitives()
    {
        Type? repository = PersistenceType(RepositoryTypeName);
        await Assert.That(repository).IsNotNull();
        if (repository is null)
        {
            return;
        }

        await Assert.That(HasMethods(
                repository,
                "BeginRecoveryAsync",
                "ValidateAndRotateAsync",
                "OpenWorkersAsync",
                "OpenSalesAsync",
                "GetAsync",
                "GetHealthAsync"))
            .IsTrue();
        await Assert.That(repository.GetField(
                "CanonicalFenceOrder",
                BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue())
            .IsEqualTo(
                "recovery-checkpoint>capabilities>credentials>reissue-intents>queues>provider-cursors");
    }

    [Test]
    public async Task RepositoryReplayIsTenantQualifiedAndCreatesNoDuplicateEffects()
    {
        RecoveryReflectionSurface? surface = await RequireSurfaceAsync();
        if (surface is null)
        {
            return;
        }

        await fixture.ResetAsync();
        Guid tenantA = Guid.CreateVersion7();
        Guid tenantB = Guid.CreateVersion7();
        Guid operationId = Guid.CreateVersion7();
        object manifestA = surface.CreateManifest(
            operationId: operationId,
            tenantId: tenantA);
        object manifestB = surface.CreateManifest(
            operationId: operationId,
            tenantId: tenantB);

        await using ExploreDbContext context = fixture.CreateDbContext();
        object repository = surface.CreateRepository(context);
        object first = await surface.BeginRecoveryAsync(
            repository,
            manifestA,
            CancellationToken.None);
        object replay = await surface.BeginRecoveryAsync(
            repository,
            manifestA,
            CancellationToken.None);
        object otherTenant = await surface.BeginRecoveryAsync(
            repository,
            manifestB,
            CancellationToken.None);

        context.EnableTenantFilterBypass(
            "Phase 8 recovery invariant verification reads exact tenant-qualified rows.");
        await Assert.That(surface.Read<Guid>(first, "Id"))
            .IsEqualTo(surface.Read<Guid>(replay, "Id"));
        await Assert.That(surface.Read<Guid>(otherTenant, "Id"))
            .IsNotEqualTo(surface.Read<Guid>(first, "Id"));
        await Assert.That(surface.Rows(context, surface.CheckpointType).Length)
            .IsEqualTo(2);
        await Assert.That(surface.Rows(context, surface.ReissueType).Length)
            .IsEqualTo(0);
    }

    private static async Task<RecoveryReflectionSurface?> RequireSurfaceAsync()
    {
        Type? manifest = DomainType(ManifestTypeName);
        Type? checkpoint = DomainType(CheckpointTypeName);
        Type? reissue = DomainType(ReissueTypeName);
        Type? repository = PersistenceType(RepositoryTypeName);
        (Type? Value, string Name)[] required =
        [
            (manifest, ManifestTypeName),
            (checkpoint, CheckpointTypeName),
            (reissue, ReissueTypeName),
            (repository, RepositoryTypeName),
        ];
        foreach ((Type? value, string name) in required)
        {
            await Assert.That(value).IsNotNull()
                .Because($"Phase 8 product RED requires {name}");
        }

        return required.Any(value => value.Value is null)
            ? null
            : new RecoveryReflectionSurface(
                manifest!,
                checkpoint!,
                reissue!,
                repository!);
    }

    private static Type? DomainType(string fullName) =>
        typeof(Explore.Domain.RegistrationOrder).Assembly.GetType(fullName);

    private static Type? PersistenceType(string fullName) =>
        typeof(ExploreDbContext).Assembly.GetType(fullName);

    private static bool HasProperties(Type type, params string[] names) =>
        names.All(name =>
            type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is not null);

    private static bool HasMethods(Type type, params string[] names) =>
        names.All(name =>
            type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            is not null);

    private static bool HasUniqueIndex(IEntityType entity, params string[] names) =>
        entity.GetIndexes().Any(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(names));

    private static async Task AssertEnumNamesAsync(
        string typeName,
        params string[] names)
    {
        Type? type = DomainType(typeName);
        await Assert.That(type).IsNotNull();
        if (type is null)
        {
            return;
        }

        foreach (string name in names)
        {
            await Assert.That(Enum.GetNames(type)).Contains(name);
        }
    }

    private sealed class RecoveryReflectionSurface(
        Type manifestType,
        Type checkpointType,
        Type reissueType,
        Type repositoryType)
    {
        public Type CheckpointType => checkpointType;
        public Type ReissueType => reissueType;

        public object CreateManifest(
            Guid? operationId = null,
            Guid? tenantId = null,
            int retainedKeyVersion = 3,
            long providerCursor = 90,
            long idempotencyFloor = 100,
            long workerFence = 110,
            int capabilityGeneration = 5,
            int credentialGeneration = 8) =>
            Invoke(
                manifestType,
                "Create",
                operationId ?? Guid.CreateVersion7(),
                tenantId ?? Guid.CreateVersion7(),
                "release-8",
                "schema-7",
                70L,
                UtcNow,
                retainedKeyVersion,
                80L,
                providerCursor,
                idempotencyFloor,
                workerFence,
                capabilityGeneration,
                credentialGeneration,
                new string('a', 64));

        public Exception? CaptureManifestFailure(
            int retainedKeyVersion = 3,
            long providerCursor = 90,
            long idempotencyFloor = 100,
            long workerFence = 110)
        {
            try
            {
                _ = CreateManifest(
                    retainedKeyVersion: retainedKeyVersion,
                    providerCursor: providerCursor,
                    idempotencyFloor: idempotencyFloor,
                    workerFence: workerFence);
                return null;
            }
            catch (TargetInvocationException exception)
            {
                return exception.InnerException;
            }
        }

        public object Begin(object manifest) =>
            Invoke(checkpointType, "Begin", manifest, UtcNow);

        public string Validate(
            object checkpoint,
            string runningReleaseRevision,
            string runningSchemaRevision,
            int minimumRetainedKeyVersion,
            long minimumAuthorityFloor,
            long minimumProviderCursor,
            long minimumIdempotencyFloor,
            long minimumWorkerFence) =>
            Invoke(
                    checkpoint,
                    "Validate",
                    runningReleaseRevision,
                    runningSchemaRevision,
                    minimumRetainedKeyVersion,
                    minimumAuthorityFloor,
                    minimumProviderCursor,
                    minimumIdempotencyFloor,
                    minimumWorkerFence,
                    UtcNow.AddMinutes(1))
                .ToString()!;

        public bool TryRotateBearerAuthority(
            object checkpoint,
            int capabilityGeneration,
            int credentialGeneration,
            long workerFence) =>
            (bool)Invoke(
                checkpoint,
                "TryRotateBearerAuthority",
                capabilityGeneration,
                credentialGeneration,
                workerFence,
                UtcNow.AddMinutes(2));

        public bool TryOpenWorkers(object checkpoint, long workerFence) =>
            (bool)Invoke(
                checkpoint,
                "TryOpenWorkers",
                workerFence,
                UtcNow.AddMinutes(3));

        public bool TryOpenSales(object checkpoint) =>
            (bool)Invoke(
                checkpoint,
                "TryOpenSales",
                UtcNow.AddMinutes(4));

        public string Status(object checkpoint) =>
            Read<object>(checkpoint, "Status").ToString()!;

        public object CreateRepository(ExploreDbContext context) =>
            Activator.CreateInstance(repositoryType, context)
            ?? throw new InvalidOperationException("Recovery repository was not created.");

        public async Task<object> BeginRecoveryAsync(
            object repository,
            object manifest,
            CancellationToken cancellationToken) =>
            await InvokeTaskResultAsync(
                repository,
                "BeginRecoveryAsync",
                manifest,
                UtcNow,
                cancellationToken);

        public object[] Rows(ExploreDbContext context, Type entityType)
        {
            object set = typeof(Microsoft.EntityFrameworkCore.DbContext)
                .GetMethod(nameof(Microsoft.EntityFrameworkCore.DbContext.Set), Type.EmptyTypes)!
                .MakeGenericMethod(entityType)
                .Invoke(context, null)!;
            return ((System.Collections.IEnumerable)set).Cast<object>().ToArray();
        }

        public T Read<T>(object instance, string propertyName) =>
            (T)(instance.GetType().GetProperty(propertyName)!.GetValue(instance)
                ?? throw new InvalidOperationException($"{propertyName} is null."));

        private static object Invoke(object instance, string method, params object[] args) =>
            instance.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance)!
                .Invoke(instance, args)
            ?? throw new InvalidOperationException($"{method} returned null.");

        private static object Invoke(Type type, string method, params object[] args) =>
            type.GetMethod(method, BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, args)
            ?? throw new InvalidOperationException($"{method} returned null.");

        private static async Task<object> InvokeTaskResultAsync(
            object instance,
            string method,
            params object[] args)
        {
            object task = instance.GetType()
                .GetMethod(method, BindingFlags.Public | BindingFlags.Instance)!
                .Invoke(instance, args)!;
            await (Task)task;
            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }
    }
}
