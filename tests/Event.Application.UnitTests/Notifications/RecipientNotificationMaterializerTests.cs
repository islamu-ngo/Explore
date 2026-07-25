// ABOUTME: Unit tests for atomic recipient notification graph construction and exact conflict recovery.
// ABOUTME: Proves channel linkage, typed skips, fresh-UoW recovery, and unrelated-error propagation.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Notifications;

public sealed class RecipientNotificationMaterializerTests
{
    [Test]
    public async Task MaterializeInCurrentTransactionAsyncBuildsOneLinkedIntentChannelGraph()
    {
        var repository = new RecordingGraphRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var materializer = new RecipientNotificationMaterializer(repository, unitOfWork);
        RecipientNotificationMaterialization request = CreateRequest(includeEmail: true);

        RecipientNotificationMaterializationResult result =
            await materializer.MaterializeInCurrentTransactionAsync(request);

        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(0);
        await Assert.That(repository.Created).IsSameReferenceAs(result.Intent);
        await Assert.That(result.Deliveries.Count).IsEqualTo(2);
        NotificationDelivery inApp = result.Deliveries.Single(row => row.ChannelId == (int)NotificationPreferenceChannelEnum.InApp);
        NotificationDelivery email = result.Deliveries.Single(row => row.ChannelId == (int)NotificationPreferenceChannelEnum.Email);
        await Assert.That(inApp.NotificationId).IsEqualTo(result.Notification!.Id);
        await Assert.That(inApp.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Delivered);
        await Assert.That(email.EmailDispatchOutboxId).IsEqualTo(result.Email!.Id);
        await Assert.That(email.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Queued);
        await Assert.That(result.Email.NotificationIntentId).IsEqualTo(result.Intent.Id);
        await Assert.That(result.Intent.RecipientUserId).IsEqualTo(request.Intent.UserId!.Value);
    }

    [Test]
    public async Task MaterializeInCurrentTransactionAsyncSkipsFencedRecipientWithoutCreatingGraph()
    {
        RecipientNotificationMaterialization request = CreateRequest(includeEmail: true);
        IPrivacyErasureStateRepository privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        privacyErasureStateRepository
            .GetBySubjectAsync(request.Intent.UserId!.Value, Arg.Any<CancellationToken>())
            .Returns(CreateFencedSaga(request.Intent.UserId.Value));
        var repository = new RecordingGraphRepository();
        var materializer = new RecipientNotificationMaterializer(
            repository,
            new RecordingUnitOfWork(),
            privacyErasureStateRepository);

        RecipientNotificationMaterializationResult result =
            await materializer.MaterializeInCurrentTransactionAsync(request);

        await Assert.That(result.IsSkipped).IsTrue();
        await Assert.That(result.Intent).IsNull();
        await Assert.That(result.Deliveries).IsEmpty();
        await Assert.That(result.Notification).IsNull();
        await Assert.That(result.Email).IsNull();
        await Assert.That(repository.Created).IsNull();
    }

    [Test]
    public async Task MaterializeInCurrentTransactionAsyncSkipsWhenFenceAppearsBeforeGraphPersist()
    {
        RecipientNotificationMaterialization request = CreateRequest(includeEmail: true);
        IPrivacyErasureStateRepository privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        privacyErasureStateRepository
            .GetBySubjectAsync(request.Intent.UserId!.Value, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreateFencedSaga(request.Intent.UserId.Value));
        var repository = new RecordingGraphRepository();
        var materializer = new RecipientNotificationMaterializer(
            repository,
            new RecordingUnitOfWork(),
            privacyErasureStateRepository);

        RecipientNotificationMaterializationResult result =
            await materializer.MaterializeInCurrentTransactionAsync(request);

        await Assert.That(result.IsSkipped).IsTrue();
        await privacyErasureStateRepository.Received(2)
            .GetBySubjectAsync(request.Intent.UserId.Value, Arg.Any<CancellationToken>());
        await Assert.That(repository.Created).IsNull();
    }

