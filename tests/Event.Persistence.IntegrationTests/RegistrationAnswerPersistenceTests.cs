// ABOUTME: Defines real PostgreSQL acceptance for typed atomic registration-answer storage.
// ABOUTME: Requires relational value, subject, lineage, durable identity, sensitive-shape, and tenant constraints.

using System.Security.Cryptography;
using System.Text;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using Explore.Persistence;
using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests;

public sealed class RegistrationAnswerPersistenceContractTests
{
    [Test]
    public async Task ModelDeclaresTypedAnswerIdentitySensitiveShapeAndNamedFilters()
    {
        await using ExploreDbContext context = new(
            new DbContextOptionsBuilder<ExploreDbContext>()
                .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
                .UseSnakeCaseNamingConvention()
                .Options);

        IEntityType answer = context.Model.FindEntityType(typeof(RegistrationAnswer))!;
        IEntityType sensitive = context.Model.FindEntityType(typeof(RegistrationSensitiveAnswerValue))!;
        IIndex identity = answer.GetIndexes().Single(index => index.GetDatabaseName() == "ux_registration_answers_durable_identity");

        await Assert.That(answer.GetDeclaredQueryFilters().Count()).IsEqualTo(2);
        await Assert.That(sensitive.GetDeclaredQueryFilters().Count()).IsEqualTo(2);
        await Assert.That(identity.IsUnique).IsTrue();
        await Assert.That(identity.GetFilter()).IsNull();
        await Assert.That(sensitive.FindProperty("Plaintext")).IsNull();
        await Assert.That(sensitive.FindProperty(nameof(RegistrationSensitiveAnswerValue.Ciphertext))).IsNotNull();
        await Assert.That(sensitive.FindProperty(nameof(RegistrationSensitiveAnswerValue.KeyVersion))).IsNotNull();
    }
}

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class RegistrationAnswerPostgreSqlPersistenceTests(PostgreSqlContainerFixture fixture)
{
    private static readonly DateTime UtcNow = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
    private const string AnswerTable = RelationalModelNamespace.Name + ".registration_answers";
    private const string AnswerSubjectTypeTable = RelationalModelNamespace.Name + ".registration_answer_subject_types";

    [Test]
    [Category("Runtime")]
    public async Task PostgreSqlCatalogContainsNamedTypedValueSubjectAndIdentityConstraints()
    {
        await fixture.ResetAsync();
        await using ExploreDbContext context = fixture.CreateDbContext();
        string[] expected =
        [
            "ck_registration_answers_exactly_one_value",
            "ck_registration_answers_value_matches_field_type",
            "ck_registration_answers_subject_shape",
            "ck_registration_answers_positive_ordinal",
            "ux_registration_answers_durable_identity"
        ];

        string[] actual = await context.Database.SqlQueryRaw<string>(
            $"SELECT conname AS value FROM pg_constraint WHERE conrelid = '{AnswerTable}'::regclass " +
            $"UNION ALL SELECT indexname AS value FROM pg_indexes WHERE schemaname = '{RelationalModelNamespace.Name}' AND tablename = 'registration_answers'")
            .ToArrayAsync();
        string[] subjectTypes = await context.Database.SqlQueryRaw<string>(
            $"SELECT id || ':' || master_code AS value FROM {AnswerSubjectTypeTable} ORDER BY id")
            .ToArrayAsync();

        await Assert.That(expected.Except(actual)).IsEmpty();
        await Assert.That(subjectTypes).IsEquivalentTo(
        [
            "1:REGISTRATION_ORDER",
            "2:PURCHASER",
            "3:PARTICIPANT",
            "4:TICKET_ASSIGNMENT",
            "5:SESSION_SELECTION"
        ]);
    }

    [Test]
    [Category("Runtime")]
    public async Task TextAnswerRoundTripsAndTenantFilterHidesIt()
    {
        await fixture.ResetAsync();
        AnswerScope scope = await SeedAnswerAsync();

        await using ExploreDbContext tenant = fixture.CreateTenantFilteredDbContext(new TestTenantContext(scope.TenantId));
        RegistrationAnswer persisted = await tenant.RegistrationAnswers.AsNoTracking().SingleAsync();
        await Assert.That(persisted.TextValue).IsEqualTo("Ada");

        await using ExploreDbContext otherTenant = fixture.CreateTenantFilteredDbContext(new TestTenantContext(Guid.CreateVersion7()));
        await Assert.That(await otherTenant.RegistrationAnswers.CountAsync()).IsEqualTo(0);
    }

    [Test]
    [Category("Runtime")]
    public async Task DatabaseRejectsTwoValuesWrongTypeSubjectShapeAndDuplicateIdentity()
    {
        await fixture.ResetAsync();
        AnswerScope scope = await SeedAnswerAsync();

        await AssertRejectedUpdateAsync(scope.AnswerId,
            "integer_value = 1", "ck_registration_answers_exactly_one_value");
        await AssertRejectedUpdateAsync(scope.AnswerId,
            "text_value = NULL, integer_value = 1", "ck_registration_answers_value_matches_field_type");
        await AssertRejectedUpdateAsync(scope.AnswerId,
            $"order_subject_id = NULL, participant_subject_id = '{Guid.CreateVersion7()}', answer_subject_type_id = 3",
            "ck_registration_answers_subject_shape");

        await using ExploreDbContext context = fixture.CreateDbContext();
        DbUpdateException duplicate = await AssertDatabaseFailureAsync(() => context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO {AnswerTable} (id, tenant_id, event_id, registration_order_id, registration_attempt_id, registration_submission_id, registration_workflow_id, registration_requirement_id, registration_form_id, registration_form_version_id, registration_form_section_id, registration_form_field_id, field_type_id, requirement_subject_type_id, requirement_subject_id, answer_subject_type_id, order_subject_id, purchaser_subject_id, participant_subject_id, ticket_assignment_subject_id, session_selection_subject_id, ordinal, text_value, integer_value, decimal_value, boolean_value, date_value, time_value, instant_value, selected_option_id, sensitive_answer_value_id, created_at, created_by, updated_at, updated_by, is_deleted, deleted_at, deleted_by) " +
            $"SELECT gen_random_uuid(), tenant_id, event_id, registration_order_id, registration_attempt_id, registration_submission_id, registration_workflow_id, registration_requirement_id, registration_form_id, registration_form_version_id, registration_form_section_id, registration_form_field_id, field_type_id, requirement_subject_type_id, requirement_subject_id, answer_subject_type_id, order_subject_id, purchaser_subject_id, participant_subject_id, ticket_assignment_subject_id, session_selection_subject_id, ordinal, text_value, integer_value, decimal_value, boolean_value, date_value, time_value, instant_value, selected_option_id, sensitive_answer_value_id, created_at, created_by, updated_at, updated_by, is_deleted, deleted_at, deleted_by FROM {AnswerTable} WHERE id = {{0}}",
            scope.AnswerId));
        PostgresException duplicatePostgres = FindPostgresException(duplicate);
        await Assert.That(duplicatePostgres.ConstraintName ?? $"{duplicatePostgres.SqlState}: {duplicatePostgres.MessageText}")
            .IsEqualTo("ux_registration_answers_durable_identity");
    }

    [Test]
    [Category("Runtime")]
    public async Task DatabaseRejectsTicketAssignmentFromWrongTargetedTicketType()
    {
        await fixture.ResetAsync();

        await Assert.That(() => SeedAnswerAsync(useWrongTicketTypeAssignment: true))
            .Throws<DbUpdateException>();
    }

    [Test]
    [Category("Runtime")]
    public async Task SensitiveAnswerForeignKeyRejectsCrossTenantCiphertext()
    {
        await fixture.ResetAsync();
        AnswerScope scope = await SeedAnswerAsync();
        Guid otherTenantId;
        Guid sensitiveId;
        await using (ExploreDbContext setup = fixture.CreateDbContext())
        {
            Tenant otherTenant = new() { FullName = "Other", Slug = $"other-{Guid.NewGuid():N}", TenantStatusId = 2, TenantStatus = null! };
            setup.Tenants.Add(otherTenant);
            await setup.SaveChangesAsync();
            RegistrationSensitiveAnswerValue sensitive = RegistrationSensitiveAnswerValue.Create(
                otherTenant.Id, Convert.ToBase64String(new byte[29]), 1, UtcNow);
            setup.RegistrationSensitiveAnswerValues.Add(sensitive);
            await setup.SaveChangesAsync();
            otherTenantId = otherTenant.Id;
            sensitiveId = sensitive.Id;
        }

        await using ExploreDbContext context = fixture.CreateDbContext();
        DbUpdateException rejected = await AssertDatabaseFailureAsync(() => context.Database.ExecuteSqlRawAsync(
            $"UPDATE {AnswerTable} SET text_value = NULL, sensitive_answer_value_id = {{0}} WHERE id = {{1}}",
            sensitiveId, scope.AnswerId));
        PostgresException postgres = FindPostgresException(rejected);
        await Assert.That(postgres.SqlState).IsEqualTo(PostgresErrorCodes.ForeignKeyViolation);
        await Assert.That(otherTenantId).IsNotEqualTo(scope.TenantId);
    }

    [Test]
    [Category("Runtime")]
    public async Task SensitiveAnswerPersistsOnlyOpaqueCiphertextAndKeyMetadata()
    {
        await fixture.ResetAsync();
        AnswerScope scope = await SeedAnswerAsync();
        string ciphertext = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        await using (ExploreDbContext setup = fixture.CreateDbContext())
        {
            RegistrationSensitiveAnswerValue sensitive = RegistrationSensitiveAnswerValue.Create(
                scope.TenantId, ciphertext, 7, UtcNow);
            setup.RegistrationSensitiveAnswerValues.Add(sensitive);
            await setup.SaveChangesAsync();
            await setup.Database.ExecuteSqlRawAsync(
                $"UPDATE {AnswerTable} SET text_value = NULL, sensitive_answer_value_id = {{0}} WHERE id = {{1}}",
                sensitive.Id, scope.AnswerId);
        }

        await using ExploreDbContext context = fixture.CreateDbContext();
        RegistrationAnswer answer = await context.RegistrationAnswers
            .Include(candidate => candidate.SensitiveAnswerValue)
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == scope.AnswerId);

        await Assert.That(answer.TextValue).IsNull();
        await Assert.That(answer.IntegerValue).IsNull();
        await Assert.That(answer.DecimalValue).IsNull();
        await Assert.That(answer.BooleanValue).IsNull();
        await Assert.That(answer.DateValue).IsNull();
        await Assert.That(answer.TimeValue).IsNull();
        await Assert.That(answer.InstantValue).IsNull();
        await Assert.That(answer.SelectedOptionId).IsNull();
        await Assert.That(answer.SensitiveAnswerValue!.Ciphertext).IsEqualTo(ciphertext);
        await Assert.That(answer.SensitiveAnswerValue.KeyVersion).IsEqualTo(7);
    }

    private async Task<AnswerScope> SeedAnswerAsync(bool useWrongTicketTypeAssignment = false)
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        Tenant tenant = new() { FullName = "Answers", Slug = $"answers-{Guid.NewGuid():N}", TenantStatusId = 2, TenantStatus = null! };
        User user = new() { Pii = new UserPii { Email = $"{Guid.NewGuid():N}@example.com", FirstName = "Answer", LastName = "Owner" } };
        context.AddRange(tenant, user);
        await context.SaveChangesAsync();
        Actor actor = new() { Pii = new ActorPii { DisplayName = "Answers" }, ActorTypeId = 1, ActorType = null!, UserId = user.Id };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();
        Explore.Domain.Event @event = new()
        {
            Id = Guid.CreateVersion7(),
            Title = "Answers",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            EventStatusId = 1,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            EventProvenanceTypeId = 1,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenant.Id, @event.Id, "EUR", 1);
        EventTicketType? targetedTicketType = null;
        EventTicketType? wrongTicketType = null;
        if (useWrongTicketTypeAssignment)
        {
            targetedTicketType = FreeTicketType(tenant.Id, catalog.Id, "Targeted");
            wrongTicketType = FreeTicketType(tenant.Id, catalog.Id, "Wrong");
            catalog.AddTicketType(targetedTicketType, null);
            catalog.AddTicketType(wrongTicketType, null);
            catalog.AddEntitlement(targetedTicketType,
                TicketTypeEntitlement.CreateForEvent(targetedTicketType.Id, tenant.Id, @event.Id, 1));
            catalog.AddEntitlement(wrongTicketType,
                TicketTypeEntitlement.CreateForEvent(wrongTicketType.Id, tenant.Id, @event.Id, 1));
            catalog.Publish();
        }
        RegistrationWorkflow workflow = RegistrationWorkflow.Create(tenant.Id, @event.Id, "ANSWERS", UtcNow);
        RegistrationRequirement requirement = RegistrationRequirement.Create(
            workflow, 1, RegistrationRequirementCriticalityEnum.Required, false,
            RegistrationRequirementCompletionEffectEnum.BlocksRegistration,
            RegistrationAnswerSyncModeEnum.FULL_CANONICAL,
            useWrongTicketTypeAssignment
                ? RegistrationRequirementSubjectTypeEnum.SpecificTicketType
                : RegistrationRequirementSubjectTypeEnum.AllOrders,
            targetedTicketType?.Id, UtcNow);
        RegistrationChannel channel = RegistrationChannel.Create(requirement, 1, true, null, UtcNow);
        requirement.AddChannel(channel);
        workflow.AddRequirement(requirement);
        RegistrationForm form = RegistrationForm.Create(tenant.Id, @event.Id, "platform.registration", "answers", "Answers", UtcNow);
        RegistrationFormVersion version = RegistrationFormVersion.Create(form, 1, "en", null, null, UtcNow);
        RegistrationFormSection section = RegistrationFormSection.Create(Guid.CreateVersion7(), version, 1, "Details", UtcNow);
        RegistrationFormField field = RegistrationFormField.Create(
            Guid.CreateVersion7(), section, 1, "platform.registration", "name", "Name",
            RegistrationFieldTypeEnum.ShortText, 1, RegistrationOrganizerVisibilityEnum.AuthorizedOrganizers,
            false, true, UtcNow);
        version.AddSection(section);
        version.AddField(section, field);
        form.AddVersion(version);
        context.AddRange(catalog, workflow, form);
        await context.SaveChangesAsync();

        RegistrationOrder order = RegistrationOrder.Create(
            tenant.Id, @event.Id, user.Id, null, BookingPartyTypeEnum.Individual, catalog.Id,
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            workflow.Id, null, "EUR", UtcNow, UtcNow.AddHours(1));
        RegistrationTicketAssignment? wrongAssignment = null;
        Guid? wrongAssignmentOrderLineId = null;
        if (useWrongTicketTypeAssignment)
        {
            RegistrationOrderLine targetedLine = RegistrationOrderLine.Create(
                catalog, targetedTicketType!, order.Id, 1, null, null);
            RegistrationOrderLine wrongLine = RegistrationOrderLine.Create(
                catalog, wrongTicketType!, order.Id, 1, null, null);
            order.AddLine(targetedLine);
            order.AddLine(wrongLine);
            wrongAssignment = RegistrationTicketAssignment.Create(
                tenant.Id, order.Id, wrongLine.Id, 1, null, AssignmentStatusEnum.Unassigned, null, UtcNow);
            wrongAssignmentOrderLineId = wrongLine.Id;
        }
        context.RegistrationOrders.Add(order);
        if (wrongAssignment is not null)
        {
            context.RegistrationTicketAssignments.Add(wrongAssignment);
        }
        await context.SaveChangesAsync();
        RegistrationAttempt attempt = RegistrationAttempt.Create(
            tenant.Id, @event.Id, order.Id, workflow.Id, requirement.Id, channel.Id, form.Id, version.Id,
            CapabilityTokenHash.Create(Hash("capability")), null, null, UtcNow, UtcNow.AddMinutes(10));
        RegistrationSubmission submission = RegistrationSubmission.Create(
            attempt, RegistrationEvidenceHash.Create(Hash("evidence")), UtcNow.AddMinutes(1), null, null, null, null);
        context.AddRange(attempt, submission);
        await context.SaveChangesAsync();
        RegistrationAnswer answer = RegistrationAnswer.CreateText(
            submission, field, requirement,
            useWrongTicketTypeAssignment
                ? RegistrationAnswerSubjectTypeEnum.TicketAssignment
                : RegistrationAnswerSubjectTypeEnum.RegistrationOrder,
            wrongAssignment?.Id ?? order.Id, 1, "Ada", UtcNow.AddMinutes(2), wrongAssignmentOrderLineId);
        context.RegistrationAnswers.Add(answer);
        await context.SaveChangesAsync();
        return new(tenant.Id, answer.Id);
    }

    private async Task AssertRejectedUpdateAsync(Guid answerId, string assignment, string constraint)
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        DbUpdateException rejected = await AssertDatabaseFailureAsync(() => context.Database.ExecuteSqlRawAsync(
            $"UPDATE {AnswerTable} SET {assignment} WHERE id = {{0}}", answerId));
        await Assert.That(FindPostgresException(rejected).ConstraintName).IsEqualTo(constraint);
    }

    private static async Task<DbUpdateException> AssertDatabaseFailureAsync(Func<Task<int>> action)
    {
        Exception exception = (await Assert.That(action).Throws<Exception>())!;
        return exception as DbUpdateException ?? new DbUpdateException("PostgreSQL rejected the row.", exception);
    }

    private static PostgresException FindPostgresException(Exception exception) =>
        exception is PostgresException postgres
            ? postgres
            : exception.InnerException is not null
                ? FindPostgresException(exception.InnerException)
                : throw new InvalidOperationException("Expected PostgreSQL failure.", exception);

    private static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static EventTicketType FreeTicketType(Guid tenantId, Guid catalogId, string name) => EventTicketType.Create(
        Guid.CreateVersion7(), tenantId, catalogId, name, "EUR", TicketPricingModeEnum.Free,
        null, null, null, ParticipantDataCollectionModeEnum.None,
        null, null, null, false, false, null, null, null, null);

    private sealed record AnswerScope(Guid TenantId, Guid AnswerId);
    private sealed record TestTenantContext(Guid TenantId) : Explore.Application.Contracts.Infrastructure.ITenantContext;
}
