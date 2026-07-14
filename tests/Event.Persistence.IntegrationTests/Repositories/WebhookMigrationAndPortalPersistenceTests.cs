// ABOUTME: Migration-chain and persisted provider-authority tests for the webhook schema freeze.
// ABOUTME: Verifies deterministic upgrade SQL and that legacy-unverified bindings cannot grant portal capability.

using System.Diagnostics;
using System.Globalization;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class WebhookMigrationAndPortalPersistenceTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task MigrationScripts_FromCleanAndCommittedBaselineContainTheSameWebhookFinalState()
    {
        await using var context = CreateRelationalContext();
        var migrator = context.GetService<IMigrator>();
        var cleanScript = migrator.GenerateScript(
            fromMigration: null,
            toMigration: null,
            MigrationsSqlGenerationOptions.Idempotent);
        var baselineScript = migrator.GenerateScript(
            "20260712144721_AddManagedTenantProvisioningOperationOutboxPointer",
            toMigration: null,
            MigrationsSqlGenerationOptions.Idempotent);

        foreach (var marker in new[]
                 {
                     "AddWebhookProviderBindingFoundation",
                     "FreezeWebhookDeliverySchema",
                     "FinalizeWebhookTenantConstraints",
                     "NormalizeWebhookProviderBindingInstanceIdentity",
                     "NormalizeWebhookProviderCapabilities",
                     "webhook_delivery_plan_snapshots",
                     "webhook_local_target_snapshots",
                     "webhook_provider_publications",
                     "webhook_provider_capabilities",
                     "ck_webhook_consumer_provider_bindings_capabilities_known",
                     "LEGACY_JSON_CANONICALIZED"
                 })
        {
            await Assert.That(cleanScript).Contains(marker);
            await Assert.That(baselineScript).Contains(marker);
        }

        await Assert.That(baselineScript).DoesNotContain("HttpClient");
        await Assert.That(baselineScript).DoesNotContain("SendAsync");
        await Assert.That(baselineScript).DoesNotContain("api.svix.com");
    }

    [Test]
    public async Task PersistedBinding_GrantsPortalCapabilityOnlyAfterExactOwnershipVerification()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseInMemoryDatabase($"webhook-portal-binding-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ExploreDbContext(options);
        var tenantId = Guid.CreateVersion7();
        var verifiedConsumer = CreateConsumer(tenantId, "Verified");
        var legacyConsumer = CreateConsumer(tenantId, "Legacy");
        var instanceId = Guid.CreateVersion7();
        var profile = CreateCapabilityProfile();
        var verified = WebhookConsumerProviderBinding.CreatePending(
            tenantId,
            verifiedConsumer.Id,
            instanceId,
            "production",
            profile,
            WebhookProviderCapability.AppPortal);
        verified.VerifyOwnership(tenantId, verifiedConsumer.Id, "verified-app", DateTimeOffset.UtcNow);
        var legacy = WebhookConsumerProviderBinding.CreateLegacyUnverified(
            tenantId,
            legacyConsumer.Id,
            instanceId,
            "production",
            "legacy-app",
            profile);

        context.WebhookConsumers.AddRange(verifiedConsumer, legacyConsumer);
        context.WebhookConsumerProviderBindings.AddRange(verified, legacy);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new WebhookConsumerProviderBindingRepository(context);
        var persistedVerified = await repository.GetVerifiedByConsumerAsync(
            tenantId,
            verifiedConsumer.Id,
            WebhookProviderKind.Svix,
            "production",
            CancellationToken.None);
        var persistedLegacy = await repository.GetVerifiedByConsumerAsync(
            tenantId,
            legacyConsumer.Id,
            WebhookProviderKind.Svix,
            "production",
            CancellationToken.None);

        await Assert.That(persistedVerified).IsNotNull();
        await Assert.That(persistedVerified!.CanIssueAppPortalFor(tenantId, verifiedConsumer.Id)).IsTrue();
        await Assert.That(persistedLegacy).IsNull();
        await Assert.That(legacy.CanIssueAppPortalFor(tenantId, legacyConsumer.Id)).IsFalse();
    }

    [Test]
    public async Task BindingIdentityNormalization_UsesBootstrapIdentityAndDownRestoresAuditEvidence()
    {
        var databaseName = $"webhook_binding_identity_{Guid.NewGuid():N}";
        var connectionString = await CreateDatabaseAsync(databaseName);
        try
        {
            var options = new DbContextOptionsBuilder<ExploreDbContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;
            await using var context = new ExploreDbContext(options);
            context.EnableTenantFilterBypass("Webhook binding identity migration verification.");
            var migrator = context.GetService<IMigrator>();
            await MigrateCurrentSchemaAndSeedLookupsAsync(context, migrator);

            var now = new DateTime(2026, 7, 14, 8, 30, 0, DateTimeKind.Utc);
            var tenant = CreateTenantForMigration();
            var consumer = CreateConsumer(tenant.Id, "Identity normalization");
            var legacyInstanceId = Guid.CreateVersion7();
            var bootstrap = new InstanceBootstrapState
            {
                Id = Guid.CreateVersion7(),
                IsCompleted = true,
                CreatedAt = now,
                CompletedAt = now,
                CompletedByUserId = Guid.CreateVersion7()
            };
            var binding = WebhookConsumerProviderBinding.CreatePending(
                tenant.Id,
                consumer.Id,
                legacyInstanceId,
                "self-hosted",
                WebhookProviderCapabilityProfile.Create(
                    WebhookProviderKind.Svix,
                    "1.96.1",
                    WebhookProviderCapability.AppPortal,
                    "svix-self-hosted-1.96.1-v1",
                    now),
                WebhookProviderCapability.AppPortal);
            binding.VerifyOwnership(tenant.Id, consumer.Id, "app_pre_normalization", now);
            context.Tenants.Add(tenant);
            context.WebhookConsumers.Add(consumer);
            context.InstanceBootstrapStates.Add(bootstrap);
            context.WebhookConsumerProviderBindings.Add(binding);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            await MigrateToHistoricalBoundaryAsync(
                context,
                migrator,
                "20260714080458_RetireLegacyWebhookProviderLinks");

            await migrator.MigrateAsync();
            var normalized = await context.WebhookConsumerProviderBindings.SingleAsync();
            var normalizationAudit = await context.AuditLogs.SingleAsync(audit =>
                audit.Action == "WebhookBindingIdentityNormalized");

            await Assert.That(normalized.InstanceId).IsEqualTo(bootstrap.Id);
            await Assert.That(normalized.ApplicationUid).IsEqualTo(
                WebhookConsumerProviderBinding.CreateApplicationUid(bootstrap.Id, consumer.Id));
            await Assert.That(normalized.VerificationState)
                .IsEqualTo(WebhookProviderBindingVerificationState.LegacyUnverified);
            await Assert.That(normalized.IsEnabled).IsFalse();
            await Assert.That(normalizationAudit.OldValues).Contains(legacyInstanceId.ToString("D"));
            await Assert.That(normalizationAudit.NewValues).DoesNotContain("app_pre_normalization");

            context.ChangeTracker.Clear();
            await migrator.MigrateAsync("20260714080458_RetireLegacyWebhookProviderLinks");
            var restored = await context.WebhookConsumerProviderBindings.SingleAsync();

            await Assert.That(restored.InstanceId).IsEqualTo(legacyInstanceId);
            await Assert.That(restored.VerificationState)
                .IsEqualTo(WebhookProviderBindingVerificationState.Verified);
            await Assert.That(restored.IsEnabled).IsTrue();
            await Assert.That(await context.AuditLogs.CountAsync(audit =>
                audit.Action == "WebhookBindingIdentityNormalized")).IsEqualTo(0);
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    [Test]
    public async Task LegacyProviderLinkUpgrade_PreservesEvidenceAndRemovesRetiredTable()
    {
        var databaseName = $"webhook_link_upgrade_{Guid.NewGuid():N}";
        var restoredDatabaseName = $"webhook_link_restore_{Guid.NewGuid():N}";
        var backupPath = Path.Combine(Path.GetTempPath(), $"{databaseName}.dump");
        var connectionString = await CreateDatabaseAsync(databaseName);
        try
        {
            var options = new DbContextOptionsBuilder<ExploreDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(300))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;
            await using var context = new ExploreDbContext(options);
            context.EnableTenantFilterBypass("Webhook legacy-link migration verification.");
            var migrator = context.GetService<IMigrator>();
            await MigrateCurrentSchemaAndSeedLookupsAsync(context, migrator);

            var now = new DateTime(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc);
            var tenant = CreateTenantForMigration();
            var consumer = CreateConsumer(tenant.Id, "Legacy migration");
            consumer.CreatedAt = now;
            var bootstrap = new InstanceBootstrapState
            {
                Id = Guid.CreateVersion7(),
                IsCompleted = true,
                CreatedAt = now,
                CompletedAt = now,
                CompletedByUserId = Guid.CreateVersion7()
            };
            var endpoint = new WebhookEndpoint
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenant.Id,
                ConsumerId = consumer.Id,
                Url = "https://example.org/webhooks/legacy",
                Status = WebhookEndpointStatus.Active,
                SecretRef = "webhooks/legacy",
                SecretVersion = 1,
                MaxAttempts = 8,
                TimeoutSeconds = 15,
                CreatedAt = now
            };
            var queuedMessage = CreateMessageForMigration(
                tenant.Id,
                consumer.Id,
                "queued",
                now);
            var manualMessage = CreateMessageForMigration(
                tenant.Id,
                consumer.Id,
                "manual",
                now.AddSeconds(1));
            context.Tenants.Add(tenant);
            context.InstanceBootstrapStates.Add(bootstrap);
            context.WebhookConsumers.Add(consumer);
            context.WebhookEndpoints.Add(endpoint);
            context.WebhookMessages.AddRange(queuedMessage, manualMessage);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            await MigrateToHistoricalBoundaryAsync(
                context,
                migrator,
                "20260713232047_NormalizeWebhookProviderPublicationAttemptOutcomes");

            var queuedLinkId = Guid.CreateVersion7();
            var manualLinkId = Guid.CreateVersion7();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO webhook_provider_links (
                    id, tenant_id, consumer_id, endpoint_id, message_id, provider,
                    external_app_id, external_endpoint_id, external_message_id,
                    sync_state, last_synced_at, retry_count, created_at)
                VALUES (
                    {queuedLinkId}, {tenant.Id}, {consumer.Id}, {endpoint.Id}, {queuedMessage.Id}, 1,
                    {" legacy-app "}, {" legacy-endpoint "}, {"legacy-provider-message"},
                    2, {now.AddMinutes(1)}, 2, {now});

                INSERT INTO webhook_provider_links (
                    id, tenant_id, consumer_id, message_id, provider, sync_state,
                    last_error_category, retry_count, created_at)
                VALUES (
                    {manualLinkId}, {tenant.Id}, {consumer.Id}, {manualMessage.Id}, 1, 1,
                    {"legacy_transport_unknown"}, 0, {now.AddSeconds(1)});

                CREATE TEMP TABLE legacy_volume_ids AS
                SELECT series,
                       uuidv7() AS message_id,
                       uuidv7() AS link_id
                FROM generate_series(1, 10000) AS series;

                INSERT INTO webhook_messages (
                    id, tenant_id, event_type, event_id, aggregate_kind, aggregate_id,
                    consumer_id, payload_bytes, payload_hash, payload_byte_length,
                    payload_provenance_id, content_type, content_encoding, occurred_at,
                    materialized_at, payload_retention_until, created_at)
                SELECT ids.message_id,
                       {tenant.Id},
                       'event.updated',
                       'volume-' || ids.series,
                       'event',
                       uuidv7(),
                       {consumer.Id},
                       convert_to({"{}"}, 'UTF8'),
                       'sha256:44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a',
                       2,
                       1,
                       'application/json',
                       'utf-8',
                       {now},
                       {now} + ids.series * INTERVAL '1 microsecond',
                       {now.AddDays(14)},
                       {now} + ids.series * INTERVAL '1 microsecond'
                FROM legacy_volume_ids ids;

                INSERT INTO webhook_provider_links (
                    id, tenant_id, consumer_id, message_id, provider, sync_state,
                    retry_count, created_at)
                SELECT ids.link_id,
                       {tenant.Id},
                       {consumer.Id},
                       ids.message_id,
                       1,
                       1,
                       0,
                       {now} + ids.series * INTERVAL '1 microsecond'
                FROM legacy_volume_ids ids;

                DROP TABLE legacy_volume_ids;
                """);

            using var lockMonitorCancellation = new CancellationTokenSource();
            var lockMonitor = MonitorWaitingWebhookLocksAsync(
                connectionString,
                lockMonitorCancellation.Token);
            var migrationTimer = Stopwatch.StartNew();
            try
            {
                await migrator.MigrateAsync();
            }
            finally
            {
                migrationTimer.Stop();
                await lockMonitorCancellation.CancelAsync();
            }

            var maxWaitingLocks = await lockMonitor;
            context.ChangeTracker.Clear();

            var binding = await context.WebhookConsumerProviderBindings.SingleAsync();
            var publicationCount = await context.WebhookProviderPublications.CountAsync();
            var publicationAttemptCount = await context.WebhookProviderPublicationAttempts.CountAsync();
            var queuedPublication = await context.WebhookProviderPublications
                .SingleAsync(publication => publication.WebhookMessageId == queuedMessage.Id);
            var manualPublication = await context.WebhookProviderPublications
                .SingleAsync(publication => publication.WebhookMessageId == manualMessage.Id);
            var persistedEndpoint = await context.WebhookEndpoints.SingleAsync();
            var legacyTable = await ReadLegacyTableNameAsync(context);
            var publicationChecksum = await ReadPublicationChecksumAsync(connectionString);

            var backupTimer = Stopwatch.StartNew();
            await RunPostgresToolAsync("pg_dump", connectionString,
                "--format=custom",
                "--no-owner",
                "--no-acl",
                $"--file={backupPath}");
            backupTimer.Stop();
            var restoredConnectionString = await CreateDatabaseAsync(restoredDatabaseName);
            var restoreTimer = Stopwatch.StartNew();
            await RunPostgresToolAsync("pg_restore", restoredConnectionString,
                "--no-owner",
                "--no-acl",
                "--exit-on-error",
                backupPath);
            restoreTimer.Stop();
            var restoredPublicationChecksum = await ReadPublicationChecksumAsync(restoredConnectionString);
            var restoredLegacyTable = await ReadLegacyTableNameAsync(restoredConnectionString);

            await Assert.That(migrationTimer.Elapsed).IsLessThan(TimeSpan.FromMinutes(5));
            await Assert.That(backupTimer.Elapsed).IsLessThan(TimeSpan.FromMinutes(5));
            await Assert.That(restoreTimer.Elapsed).IsLessThan(TimeSpan.FromMinutes(5));
            await Assert.That(maxWaitingLocks).IsEqualTo(0);
            await Assert.That(legacyTable).IsNull();
            await Assert.That(restoredLegacyTable).IsNull();
            await Assert.That(restoredPublicationChecksum).IsEqualTo(publicationChecksum);
            await Assert.That(await context.WebhookDeliveryPlanSnapshots.CountAsync()).IsEqualTo(10_002);
            await Assert.That(binding.VerificationState)
                .IsEqualTo(WebhookProviderBindingVerificationState.LegacyUnverified);
            await Assert.That(binding.InstanceId).IsEqualTo(bootstrap.Id);
            await Assert.That(binding.ApplicationUid).IsEqualTo(
                WebhookConsumerProviderBinding.CreateApplicationUid(bootstrap.Id, consumer.Id));
            await Assert.That(binding.IsEnabled).IsFalse();
            await Assert.That(binding.ExternalApplicationId).IsEqualTo("legacy-app");
            await Assert.That(binding.NormalizedExternalApplicationId).IsEqualTo("LEGACY-APP");
            await Assert.That(persistedEndpoint.ProviderEndpointId).IsEqualTo("legacy-endpoint");
            await Assert.That(publicationCount).IsEqualTo(10_002);
            await Assert.That(publicationAttemptCount).IsEqualTo(10_002);
            await Assert.That(queuedPublication.Status)
                .IsEqualTo(WebhookProviderPublicationStatus.ProviderQueued);
            await Assert.That(queuedPublication.ExternalProviderMessageId)
                .IsEqualTo("legacy-provider-message");
            await Assert.That(manualPublication.Status)
                .IsEqualTo(WebhookProviderPublicationStatus.ManualReconciliation);
            await Assert.That(manualPublication.FailureCategory)
                .IsEqualTo("legacy_transport_unknown");
        }
        finally
        {
            await DropDatabaseAsync(restoredDatabaseName);
            await DropDatabaseAsync(databaseName);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    private static ExploreDbContext CreateRelationalContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql("Host=localhost;Database=webhook_model;Username=unused;Password=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ExploreDbContext(options);
    }

    private static WebhookConsumer CreateConsumer(Guid tenantId, string name) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        ConsumerKind = WebhookConsumerKind.Tenant,
        Name = name,
        Status = WebhookConsumerStatus.Active,
        ProviderMode = WebhookProviderMode.Svix
    };

    private static WebhookProviderCapabilityProfile CreateCapabilityProfile() =>
        WebhookProviderCapabilityProfile.Create(
            WebhookProviderKind.Svix,
            "1.84.0",
            WebhookProviderCapability.AppPortal,
            "svix-1.84.0-v1",
            DateTimeOffset.UtcNow);

    private static async Task MigrateCurrentSchemaAndSeedLookupsAsync(
        ExploreDbContext context,
        IMigrator migrator)
    {
        await migrator.MigrateAsync();
        await LookupTableSeeder.SeedAsync(context);
        context.ChangeTracker.Clear();
    }

    private static async Task MigrateToHistoricalBoundaryAsync(
        ExploreDbContext context,
        IMigrator migrator,
        string historicalMigration)
    {
        await migrator.MigrateAsync(historicalMigration);
        context.ChangeTracker.Clear();
    }

    private async Task<string> CreateDatabaseAsync(string databaseName)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();
        return new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = databaseName
        }.ConnectionString;
    }

    private async Task DropDatabaseAsync(string databaseName)
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string?> ReadLegacyTableNameAsync(ExploreDbContext context)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.webhook_provider_links')::text";
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : (string)result;
    }

    private static async Task<string?> ReadLegacyTableNameAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.webhook_provider_links')::text";
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : (string)result;
    }

    private static async Task<string> ReadPublicationChecksumAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT md5(string_agg(id::text || ':' || status_id::text, ',' ORDER BY id)) " +
            "FROM webhook_provider_publications";
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<int> MonitorWaitingWebhookLocksAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        var maxWaitingLocks = 0;
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
            do
            {
                await using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT COUNT(*) FROM pg_locks l " +
                    "JOIN pg_class c ON c.oid = l.relation " +
                    "WHERE NOT l.granted AND c.relname = ANY (ARRAY[" +
                    "'webhook_provider_links', 'webhook_messages', " +
                    "'webhook_delivery_plan_snapshots', 'webhook_consumer_provider_bindings', " +
                    "'webhook_provider_publications', 'webhook_provider_publication_attempts'])";
                var waitingLocks = Convert.ToInt32(
                    await command.ExecuteScalarAsync(cancellationToken),
                    CultureInfo.InvariantCulture);
                maxWaitingLocks = Math.Max(maxWaitingLocks, waitingLocks);
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));

        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }

        return maxWaitingLocks;
    }

    private static async Task RunPostgresToolAsync(
        string executable,
        string connectionString,
        params string[] additionalArguments)
    {
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var startInfo = new ProcessStartInfo(executable)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add($"--host={connection.Host}");
        startInfo.ArgumentList.Add($"--port={connection.Port.ToString(CultureInfo.InvariantCulture)}");
        startInfo.ArgumentList.Add($"--username={connection.Username}");
        startInfo.ArgumentList.Add($"--dbname={connection.Database}");
        foreach (var argument in additionalArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["PGPASSWORD"] = connection.Password;
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {executable}.");
        var standardError = process.StandardError.ReadToEndAsync();
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        await standardOutput;
        var error = await standardError;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{executable} failed with exit code {process.ExitCode}: {error}");
        }
    }

    private static Tenant CreateTenantForMigration() => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = "Webhook migration tenant",
        Slug = $"webhook-migration-{Guid.NewGuid():N}",
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!,
        CreatedAt = new DateTime(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc)
    };

    private static WebhookMessage CreateMessageForMigration(
        Guid tenantId,
        Guid consumerId,
        string eventId,
        DateTime materializedAt) =>
        WebhookMessage.Create(
            Guid.CreateVersion7(),
            tenantId,
            "event.updated",
            eventId,
            "event",
            Guid.CreateVersion7(),
            consumerId,
            System.Text.Encoding.UTF8.GetBytes($"{{\"id\":\"{eventId}\"}}"),
            "application/json",
            "utf-8",
            materializedAt.AddMinutes(-1),
            materializedAt.AddDays(14),
            materializedAt);
}