    [Test]
    public async Task MaterializeInCurrentTransactionAsyncUsesRetryStableGraphIdentityAndTime()
    {
        Guid notificationId = Guid.CreateVersion7();
        Guid inAppDeliveryId = Guid.CreateVersion7();
        Guid emailDeliveryId = Guid.CreateVersion7();
        DateTime materializedAt = new(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);
        RecipientNotificationMaterialization request = CreateRequest(includeEmail: true) with
        {
            InAppNotificationId = notificationId,
            InAppDeliveryId = inAppDeliveryId,
            EmailDeliveryId = emailDeliveryId,
            MaterializedAt = materializedAt
        };
        var repository = new RecordingGraphRepository();
        var materializer = new RecipientNotificationMaterializer(repository, new RecordingUnitOfWork());

        RecipientNotificationMaterializationResult result =
            await materializer.MaterializeInCurrentTransactionAsync(request);

        await Assert.That(result.Notification!.Id).IsEqualTo(notificationId);
        await Assert.That(result.Notification.CreatedAt).IsEqualTo(materializedAt);
        NotificationDelivery inApp = result.Deliveries.Single(row =>
            row.ChannelId == (int)NotificationPreferenceChannelEnum.InApp);
        NotificationDelivery email = result.Deliveries.Single(row =>
            row.ChannelId == (int)NotificationPreferenceChannelEnum.Email);
        await Assert.That(inApp.Id).IsEqualTo(inAppDeliveryId);
        await Assert.That(inApp.CreatedAt).IsEqualTo(materializedAt);
        await Assert.That(email.Id).IsEqualTo(emailDeliveryId);
        await Assert.That(email.CreatedAt).IsEqualTo(materializedAt);
    }

    [Test]
    public async Task MaterializeInCurrentTransactionAsyncPersistsFanoutOccurrenceAuthority()
    {
        Guid occurrenceId = Guid.CreateVersion7();
        var repository = new RecordingGraphRepository();
        var materializer = new RecipientNotificationMaterializer(repository, new RecordingUnitOfWork());
        RecipientNotificationMaterialization original = CreateRequest(includeEmail: true);
        RecipientNotificationMaterialization request = original with
        {
            Intent = original.Intent with { FanoutOccurrenceId = occurrenceId }
        };

        RecipientNotificationMaterializationResult result =
            await materializer.MaterializeInCurrentTransactionAsync(request);

        await Assert.That(result.Intent.FanoutOccurrenceId).IsEqualTo(occurrenceId);
    }

    [Test]
    public async Task MaterializeInCurrentTransactionAsyncPersistsTypedSkippedEmailChannel()
    {
        var repository = new RecordingGraphRepository();
        var materializer = new RecipientNotificationMaterializer(repository, new RecordingUnitOfWork());
        RecipientNotificationMaterialization request = CreateRequest(includeEmail: false);

        RecipientNotificationMaterializationResult result =
            await materializer.MaterializeInCurrentTransactionAsync(request);

        NotificationDelivery email = result.Deliveries.Single(row => row.ChannelId == (int)NotificationPreferenceChannelEnum.Email);
        await Assert.That(result.Email).IsNull();
        await Assert.That(email.StatusId).IsEqualTo((int)NotificationDeliveryStatusEnum.Skipped);
        await Assert.That(email.FailureCategory).IsEqualTo("recipient_email_unverified");
        await Assert.That(email.CompletedAt).IsNotNull();
    }

    [Test]
    public async Task MaterializeAsyncExactConflictLoadsAndRepairsInsideFreshUnitOfWork()
    {
        RecipientNotificationMaterialization request = CreateRequest(includeEmail: true);
        NotificationIntent winner = CreateWinningIntent(request);
        var repository = new RecordingGraphRepository
        {
            CreateFailure = new NotificationIntentDeduplicationConflictException(new InvalidOperationException("23505")),
            Loaded = winner
        };
        var unitOfWork = new RecordingUnitOfWork();
        var materializer = new RecipientNotificationMaterializer(repository, unitOfWork);

        RecipientNotificationMaterializationResult result = await materializer.MaterializeAsync(request);

        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(2);
        await Assert.That(repository.LoadCount).IsEqualTo(2);
        await Assert.That(repository.RepairCount).IsEqualTo(1);
        await Assert.That(result.Intent.Id).IsEqualTo(winner.Id);
    }

