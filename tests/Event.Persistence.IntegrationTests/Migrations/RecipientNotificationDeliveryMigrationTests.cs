// ABOUTME: Verifies the pre-1.0 recipient-delivery ledger reset in an isolated PostgreSQL Testcontainer.
// ABOUTME: Proves exact reset scope, preserved notification canaries, canonical lookups, and required recipient constraints.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RecipientDeliveryMigrationDb")]
public sealed class RecipientNotificationDeliveryMigrationTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    private const string PreviousMigration = "20260717115227_OperateIncomingWebhookEffectOutbox";
    private const string TargetMigration = "20260717131038_NormalizeRecipientNotificationDelivery";
    private static readonly Guid TenantId = Guid.Parse("019b3333-2000-7000-8000-000000000001");
    private static readonly Guid OtherTenantId = Guid.Parse("019b3333-2000-7000-8000-000000000010");
    private static readonly Guid RecipientUserId = Guid.Parse("019b3333-2000-7000-8000-000000000002");
    private static readonly Guid OtherUserId = Guid.Parse("019b3333-2000-7000-8000-000000000003");
    private static readonly Guid LegacyIntentId = Guid.Parse("019b3333-2000-7000-8000-000000000004");
    private static readonly Guid LegacyEmailId = Guid.Parse("019b3333-2000-7000-8000-000000000005");
    private static readonly Guid NotificationCanaryId = Guid.Parse("019b3333-2000-7000-8000-000000000006");
    private static readonly Guid CurrentIntentId = Guid.Parse("019b3333-2000-7000-8000-000000000021");
    private static readonly Guid CurrentEmailId = Guid.Parse("019b3333-2000-7000-8000-000000000022");
    private static readonly Guid CurrentNotificationId = Guid.Parse("019b3333-2000-7000-8000-000000000025");
    private static readonly Guid EventCanaryId = Guid.Parse("019b3333-2000-7000-8000-000000000011");
    private static readonly Guid RegistrationCanaryId = Guid.Parse("019b3333-2000-7000-8000-000000000012");
    private static readonly Guid ReportCanaryId = Guid.Parse("019b3333-2000-7000-8000-000000000013");
    private static readonly Guid AuditCanaryId = Guid.Parse("019b3333-2000-7000-8000-000000000014");
    private static readonly Guid SettingCanaryId = Guid.Parse("019b3333-2000-7000-8000-000000000015");

    [Test]
    public async Task UpDownUp_RehearsesResetScopeConstraintsAndLookupTransitions()
    {
        var connectionIdentity = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        await Assert.That(connectionIdentity.Database).StartsWith("recipient_delivery_migration_");
        await Assert.That(connectionIdentity.Host is "127.0.0.1" or "localhost").IsTrue();

        await using var context = CreateDbContext();
        IMigrator migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        await SeedPreNormalizationCanariesAsync(context);

        await migrator.MigrateAsync(TargetMigration);
        await Assert.That(ReadPendingModelOperations(context)).IsEmpty();

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await AssertResetLedgersEmptyAsync(connection);
            await AssertPreservedCanariesAsync(connection);

            await Assert.That(await ReadScalarStringAsync(
                connection,
                "SELECT master_code FROM notification_preference_channels WHERE id = 2")).IsEqualTo("in_app");
            await Assert.That(await ReadScalarStringAsync(
                connection,
                "SELECT master_code FROM notification_delivery_statuses WHERE id = 2")).IsEqualTo("QUEUED");
            await Assert.That(await ReadScalarStringAsync(
                connection,
                "SELECT master_code FROM notification_delivery_statuses WHERE id = 3")).IsEqualTo("DELIVERED");
            await Assert.That(await ReadCountAsync(connection, "notification_delivery_policies")).IsEqualTo(8L);
            await Assert.That(await ConstraintExistsAsync(
                connection,
                "fk_email_dispatch_outbox_managed_operation_tenant")).IsTrue();
            await Assert.That(await ConstraintExistsAsync(
                connection,
                "fk_notification_deliveries_notification_same_intent")).IsTrue();
            await Assert.That(await ConstraintExistsAsync(
                connection,
                "fk_notification_deliveries_notification_tenant")).IsTrue();
            await Assert.That(await ReadCountAsync(
                connection,
                "notification_delivery_statuses",
                "id BETWEEN 7 AND 9")).IsEqualTo(3L);

            await Assert.That(await IsColumnRequiredAsync(connection, "notification_intents", "recipient_user_id")).IsTrue();
            await Assert.That(await IsColumnRequiredAsync(connection, "email_dispatch_outbox", "recipient_user_id")).IsTrue();
            await Assert.That(await IsColumnRequiredAsync(connection, "email_dispatch_outbox", "notification_intent_id")).IsTrue();

            await InsertCurrentRecipientGraphAsync(connection);
            await Assert.That(await ReadCountAsync(connection, "notification_intents")).IsEqualTo(1L);
            await Assert.That(await ReadCountAsync(connection, "email_dispatch_outbox")).IsEqualTo(1L);
            await Assert.That(await ReadCountAsync(connection, "notification_deliveries")).IsEqualTo(1L);
            await AssertRecipientDeliveryConstraintsAsync(connection);
        }

        await migrator.MigrateAsync(PreviousMigration);
        await using (var downConnection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await downConnection.OpenAsync();
            await AssertResetLedgersEmptyAsync(downConnection);
            await AssertPreservedCanariesAsync(downConnection);
            await Assert.That(await ReadScalarStringAsync(
                downConnection,
                "SELECT master_code FROM notification_preference_channels WHERE id = 2")).IsEqualTo("in-app");
            await Assert.That(await ReadScalarStringAsync(
                downConnection,
                "SELECT master_code FROM notification_delivery_statuses WHERE id = 2")).IsEqualTo("LINKED_TO_EMAIL_DISPATCH");
            await Assert.That(await ReadScalarStringAsync(
                downConnection,
                "SELECT master_code FROM notification_delivery_statuses WHERE id = 3")).IsEqualTo("SENT");
            await Assert.That(await ReadCountAsync(
                downConnection,
                "notification_delivery_statuses",
                "id BETWEEN 7 AND 9")).IsEqualTo(0L);
            await Assert.That(await TableExistsAsync(downConnection, "notification_delivery_policies")).IsFalse();
            await Assert.That(await ColumnExistsAsync(downConnection, "notification_intents", "recipient_user_id")).IsFalse();
            await Assert.That(await IsColumnRequiredAsync(downConnection, "notification_intents", "user_id")).IsFalse();
            await Assert.That(await IsColumnRequiredAsync(downConnection, "email_dispatch_outbox", "user_id")).IsFalse();
            await Assert.That(await ReadCountAsync(
                downConnection,
                "notifications",
                $"id = '{CurrentNotificationId}' AND title = 'Current linked notification'")).IsEqualTo(1L);
        }

        await migrator.MigrateAsync(TargetMigration);
        await using var secondUpConnection = new NpgsqlConnection(fixture.ConnectionString);
        await secondUpConnection.OpenAsync();
        await AssertResetLedgersEmptyAsync(secondUpConnection);
        await AssertPreservedCanariesAsync(secondUpConnection);
        await Assert.That(await ReadScalarStringAsync(
            secondUpConnection,
            "SELECT master_code FROM notification_delivery_statuses WHERE id = 2")).IsEqualTo("QUEUED");
        await Assert.That(await ReadScalarStringAsync(
            secondUpConnection,
            "SELECT master_code FROM notification_delivery_statuses WHERE id = 3")).IsEqualTo("DELIVERED");
        await Assert.That(await ReadScalarStringAsync(
            secondUpConnection,
            "SELECT master_code FROM notification_preference_channels WHERE id = 2")).IsEqualTo("in_app");
        await Assert.That(await ReadCountAsync(
            secondUpConnection,
            "notification_delivery_statuses",
            "id BETWEEN 7 AND 9")).IsEqualTo(3L);
        await Assert.That(await ReadCountAsync(secondUpConnection, "notification_delivery_policies")).IsEqualTo(8L);
        await Assert.That(await ReadCountAsync(
            secondUpConnection,
            "notifications",
            $"id = '{CurrentNotificationId}' AND notification_intent_id IS NULL")).IsEqualTo(1L);
    }

    private ExploreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new ExploreDbContext(options);
    }

    private static string[] ReadPendingModelOperations(ExploreDbContext context)
    {
        IMigrationsAssembly migrationsAssembly = context.GetService<IMigrationsAssembly>();
        IMigrationsModelDiffer modelDiffer = context.GetService<IMigrationsModelDiffer>();
        IModel runtimeModel = context.GetService<IDesignTimeModel>().Model;
        IModel rawSnapshotModel = migrationsAssembly.ModelSnapshot?.Model
            ?? throw new InvalidOperationException("ExploreDbContext migration snapshot was not found.");
        IModel snapshotModel = context.GetService<IModelRuntimeInitializer>()
            .Initialize(rawSnapshotModel, designTime: true, validationLogger: null);

        return modelDiffer
            .GetDifferences(snapshotModel.GetRelationalModel(), runtimeModel.GetRelationalModel())
            .Select(DescribeOperation)
            .ToArray();
    }

    private static string DescribeOperation(MigrationOperation operation) => operation switch
    {
        AddColumnOperation value => $"AddColumn:{value.Table}.{value.Name}",
        AlterColumnOperation value => $"AlterColumn:{value.Table}.{value.Name}",
        DropColumnOperation value => $"DropColumn:{value.Table}.{value.Name}",
        CreateIndexOperation value =>
            $"CreateIndex:{value.Table}.{value.Name}[{string.Join(',', value.Columns)}]" +
            $":unique={value.IsUnique}:filter={value.Filter ?? "<none>"}",
        DropIndexOperation value => $"DropIndex:{value.Table}.{value.Name}",
        AddForeignKeyOperation value => $"AddForeignKey:{value.Table}.{value.Name}",
        DropForeignKeyOperation value => $"DropForeignKey:{value.Table}.{value.Name}",
        AddUniqueConstraintOperation value => $"AddUnique:{value.Table}.{value.Name}",
        DropUniqueConstraintOperation value => $"DropUnique:{value.Table}.{value.Name}",
        AddCheckConstraintOperation value => $"AddCheck:{value.Table}.{value.Name}",
        DropCheckConstraintOperation value => $"DropCheck:{value.Table}.{value.Name}",
        _ => operation.GetType().Name
    };

    private static async Task SeedPreNormalizationCanariesAsync(ExploreDbContext context)
    {
        DateTime createdAt = new(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);
        Guid tenantUserId = Guid.Parse("019b3333-2000-7000-8000-000000000007");
        Guid otherTenantUserId = Guid.Parse("019b3333-2000-7000-8000-000000000008");
        Guid deliveryId = Guid.Parse("019b3333-2000-7000-8000-000000000009");
        Guid attemptId = Guid.Parse("019b3333-2000-7000-8000-00000000000a");
        Guid receiptId = Guid.Parse("019b3333-2000-7000-8000-00000000000b");
        Guid delegationId = Guid.Parse("019b3333-2000-7000-8000-00000000000c");
        Guid publishEventId = Guid.Parse("019b3333-2000-7000-8000-00000000000d");
        Guid actorId = Guid.Parse("019b3333-2000-7000-8000-000000000016");

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tenant_statuses (id, master_code, full_name, is_active_state)
            VALUES (1, 'ACTIVE', 'Active', true);
            INSERT INTO tenants (id, tenant_status_id, full_name, slug, created_at)
            VALUES
                ({TenantId}, 1, 'Migration test tenant', 'recipient-delivery-reset-test', {createdAt}),
                ({OtherTenantId}, 1, 'Other migration test tenant', 'recipient-delivery-other-test', {createdAt});
            INSERT INTO users (id, concurrency_stamp, created_at, is_deleted, email_verified)
            VALUES
                ({RecipientUserId}, {Guid.Parse("019b3333-2000-7000-8000-00000000000e")}, {createdAt}, false, true),
                ({OtherUserId}, {Guid.Parse("019b3333-2000-7000-8000-00000000000f")}, {createdAt}, false, true);
            INSERT INTO tenant_users (id, tenant_id, user_id, status_id, created_at, is_deleted)
            VALUES
                ({tenantUserId}, {TenantId}, {RecipientUserId}, 1, {createdAt}, false),
                ({otherTenantUserId}, {TenantId}, {OtherUserId}, 1, {createdAt}, false);

            INSERT INTO actor_types (id, master_code, full_name)
            VALUES (1, 'USER', 'User');
            INSERT INTO actors
                (id, actor_type_id, concurrency_stamp, created_at, is_deleted, tenant_id, user_id)
            VALUES
                ({actorId}, 1, {Guid.Parse("019b3333-2000-7000-8000-000000000017")},
                 {createdAt}, false, {TenantId}, {RecipientUserId});
            INSERT INTO event_formats (id, master_code, full_name)
            VALUES (1, 'IN_PERSON', 'In person');
            INSERT INTO event_statuses (id, master_code, full_name)
            VALUES (1, 'DRAFT', 'Draft');
            INSERT INTO visibility_types (id, master_code, full_name)
            VALUES (1, 'PUBLIC', 'Public');
            INSERT INTO registration_scopes (id, master_code, full_name)
            VALUES (1, 'WHOLE_EVENT', 'Whole event');
            INSERT INTO approval_statuses (id, master_code, full_name)
            VALUES (1, 'PENDING', 'Pending');
            INSERT INTO setting_value_types (id, master_code, full_name)
            VALUES (1, 'STRING', 'String');

            INSERT INTO events
                (id, actor_id, concurrency_stamp, created_at, event_format_id, event_status_id,
                 is_deleted, is_registration_required, public_code, tenant_id, title, visibility_type_id)
            VALUES
                ({EventCanaryId}, {actorId}, {Guid.Parse("019b3333-2000-7000-8000-000000000018")},
                 {createdAt}, 1, 1, false, true, 'MIGRATE01', {TenantId}, 'Preserved event', 1);
            INSERT INTO event_registration_intents
                (id, approval_status_id, concurrency_stamp, created_at, event_id, is_deleted,
                 registration_scope_id, tenant_id, user_id)
            VALUES
                ({RegistrationCanaryId}, 1, {Guid.Parse("019b3333-2000-7000-8000-000000000019")},
                 {createdAt}, {EventCanaryId}, false, 1, {TenantId}, {RecipientUserId});
            INSERT INTO event_reports
                (id, concurrency_stamp, created_at, event_id, priority, reason_code,
                 reporter_contact_consent, reporter_kind, source_kind, status, tenant_id)
            VALUES
                ({ReportCanaryId}, {Guid.Parse("019b3333-2000-7000-8000-00000000001a")},
                 {createdAt}, {EventCanaryId}, 1, 'MIGRATION_CANARY', false, 1, 1, 1, {TenantId});
            INSERT INTO audit_logs
                (id, action, entity_id, entity_type, tenant_id, timestamp)
            VALUES
                ({AuditCanaryId}, 'MIGRATION_CANARY', 'preserved-audit', 'MigrationProbe', {TenantId}, {createdAt});
            INSERT INTO system_settings
                (id, setting_key, setting_value_type_id, value, created_at)
            VALUES
                ({SettingCanaryId}, 'Migration:PreservedSetting', 1, 'preserved-value', {createdAt});

            INSERT INTO notification_types (id, master_code, full_name)
            VALUES (1, 'SYSTEM', 'System');
            INSERT INTO notification_scope_types (id, master_code, full_name)
            VALUES (1, 'USER', 'User');
            INSERT INTO notifications
                (id, tenant_id, user_id, notification_type_id, title, body, deduplication_key,
                 notification_scope_id, is_read, is_archived, created_at, is_deleted)
            VALUES
                ({NotificationCanaryId}, {TenantId}, {RecipientUserId}, 1,
                 'Preserved notification', 'This notification must survive the ledger reset.',
                 'migration-notification-canary', 1, false, false, {createdAt}, false);

            INSERT INTO notification_categories (id, master_code, full_name)
            VALUES (4, 'REGISTRATION_LIFECYCLE', 'Registration lifecycle');
            INSERT INTO notification_ownership_types (id, master_code, full_name)
            VALUES (1, 'ISLAMU_EVENT', 'ISLAMU Event');
            INSERT INTO notification_recipient_kinds (id, master_code, full_name)
            VALUES (1, 'USER', 'User');
            INSERT INTO notification_intent_statuses (id, master_code, full_name)
            VALUES (3, 'DISPATCH_QUEUED', 'Dispatch queued');
            INSERT INTO notification_preference_channels
                (id, master_code, full_name, description, sort_order)
            VALUES
                (1, 'email', 'Email', 'Email delivery', 10),
                (2, 'in-app', 'In-App', 'In-application notifications', 20),
                (3, 'push', 'Browser Push', 'Browser push delivery', 30);
            INSERT INTO notification_delivery_statuses (id, master_code, full_name, description)
            VALUES
                (1, 'PENDING', 'Pending', 'Delivery audit row is pending dispatch linkage'),
                (2, 'LINKED_TO_EMAIL_DISPATCH', 'Linked to email dispatch', 'Delivery has a linked EmailDispatchOutbox row'),
                (3, 'SENT', 'Sent', 'Delivery was sent successfully'),
                (4, 'SKIPPED', 'Skipped', 'Delivery was skipped by policy or preference'),
                (5, 'FAILED', 'Failed', 'Delivery failed and may be retried or reviewed'),
                (6, 'DEAD_LETTERED', 'Dead lettered', 'Delivery exhausted retry policy and is retained for operator review');
            INSERT INTO external_workflow_provider_kinds (id, master_code, full_name)
            VALUES (1, 'NONE', 'None');
            INSERT INTO notification_external_delegation_statuses (id, master_code, full_name)
            VALUES (1, 'PENDING', 'Pending');

            INSERT INTO notification_intents
                (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                 template_key, deduplication_key, user_id, created_at, is_deleted)
            VALUES
                ({LegacyIntentId}, {TenantId}, 4, 1, 1, 3,
                 'legacy.reset.probe', 'legacy-reset-probe', {RecipientUserId}, {createdAt}, false);
            INSERT INTO notification_deliveries
                (id, tenant_id, notification_intent_id, status_id, queued_at, created_at)
            VALUES ({deliveryId}, {TenantId}, {LegacyIntentId}, 1, {createdAt}, {createdAt});
            INSERT INTO notification_external_delegations
                (id, tenant_id, notification_intent_id, provider_kind_id, status_id,
                 recipient_kind_id, template_key, created_at)
            VALUES ({delegationId}, {TenantId}, {LegacyIntentId}, 1, 1, 1, 'legacy.reset.probe', {createdAt});

            INSERT INTO email_dispatch_outbox
                (id, tenant_id, publish_event_id, kind, source_type, source_id, user_id,
                 recipient_email, subject, status, attempt_count, max_attempts,
                 rabbit_mq_publish_attempt_count, created_at, is_deleted)
            VALUES
                ({LegacyEmailId}, {TenantId}, {publishEventId}, 1, 'legacy-reset-probe', {LegacyIntentId}, {RecipientUserId},
                 'legacy@example.test', 'Legacy reset probe', 1, 0, 5, 0, {createdAt}, false);
            INSERT INTO email_dispatch_attempts
                (id, tenant_id, email_dispatch_outbox_id, attempt_number, transport, outcome, started_at, created_at)
            VALUES ({attemptId}, {TenantId}, {LegacyEmailId}, 1, 'smtp', 1, {createdAt}, {createdAt});
            INSERT INTO email_dispatch_receipts
                (id, tenant_id, publish_event_id, email_dispatch_outbox_id, status, first_seen_at, created_at)
            VALUES ({receiptId}, {TenantId}, {publishEventId}, {LegacyEmailId}, 1, {createdAt}, {createdAt});
            """);
    }

    private static async Task AssertResetLedgersEmptyAsync(NpgsqlConnection connection)
    {
        foreach (string table in new[]
                 {
                     "notification_deliveries",
                     "email_dispatch_receipts",
                     "email_dispatch_attempts",
                     "email_dispatch_outbox",
                     "notification_external_delegations",
                     "notification_intents"
                 })
        {
            await Assert.That(await ReadCountAsync(connection, table)).IsEqualTo(0L);
        }
    }

    private static async Task AssertPreservedCanariesAsync(NpgsqlConnection connection)
    {
        await Assert.That(await ReadCountAsync(connection, "tenants")).IsEqualTo(2L);
        await Assert.That(await ReadCountAsync(connection, "users")).IsEqualTo(2L);
        await Assert.That(await ReadCountAsync(connection, "events", $"id = '{EventCanaryId}'")).IsEqualTo(1L);
        await Assert.That(await ReadCountAsync(
            connection,
            "event_registration_intents",
            $"id = '{RegistrationCanaryId}'")).IsEqualTo(1L);
        await Assert.That(await ReadCountAsync(connection, "event_reports", $"id = '{ReportCanaryId}'")).IsEqualTo(1L);
        await Assert.That(await ReadCountAsync(connection, "audit_logs", $"id = '{AuditCanaryId}'")).IsEqualTo(1L);
        await Assert.That(await ReadCountAsync(connection, "system_settings", $"id = '{SettingCanaryId}'")).IsEqualTo(1L);
        await Assert.That(await ReadScalarStringAsync(
            connection,
            $"SELECT full_name FROM tenants WHERE id = '{TenantId}'")).IsEqualTo("Migration test tenant");
        await Assert.That(await ReadScalarBoolAsync(
            connection,
            $"SELECT email_verified FROM users WHERE id = '{RecipientUserId}'")).IsTrue();
        await Assert.That(await ReadScalarStringAsync(
            connection,
            $"SELECT title FROM events WHERE id = '{EventCanaryId}'")).IsEqualTo("Preserved event");
        await Assert.That(await ReadScalarIntAsync(
            connection,
            $"SELECT approval_status_id FROM event_registration_intents WHERE id = '{RegistrationCanaryId}'"))
            .IsEqualTo(1);
        await Assert.That(await ReadScalarStringAsync(
            connection,
            $"SELECT reason_code FROM event_reports WHERE id = '{ReportCanaryId}'")).IsEqualTo("MIGRATION_CANARY");
        await Assert.That(await ReadScalarIntAsync(
            connection,
            $"SELECT status FROM event_reports WHERE id = '{ReportCanaryId}'")).IsEqualTo(1);
        await Assert.That(await ReadScalarStringAsync(
            connection,
            $"SELECT action FROM audit_logs WHERE id = '{AuditCanaryId}'")).IsEqualTo("MIGRATION_CANARY");
        await Assert.That(await ReadScalarStringAsync(
            connection,
            $"SELECT entity_id FROM audit_logs WHERE id = '{AuditCanaryId}'")).IsEqualTo("preserved-audit");
        await Assert.That(await ReadScalarStringAsync(
            connection,
            $"SELECT value FROM system_settings WHERE id = '{SettingCanaryId}'")).IsEqualTo("preserved-value");

        bool hasNotificationIntentColumn = await ColumnExistsAsync(
            connection,
            "notifications",
            "notification_intent_id");
        await using var notificationCommand = connection.CreateCommand();
        notificationCommand.CommandText = hasNotificationIntentColumn
            ? """
              SELECT title, body, deduplication_key, notification_intent_id
              FROM notifications
              WHERE id = @notification_id
              """
            : """
              SELECT title, body, deduplication_key, NULL::uuid AS notification_intent_id
              FROM notifications
              WHERE id = @notification_id
              """;
        notificationCommand.Parameters.AddWithValue("notification_id", NotificationCanaryId);
        await using NpgsqlDataReader reader = await notificationCommand.ExecuteReaderAsync();
        await Assert.That(await reader.ReadAsync()).IsTrue();
        await Assert.That(reader.GetString(0)).IsEqualTo("Preserved notification");
        await Assert.That(reader.GetString(1)).IsEqualTo("This notification must survive the ledger reset.");
        await Assert.That(reader.GetString(2)).IsEqualTo("migration-notification-canary");
        if (hasNotificationIntentColumn)
        {
            await Assert.That(reader.IsDBNull(3)).IsTrue();
        }
    }

    private static async Task AssertRecipientDeliveryConstraintsAsync(NpgsqlConnection connection)
    {
        const string createdAt = "2026-07-17 12:00:00+00";

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000030', '{TenantId}', 4, 1, 1, 3,
                  'constraint.required-recipient', 'constraint-required-recipient', NULL, '{createdAt}', false);
             """,
            PostgresErrorCodes.NotNullViolation,
            columnName: "recipient_user_id");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000031', '{OtherTenantId}', 4, 1, 1, 3,
                  'constraint.tenant-member', 'constraint-tenant-member', '{RecipientUserId}', '{createdAt}', false);
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_notification_intents_tenant_users_tenant_id_recipient_user_");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000032', '{TenantId}', 4, 1, 1, 3,
                  'constraint.recipient-match', 'constraint-recipient-match', '{RecipientUserId}', '{createdAt}', false);
             INSERT INTO email_dispatch_outbox
                 (id, tenant_id, publish_event_id, kind, source_type, source_id,
                  notification_intent_id, recipient_user_id, recipient_address_source,
                  recipient_email, subject, status, attempt_count, max_attempts,
                  rabbit_mq_publish_attempt_count, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000033', '{TenantId}',
                  '019b3333-2000-7000-8000-000000000034', 1, 'constraint-probe',
                  '019b3333-2000-7000-8000-000000000035',
                  '019b3333-2000-7000-8000-000000000032', '{OtherUserId}', 1,
                  'other@example.test', 'Recipient mismatch', 1, 0, 5, 0, '{createdAt}', false);
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_email_dispatch_outbox_recipient_matches_intent");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO email_dispatch_outbox
                 (id, tenant_id, publish_event_id, kind, source_type, source_id,
                  notification_intent_id, recipient_user_id, recipient_address_source,
                  recipient_email, subject, status, attempt_count, max_attempts,
                  rabbit_mq_publish_attempt_count, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000036', '{TenantId}',
                  '019b3333-2000-7000-8000-000000000037', 1, 'constraint-probe',
                  '019b3333-2000-7000-8000-000000000038', '{CurrentIntentId}', '{RecipientUserId}', 1,
                  'recipient@example.test', 'Duplicate intent email', 1, 0, 5, 0, '{createdAt}', false);
             """,
            PostgresErrorCodes.UniqueViolation,
            "ux_email_dispatch_outbox_tenant_intent");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000039', '{TenantId}', 4, 1, 1, 3,
                  'constraint.managed-authority', 'constraint-managed-authority', '{RecipientUserId}', '{createdAt}', false);
             INSERT INTO email_dispatch_outbox
                 (id, tenant_id, publish_event_id, kind, source_type, source_id,
                  notification_intent_id, recipient_user_id, recipient_address_source,
                  recipient_email, subject, status, attempt_count, max_attempts,
                  rabbit_mq_publish_attempt_count, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-00000000003a', '{TenantId}',
                  '019b3333-2000-7000-8000-00000000003b', 1, 'constraint-probe',
                  '019b3333-2000-7000-8000-00000000003c',
                  '019b3333-2000-7000-8000-000000000039', '{RecipientUserId}', 2,
                  'recipient@example.test', 'Invalid managed authority', 1, 0, 5, 0, '{createdAt}', false);
             """,
            PostgresErrorCodes.CheckViolation,
            "ck_email_dispatch_outbox_recipient_authority");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-00000000004f', '{TenantId}', 4, 1, 1, 3,
                  'constraint.unknown-source', 'constraint-unknown-source', '{RecipientUserId}', '{createdAt}', false);
             INSERT INTO email_dispatch_outbox
                 (id, tenant_id, publish_event_id, kind, source_type, source_id,
                  notification_intent_id, recipient_user_id, recipient_address_source,
                  recipient_email, subject, status, attempt_count, max_attempts,
                  rabbit_mq_publish_attempt_count, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000050', '{TenantId}',
                  '019b3333-2000-7000-8000-000000000051', 1, 'constraint-probe',
                  '019b3333-2000-7000-8000-000000000052',
                  '019b3333-2000-7000-8000-00000000004f', '{RecipientUserId}', 3,
                  'recipient@example.test', 'Unknown source', 1, 0, 5, 0, '{createdAt}', false);
             """,
            PostgresErrorCodes.CheckViolation,
            "ck_email_dispatch_outbox_recipient_authority");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO managed_tenant_provisioning_operations
                 (id, completed_at, created_at, current_outbox_message_id, external_customer_reference,
                  external_request_id, managed_instance_id, request_hash, status,
                  tenant_administrator_user_id, tenant_id, tenant_slug)
             VALUES
                 ('019b3333-2000-7000-8000-00000000003d', '{createdAt}', '{createdAt}',
                  '019b3333-2000-7000-8000-00000000003e', 'migration-customer', 'migration-request',
                  '019b3333-2000-7000-8000-00000000003f',
                  'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
                  'Succeeded', '{OtherUserId}', '{OtherTenantId}', 'migration-managed-tenant');
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000040', '{TenantId}', 4, 1, 1, 3,
                  'constraint.managed-tenant', 'constraint-managed-tenant', '{RecipientUserId}', '{createdAt}', false);
             INSERT INTO email_dispatch_outbox
                 (id, tenant_id, publish_event_id, kind, source_type, source_id,
                  notification_intent_id, recipient_user_id, recipient_address_source,
                  managed_tenant_provisioning_operation_id, recipient_email, subject, status,
                  attempt_count, max_attempts, rabbit_mq_publish_attempt_count, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000041', '{TenantId}',
                  '019b3333-2000-7000-8000-000000000042', 8, 'managed_tenant_provisioning',
                  '019b3333-2000-7000-8000-00000000003d',
                  '019b3333-2000-7000-8000-000000000040', '{RecipientUserId}', 2,
                  '019b3333-2000-7000-8000-00000000003d', 'recipient@example.test',
                  'Managed tenant mismatch', 1, 0, 5, 0, '{createdAt}', false);
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_email_dispatch_outbox_managed_operation_tenant");

        await AssertSucceedsAndRollbackAsync(
            connection,
            $"""
             INSERT INTO managed_tenant_provisioning_operations
                 (id, completed_at, created_at, current_outbox_message_id, external_customer_reference,
                  external_request_id, managed_instance_id, request_hash, status,
                  tenant_administrator_user_id, tenant_id, tenant_slug)
             VALUES
                 ('019b3333-2000-7000-8000-000000000053', '{createdAt}', '{createdAt}',
                  '019b3333-2000-7000-8000-000000000054', 'same-tenant-customer', 'same-tenant-request',
                  '019b3333-2000-7000-8000-000000000055',
                  'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
                  'Succeeded', '{RecipientUserId}', '{TenantId}', 'same-tenant-managed');
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000056', '{TenantId}', 4, 1, 1, 3,
                  'constraint.managed-success', 'constraint-managed-success', '{RecipientUserId}', '{createdAt}', false);
             INSERT INTO email_dispatch_outbox
                 (id, tenant_id, publish_event_id, kind, source_type, source_id,
                  notification_intent_id, recipient_user_id, recipient_address_source,
                  managed_tenant_provisioning_operation_id, recipient_email, subject, status,
                  attempt_count, max_attempts, rabbit_mq_publish_attempt_count, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000057', '{TenantId}',
                  '019b3333-2000-7000-8000-000000000058', 8, 'managed_tenant_provisioning',
                  '019b3333-2000-7000-8000-000000000053',
                  '019b3333-2000-7000-8000-000000000056', '{RecipientUserId}', 2,
                  '019b3333-2000-7000-8000-000000000053', 'recipient@example.test',
                  'Managed tenant success', 1, 0, 5, 0, '{createdAt}', false);
             """);

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO email_dispatch_attempts
                 (id, tenant_id, email_dispatch_outbox_id, attempt_number, transport, outcome, started_at, created_at)
             VALUES
                 ('019b3333-2000-7000-8000-000000000043', '{OtherTenantId}', '{CurrentEmailId}',
                  1, 'smtp', 1, '{createdAt}', '{createdAt}');
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_email_dispatch_attempts_email_dispatch_outbox_tenant_id_ema");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO email_dispatch_receipts
                 (id, tenant_id, publish_event_id, email_dispatch_outbox_id, status, first_seen_at, created_at)
             VALUES
                 ('019b3333-2000-7000-8000-000000000044', '{OtherTenantId}',
                  '019b3333-2000-7000-8000-000000000023', '{CurrentEmailId}', 1, '{createdAt}', '{createdAt}');
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_email_dispatch_receipts_email_dispatch_outbox_tenant_id_ema");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO email_dispatch_receipts
                 (id, tenant_id, publish_event_id, email_dispatch_outbox_id, status, first_seen_at, created_at)
             VALUES
                 ('019b3333-2000-7000-8000-000000000045', '{TenantId}',
                  '019b3333-2000-7000-8000-000000000046', '{CurrentEmailId}', 1, '{createdAt}', '{createdAt}');
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_email_dispatch_receipts_email_dispatch_outbox_tenant_id_ema");

        await AssertViolationAsync(
            connection,
            $"""
             UPDATE notification_deliveries
             SET recipient_address_source = 2
             WHERE id = '019b3333-2000-7000-8000-000000000024';
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_notification_deliveries_email_dispatch_outbox_tenant_id_ema");

        await AssertViolationAsync(
            connection,
            $"""
             UPDATE notification_deliveries
             SET recipient_address_source = NULL
             WHERE id = '019b3333-2000-7000-8000-000000000024';
             """,
            PostgresErrorCodes.CheckViolation,
            "ck_notification_deliveries_channel_link");

        await AssertViolationAsync(
            connection,
            $"""
             UPDATE notification_deliveries
             SET channel_id = 2
             WHERE id = '019b3333-2000-7000-8000-000000000024';
             """,
            PostgresErrorCodes.CheckViolation,
            "ck_notification_deliveries_channel_link");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000059', '{TenantId}', 4, 1, 1, 3,
                  'constraint.in-app-source', 'constraint-in-app-source', '{RecipientUserId}', '{createdAt}', false);
             INSERT INTO notification_deliveries
                 (id, tenant_id, notification_intent_id, channel_id, delivery_policy_id,
                  is_required, policy_version, disclosure_level, template_key, template_version,
                  link_allowed, recipient_address_source, status_id, queued_at, created_at)
             VALUES
                 ('019b3333-2000-7000-8000-00000000005a', '{TenantId}',
                  '019b3333-2000-7000-8000-000000000059', 2, 1, true, 1, 'generic',
                  'constraint.in-app-source', 1, false, 1, 2, '{createdAt}', '{createdAt}');
             """,
            PostgresErrorCodes.CheckViolation,
            "ck_notification_deliveries_channel_link");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-000000000049', '{TenantId}', 4, 1, 1, 3,
                  'constraint.notification-intent', 'constraint-notification-intent', '{RecipientUserId}', '{createdAt}', false);
             INSERT INTO notifications
                 (id, tenant_id, user_id, notification_type_id, title, body, deduplication_key,
                  notification_scope_id, notification_intent_id, is_read, is_archived, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-00000000004a', '{TenantId}', '{RecipientUserId}', 1,
                  'Alternate notification', 'Alternate notification body', 'alternate-linked-notification',
                  1, '019b3333-2000-7000-8000-000000000049', false, false, '{createdAt}', false);
             INSERT INTO notification_deliveries
                 (id, tenant_id, notification_intent_id, channel_id, delivery_policy_id,
                  is_required, policy_version, disclosure_level, template_key, template_version,
                  link_allowed, notification_id, status_id, queued_at, created_at)
             VALUES
                 ('019b3333-2000-7000-8000-00000000004b', '{TenantId}', '{CurrentIntentId}', 2, 1,
                  true, 1, 'generic', 'constraint.notification-intent', 1, false,
                  '019b3333-2000-7000-8000-00000000004a', 2, '{createdAt}', '{createdAt}');
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_notification_deliveries_notification_same_intent");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_intents
                 (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                  template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-00000000004c', '{TenantId}', 4, 1, 1, 3,
                  'constraint.notification-user', 'constraint-notification-user', '{RecipientUserId}', '{createdAt}', false);
             INSERT INTO notifications
                 (id, tenant_id, user_id, notification_type_id, title, body, deduplication_key,
                  notification_scope_id, notification_intent_id, is_read, is_archived, created_at, is_deleted)
             VALUES
                 ('019b3333-2000-7000-8000-00000000004d', '{TenantId}', '{OtherUserId}', 1,
                  'Wrong user notification', 'Wrong user notification body', 'wrong-user-notification',
                  1, '019b3333-2000-7000-8000-00000000004c', false, false, '{createdAt}', false);
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_notifications_recipient_matches_intent");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_external_delegations
                 (id, tenant_id, notification_intent_id, provider_kind_id, status_id,
                  recipient_kind_id, template_key, created_at)
             VALUES
                 ('019b3333-2000-7000-8000-00000000004e', '{OtherTenantId}', '{CurrentIntentId}',
                  1, 1, 1, 'constraint.delegation-tenant', '{createdAt}');
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_notification_external_delegations_tenant_intent");
    }

    private static async Task AssertViolationAsync(
        NpgsqlConnection connection,
        string sql,
        string sqlState,
        string? constraintName = null,
        string? columnName = null)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        PostgresException violation = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());
        await Assert.That(violation.SqlState).IsEqualTo(sqlState);
        if (constraintName is not null)
        {
            await Assert.That(violation.ConstraintName).IsEqualTo(constraintName);
        }

        if (columnName is not null)
        {
            await Assert.That(violation.ColumnName).IsEqualTo(columnName);
        }

        await transaction.RollbackAsync();
    }

    private static async Task AssertSucceedsAndRollbackAsync(NpgsqlConnection connection, string sql)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await Assert.That(await command.ExecuteNonQueryAsync()).IsGreaterThan(0);
        await transaction.RollbackAsync();
    }

    private static async Task InsertCurrentRecipientGraphAsync(NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO notification_intents
                (id, tenant_id, category_id, ownership_type_id, recipient_kind_id, status_id,
                 template_key, deduplication_key, recipient_user_id, created_at, is_deleted)
            VALUES
                (@intent_id, @tenant_id, 4, 1, 1, 3,
                 'registration.confirmation', 'current-recipient-graph', @recipient_user_id, @created_at, false);
            INSERT INTO notifications
                (id, tenant_id, user_id, notification_type_id, title, body, deduplication_key,
                 notification_scope_id, notification_intent_id, is_read, is_archived, created_at, is_deleted)
            VALUES
                (@notification_id, @tenant_id, @recipient_user_id, 1,
                 'Current linked notification', 'Current linked notification body',
                 'current-linked-notification', 1, @intent_id, false, false, @created_at, false);
            INSERT INTO email_dispatch_outbox
                (id, tenant_id, publish_event_id, kind, source_type, source_id,
                 notification_intent_id, recipient_user_id, recipient_address_source,
                 recipient_email, subject, status, attempt_count, max_attempts,
                 rabbit_mq_publish_attempt_count, created_at, is_deleted)
            VALUES
                (@email_id, @tenant_id, @publish_event_id, 1, 'notification_intent', @intent_id,
                 @intent_id, @recipient_user_id, 1,
                 'recipient@example.test', 'Current recipient graph', 1, 0, 5, 0, @created_at, false);
            INSERT INTO notification_deliveries
                (id, tenant_id, notification_intent_id, channel_id, delivery_policy_id,
                 is_required, policy_version, disclosure_level, template_key, template_version,
                 link_allowed, email_dispatch_outbox_id, recipient_address_source,
                 status_id, queued_at, created_at)
            VALUES
                (@delivery_id, @tenant_id, @intent_id, 1, 1,
                 false, 1, 'generic', 'registration.confirmation', 1,
                 false, @email_id, 1, 2, @created_at, @created_at);
            """;
        command.Parameters.AddWithValue("intent_id", CurrentIntentId);
        command.Parameters.AddWithValue("tenant_id", TenantId);
        command.Parameters.AddWithValue("recipient_user_id", RecipientUserId);
        command.Parameters.AddWithValue("email_id", CurrentEmailId);
        command.Parameters.AddWithValue("notification_id", CurrentNotificationId);
        command.Parameters.AddWithValue("publish_event_id", Guid.Parse("019b3333-2000-7000-8000-000000000023"));
        command.Parameters.AddWithValue("delivery_id", Guid.Parse("019b3333-2000-7000-8000-000000000024"));
        command.Parameters.AddWithValue("created_at", new DateTime(2026, 7, 17, 11, 0, 0, DateTimeKind.Utc));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> ReadCountAsync(NpgsqlConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM {table}";
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Count for '{table}' was null."));
    }

    private static async Task<long> ReadCountAsync(
        NpgsqlConnection connection,
        string table,
        string predicate)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM {table} WHERE {predicate}";
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Filtered count for '{table}' was null."));
    }

    private static async Task<bool> TableExistsAsync(NpgsqlConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.' || @table) IS NOT NULL";
        command.Parameters.AddWithValue("table", table);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Table existence result for '{table}' was null."));
    }

    private static async Task<bool> ConstraintExistsAsync(
        NpgsqlConnection connection,
        string constraint)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS
            (
                SELECT 1
                FROM pg_constraint
                WHERE conname = @constraint
            )
            """;
        command.Parameters.AddWithValue("constraint", constraint);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Constraint existence result for '{constraint}' was null."));
    }

    private static async Task<bool> ColumnExistsAsync(
        NpgsqlConnection connection,
        string table,
        string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS
            (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = @table
                  AND column_name = @column
            )
            """;
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Column existence result for '{table}.{column}' was null."));
    }

    private static async Task<string> ReadScalarStringAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (string)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Expected scalar string was null."));
    }

    private static async Task<int> ReadScalarIntAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (int)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Expected scalar integer was null."));
    }

    private static async Task<bool> ReadScalarBoolAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Expected scalar boolean was null."));
    }

    private static async Task<bool> IsColumnRequiredAsync(
        NpgsqlConnection connection,
        string table,
        string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT is_nullable = 'NO'
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = @table
              AND column_name = @column
            """;
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Column '{table}.{column}' was not found."));
    }
}