    [Test]
    public async Task MaterializeAsyncFenceBeforeConflictRecoverySkipsDeliveryRepair()
    {
        RecipientNotificationMaterialization request = CreateRequest(includeEmail: true);
        IPrivacyErasureStateRepository privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        privacyErasureStateRepository
            .GetBySubjectAsync(request.Intent.UserId!.Value, Arg.Any<CancellationToken>())
            .Returns(
                (PrivacyErasureSaga?)null,
                null,
                CreateFencedSaga(request.Intent.UserId.Value));
        var repository = new RecordingGraphRepository
        {
            CreateFailure = new NotificationIntentDeduplicationConflictException(new InvalidOperationException("23505"))
        };
        var unitOfWork = new RecordingUnitOfWork();
        var materializer = new RecipientNotificationMaterializer(
            repository,
            unitOfWork,
            privacyErasureStateRepository);

        RecipientNotificationMaterializationResult result = await materializer.MaterializeAsync(request);

        await Assert.That(result.IsSkipped).IsTrue();
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(2);
        await Assert.That(repository.LoadCount).IsEqualTo(0);
        await Assert.That(repository.RepairCount).IsEqualTo(0);
    }

    [Test]
    public async Task MaterializeAsyncUnknownCommitPrimaryKeyConflictConvergesOnStableWinner()
    {
        DateTime materializedAt = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
        RecipientNotificationMaterialization request = CreateRequest(includeEmail: true) with
        {
            InAppNotificationId = Guid.CreateVersion7(),
            InAppDeliveryId = Guid.CreateVersion7(),
            EmailDeliveryId = Guid.CreateVersion7(),
            MaterializedAt = materializedAt
        };
        NotificationIntent winner = CreateWinningIntent(request);
        winner.Id = request.IntentId;
        var repository = new RecordingGraphRepository
        {
            CreateFailure = new NotificationIntentDeduplicationConflictException(
                new InvalidOperationException("23505 pk_notification_intents")),
            Loaded = winner
        };
        var unitOfWork = new RecordingUnitOfWork();
        var materializer = new RecipientNotificationMaterializer(repository, unitOfWork);

        RecipientNotificationMaterializationResult result = await materializer.MaterializeAsync(request);

        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(2);
        await Assert.That(repository.LoadCount).IsEqualTo(2);
        await Assert.That(repository.RepairCount).IsEqualTo(1);
        await Assert.That(result.Intent.Id).IsEqualTo(request.IntentId);
    }

    [Test]
    public async Task MaterializeAsyncOccurrenceConflictRecoversByOccurrenceAndRecipient()
    {
        Guid occurrenceId = Guid.CreateVersion7();
        RecipientNotificationMaterialization original = CreateRequest(includeEmail: true);
        RecipientNotificationMaterialization request = original with
        {
            Intent = original.Intent with { FanoutOccurrenceId = occurrenceId }
        };
        NotificationIntent winner = CreateWinningIntent(request);
        winner.FanoutOccurrenceId = occurrenceId;
        var repository = new RecordingGraphRepository
        {
            CreateFailure = new NotificationIntentDeduplicationConflictException(new InvalidOperationException("23505")),
            Loaded = winner
        };
        var materializer = new RecipientNotificationMaterializer(repository, new RecordingUnitOfWork());

        RecipientNotificationMaterializationResult result = await materializer.MaterializeAsync(request);

        await Assert.That(repository.OccurrenceLoadCount).IsEqualTo(2);
        await Assert.That(repository.DeduplicationLoadCount).IsEqualTo(0);
        await Assert.That(result.Intent.Id).IsEqualTo(winner.Id);
    }

    [Test]
    public async Task MaterializeAsyncUnrelatedPersistenceErrorEscapesWithoutRecovery()
    {
        var failure = new InvalidOperationException("foreign_key_violation");
        var repository = new RecordingGraphRepository { CreateFailure = failure };
        var unitOfWork = new RecordingUnitOfWork();
        var materializer = new RecipientNotificationMaterializer(repository, unitOfWork);
        Exception? caught = null;

        try
        {
            await materializer.MaterializeAsync(CreateRequest(includeEmail: true));
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsSameReferenceAs(failure);
        await Assert.That(unitOfWork.ExecutionCount).IsEqualTo(1);
        await Assert.That(repository.LoadCount).IsEqualTo(0);
        await Assert.That(repository.RepairCount).IsEqualTo(0);
    }

    [Test]
    [Arguments(GraphFailurePoint.AfterIntent)]
    [Arguments(GraphFailurePoint.AfterEmail)]
    public async Task MaterializeAsyncPersistenceFailureRollsBackTheWholeRecipientGraph(
        GraphFailurePoint failurePoint)
    {
        var store = new TransactionalGraphStore();
        var transaction = new TransactionState();
        var unitOfWork = new TransactionalUnitOfWork(store, transaction);
        var repository = new TransactionalGraphRepository(store, transaction, failurePoint);
        var materializer = new RecipientNotificationMaterializer(repository, unitOfWork);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            materializer.MaterializeAsync(CreateRequest(includeEmail: true)));

        await Assert.That(exception.Message).IsEqualTo($"Injected graph failure: {failurePoint}");
        await Assert.That(unitOfWork.RollbackCount).IsEqualTo(1);
        await Assert.That(store.CommittedIntents).IsEmpty();
        await Assert.That(store.CommittedEmails).IsEmpty();
        await Assert.That(transaction.StagedIntent).IsNull();
        await Assert.That(transaction.StagedEmails).IsEmpty();
    }

    [Test]
    public async Task MaterializeAsyncTwoWorkersCommitExactlyOneGraphAndRecoverAfterRollback()
    {
        RecipientNotificationMaterialization requestA = CreateRequest(includeEmail: true);
        RecipientNotificationMaterialization requestB = requestA with
        {
            IntentId = Guid.CreateVersion7(),
            Email = CloneEmail(requestA.Email!)
        };
        var store = new TransactionalGraphStore(expectedConcurrentCreates: 2);
        var transactionA = new TransactionState();
        var transactionB = new TransactionState();
        var unitOfWorkA = new TransactionalUnitOfWork(store, transactionA);
        var unitOfWorkB = new TransactionalUnitOfWork(store, transactionB);
        var materializerA = new RecipientNotificationMaterializer(
            new TransactionalGraphRepository(store, transactionA),
            unitOfWorkA);
        var materializerB = new RecipientNotificationMaterializer(
            new TransactionalGraphRepository(store, transactionB),
            unitOfWorkB);

        RecipientNotificationMaterializationResult[] results = await Task.WhenAll(
            materializerA.MaterializeAsync(requestA),
            materializerB.MaterializeAsync(requestB));

        await Assert.That(store.CommittedIntents.Count).IsEqualTo(1);
        await Assert.That(store.CommittedEmails.Count).IsEqualTo(1);
        await Assert.That(results.Select(result => result.Intent.Id).Distinct().Count()).IsEqualTo(1);
        await Assert.That(unitOfWorkA.ExecutionCount + unitOfWorkB.ExecutionCount).IsEqualTo(3);
        await Assert.That(unitOfWorkA.RollbackCount + unitOfWorkB.RollbackCount).IsEqualTo(1);
        TransactionalUnitOfWork losingWorker = unitOfWorkA.RollbackCount == 1 ? unitOfWorkA : unitOfWorkB;
        await Assert.That(losingWorker.Events).IsEquivalentTo(["begin", "rollback", "begin", "commit"]);
    }

    private static RecipientNotificationMaterialization CreateRequest(bool includeEmail)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid registrationId = Guid.CreateVersion7();
        var email = includeEmail
            ? new EmailDispatchOutbox
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Kind = EmailDispatchKind.RegistrationConfirmation,
                SourceType = "event_registration_intent",
                SourceId = registrationId,
                EventId = eventId,
                RegistrationIntentId = registrationId,
                RecipientUserId = userId,
                RecipientAddressSource = RecipientAddressSource.TenantUserVerifiedEmail,
                RecipientEmail = "verified@example.test",
                Subject = "Registration created",
                PlainTextBody = "Registration created.",
                CreatedAt = DateTime.UtcNow
            }
            : null;

        return new RecipientNotificationMaterialization(
            Guid.CreateVersion7(),
            new NotificationIntentDraft(
                Explore.Application.Notifications.NotificationCategory.RegistrationLifecycle,
                tenantId,
                "User",
                "registration.confirmation",
                $"event-registration-intent:{registrationId}",
                DeduplicationKey: $"event-registration-intent:{registrationId}:registration-confirmation",
                UserId: userId,
                EventId: eventId),
            NotificationDeliveryPolicyEnum.RegistrationStatusOptional,
            "registration_status",
            new RecipientInAppNotificationDraft(
                (int)NotificationTypeEnum.RegistrationConfirmed,
                "Registration created",
                "Registration created.",
                (int)ActorTypeEnum.User),
            email,
            IncludeEmailChannel: true,
            EmailRequired: false,
            EmailSkipReason: includeEmail ? null : "recipient_email_unverified",
            PreferenceCategoryCode: NotificationPreferenceCategoryCodes.RegistrationStatus);
    }

    private static NotificationIntent CreateWinningIntent(RecipientNotificationMaterialization request)
    {
        var winner = new NotificationIntent
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.Intent.TenantId!.Value,
            CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
            OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
            RecipientKindId = (int)NotificationRecipientKindEnum.User,
            StatusId = (int)NotificationIntentStatusEnum.DispatchQueued,
            TemplateKey = request.Intent.TemplateKey!,
            DeduplicationKey = request.Intent.DeduplicationKey!,
            RecipientUserId = request.Intent.UserId!.Value
        };
        winner.Deliveries.Add(new NotificationDelivery
        {
            TenantId = winner.TenantId,
            NotificationIntentId = winner.Id,
            ChannelId = (int)NotificationPreferenceChannelEnum.InApp,
            DeliveryPolicyId = (int)NotificationDeliveryPolicyEnum.RegistrationStatusOptional,
            IsRequired = true,
            PolicyVersion = 1,
            DisclosureLevel = "registration_status",
            TemplateKey = winner.TemplateKey,
            TemplateVersion = 1,
            StatusId = (int)NotificationDeliveryStatusEnum.Delivered
        });
        return winner;
    }

    private static PrivacyErasureSaga CreateFencedSaga(Guid userId)
    {
        DateTime nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            userId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        return PrivacyErasureSaga.Start(intent, 1, new byte[32], nowUtc.AddMinutes(5), nowUtc);
    }

    private static EmailDispatchOutbox CloneEmail(EmailDispatchOutbox source) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = source.TenantId,
        Kind = source.Kind,
        SourceType = source.SourceType,
        SourceId = source.SourceId,
        EventId = source.EventId,
        RegistrationIntentId = source.RegistrationIntentId,
        RecipientUserId = source.RecipientUserId,
        RecipientAddressSource = source.RecipientAddressSource,
        RecipientEmail = source.RecipientEmail,
        Subject = source.Subject,
        PlainTextBody = source.PlainTextBody,
        CreatedAt = source.CreatedAt
    };

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int ExecutionCount { get; private set; }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            ExecutionCount++;
            await operation(ct);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            ExecutionCount++;
            return await operation(ct);
        }

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
    }

    private sealed class RecordingGraphRepository : IRecipientNotificationGraphRepository
    {
        public NotificationIntent? Created { get; private set; }
        public Exception? CreateFailure { get; init; }
        public NotificationIntent? Loaded { get; init; }
        public int LoadCount => DeduplicationLoadCount + OccurrenceLoadCount;
        public int DeduplicationLoadCount { get; private set; }
        public int OccurrenceLoadCount { get; private set; }
        public int RepairCount { get; private set; }

        public Task<NotificationIntent> CreateGraphAsync(
            NotificationIntent intent,
            CancellationToken cancellationToken = default)
        {
            Created = intent;
            return CreateFailure is null
                ? Task.FromResult(intent)
                : Task.FromException<NotificationIntent>(CreateFailure);
        }

        public Task<NotificationIntent?> GetGraphByTenantAndDeduplicationKeyAsync(
            Guid tenantId,
            string deduplicationKey,
            CancellationToken cancellationToken = default)
        {
            DeduplicationLoadCount++;
            return Task.FromResult(Loaded);
        }

        public Task<NotificationIntent?> GetGraphByTenantOccurrenceAndRecipientAsync(
            Guid tenantId,
            Guid occurrenceId,
            Guid recipientUserId,
            CancellationToken cancellationToken = default)
        {
            OccurrenceLoadCount++;
            return Task.FromResult(Loaded);
        }

        public Task RepairMissingRecipientDeliveryRowsAsync(
            NotificationIntent winningIntent,
            IReadOnlyList<NotificationDelivery> expectedDeliveries,
            Notification? expectedNotification,
            EmailDispatchOutbox? expectedEmail,
            CancellationToken cancellationToken = default)
        {
            RepairCount++;
            return Task.CompletedTask;
        }
    }

    public enum GraphFailurePoint
    {
        None,
        AfterIntent,
        AfterEmail
    }

    private sealed class TransactionState
    {
        public NotificationIntent? StagedIntent { get; set; }
        public List<EmailDispatchOutbox> StagedEmails { get; } = [];

        public void Clear()
        {
            StagedIntent = null;
            StagedEmails.Clear();
        }
    }

    private sealed class TransactionalUnitOfWork(
        TransactionalGraphStore store,
        TransactionState transaction) : IUnitOfWork
    {
        public int ExecutionCount { get; private set; }
        public int RollbackCount { get; private set; }
        public List<string> Events { get; } = [];

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            await ExecuteInTransactionAsync<object?>(async innerCt =>
            {
                await operation(innerCt);
                return null;
            }, ct);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            ExecutionCount++;
            Events.Add("begin");
            transaction.Clear();
            try
            {
                T result = await operation(ct);
                if (transaction.StagedIntent is not null)
                {
                    store.Commit(transaction);
                }
                Events.Add("commit");
                transaction.Clear();
                return result;
            }
            catch
            {
                RollbackCount++;
                Events.Add("rollback");
                transaction.Clear();
                throw;
            }
        }

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
    }

    private sealed class TransactionalGraphRepository(
        TransactionalGraphStore store,
        TransactionState transaction,
        GraphFailurePoint failurePoint = GraphFailurePoint.None) : IRecipientNotificationGraphRepository
    {
        public async Task<NotificationIntent> CreateGraphAsync(
            NotificationIntent intent,
            CancellationToken cancellationToken = default)
        {
            transaction.StagedIntent = intent;
            if (failurePoint == GraphFailurePoint.AfterIntent)
            {
                throw new InvalidOperationException($"Injected graph failure: {failurePoint}");
            }

            transaction.StagedEmails.AddRange(
                intent.Deliveries
                    .Select(delivery => delivery.EmailDispatchOutbox)
                    .Where(email => email is not null)
                    .Cast<EmailDispatchOutbox>());
            if (failurePoint == GraphFailurePoint.AfterEmail)
            {
                throw new InvalidOperationException($"Injected graph failure: {failurePoint}");
            }

            await store.WaitForConcurrentCreatesAsync(cancellationToken);
            return intent;
        }

        public Task<NotificationIntent?> GetGraphByTenantAndDeduplicationKeyAsync(
            Guid tenantId,
            string deduplicationKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(store.Load(tenantId, deduplicationKey));

        public Task<NotificationIntent?> GetGraphByTenantOccurrenceAndRecipientAsync(
            Guid tenantId,
            Guid occurrenceId,
            Guid recipientUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(store.Load(tenantId, occurrenceId, recipientUserId));

        public Task RepairMissingRecipientDeliveryRowsAsync(
            NotificationIntent winningIntent,
            IReadOnlyList<NotificationDelivery> expectedDeliveries,
            Notification? expectedNotification,
            EmailDispatchOutbox? expectedEmail,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TransactionalGraphStore(int expectedConcurrentCreates = 1)
    {
        private readonly Lock _lock = new();
        private readonly TaskCompletionSource _allCreatesArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _createArrivalCount;

        public List<NotificationIntent> CommittedIntents { get; } = [];
        public List<EmailDispatchOutbox> CommittedEmails { get; } = [];

        public async Task WaitForConcurrentCreatesAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _createArrivalCount) >= expectedConcurrentCreates)
            {
                _allCreatesArrived.TrySetResult();
            }

            await _allCreatesArrived.Task.WaitAsync(cancellationToken);
        }

        public void Commit(TransactionState transaction)
        {
            NotificationIntent intent = transaction.StagedIntent
                ?? throw new InvalidOperationException("No recipient graph was staged for commit.");
            lock (_lock)
            {
                if (CommittedIntents.Any(existing =>
                        existing.TenantId == intent.TenantId
                        && existing.DeduplicationKey == intent.DeduplicationKey))
                {
                    throw new NotificationIntentDeduplicationConflictException(new InvalidOperationException("23505 exact deduplication race"));
                }

                CommittedIntents.Add(intent);
                CommittedEmails.AddRange(transaction.StagedEmails);
            }
        }

        public NotificationIntent? Load(Guid tenantId, string deduplicationKey)
        {
            lock (_lock)
            {
                return CommittedIntents.SingleOrDefault(intent =>
                    intent.TenantId == tenantId
                    && intent.DeduplicationKey == deduplicationKey);
            }
        }

        public NotificationIntent? Load(Guid tenantId, Guid occurrenceId, Guid recipientUserId)
        {
            lock (_lock)
            {
                return CommittedIntents.SingleOrDefault(intent =>
                    intent.TenantId == tenantId
                    && intent.FanoutOccurrenceId == occurrenceId
                    && intent.RecipientUserId == recipientUserId);
            }
        }
    }
}
