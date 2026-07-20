// ABOUTME: Architecture guardrails for registration transaction ownership and retry-stable transition identity.
// ABOUTME: Keeps serializable orchestration in Application handlers and persistence repositories transaction-local.

namespace Event.Architecture.Tests;

public sealed class RegistrationTransactionBoundaryArchitectureTests
{
    private static readonly string[] HandlerFiles =
    [
        "CreateEventRegistrationCommandHandler.cs",
        "UpdateEventRegistrationCommandHandler.cs",
        "DeleteEventRegistrationCommandHandler.cs"
    ];

    [Test]
    public async Task RegistrationMutationHandlersOwnSerializableUnitOfWorkBoundary()
    {
        string handlersRoot = ContextSystemHelpers.RepoPath(
            "Explore.Application",
            "Features",
            "EventRegistrations",
            "Handlers",
            "Commands");
        var violations = new List<string>();

        foreach (string fileName in HandlerFiles)
        {
            string content = await File.ReadAllTextAsync(Path.Combine(handlersRoot, fileName));
            if (!content.Contains("IUnitOfWork", StringComparison.Ordinal)
                || !content.Contains("ExecuteSerializableAsync", StringComparison.Ordinal))
            {
                violations.Add($"{fileName} must coordinate its mutation through IUnitOfWork.ExecuteSerializableAsync.");
            }

            int occurrenceIndex = content.IndexOf("Guid.CreateVersion7()", StringComparison.Ordinal);
            int occurredAtIndex = content.IndexOf("DateTimeOffset.UtcNow", StringComparison.Ordinal);
            int transactionIndex = content.IndexOf("ExecuteSerializableAsync", StringComparison.Ordinal);
            if (occurrenceIndex < 0 || occurredAtIndex < 0
                || occurrenceIndex > transactionIndex || occurredAtIndex > transactionIndex)
            {
                violations.Add($"{fileName} must generate occurrence identity and timestamp before the retryable transaction delegate.");
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("retry delegates may run more than once, so durable registration transition identity must be stable across attempts.");
    }

    [Test]
    public async Task RegistrationRepositoriesDoNotOwnExecutionStrategyOrTransactionLifecycle()
    {
        string repositoriesRoot = ContextSystemHelpers.RepoPath("Explore.Persistence", "Repositories");
        string[] repositoryFiles =
        [
            "EventRegistrationIntentRepository.cs",
            "EventRegistrationRepository.cs"
        ];
        string[] forbiddenOperations =
        [
            "CreateExecutionStrategy",
            "BeginTransaction",
            "CommitAsync",
            "RollbackAsync"
        ];
        var violations = new List<string>();

        foreach (string fileName in repositoryFiles)
        {
            string content = await File.ReadAllTextAsync(Path.Combine(repositoriesRoot, fileName));
            foreach (string forbiddenOperation in forbiddenOperations)
            {
                if (content.Contains(forbiddenOperation, StringComparison.Ordinal))
                {
                    violations.Add($"{fileName} contains repository-owned transaction operation {forbiddenOperation}.");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("Application owns the serializable unit of work; repositories only participate in the caller transaction.");
    }

    [Test]
    public async Task RegistrationReminderPreparationIsTransactionBoundAndPointerTriggerIsPostCommit()
    {
        string handlersRoot = ContextSystemHelpers.RepoPath(
            "Explore.Application",
            "Features",
            "EventRegistrations",
            "Handlers",
            "Commands");
        var violations = new List<string>();

        foreach (string fileName in HandlerFiles.Take(2))
        {
            string content = await File.ReadAllTextAsync(Path.Combine(handlersRoot, fileName));
            int transactionIndex = content.IndexOf("ExecuteSerializableAsync", StringComparison.Ordinal);
            int graphIdsIndex = content.IndexOf("EventReminderGraphIds.Create()", StringComparison.Ordinal);
            int triggerIndex = content.IndexOf("TriggerPreparedEventReminderAsync", StringComparison.Ordinal);
            int prepareIndex = content.IndexOf("PrepareEventReminderInCurrentTransactionAsync", StringComparison.Ordinal);

            if (graphIdsIndex < 0 || graphIdsIndex > transactionIndex)
            {
                violations.Add($"{fileName} must generate reminder graph ids before the retryable transaction delegate.");
            }

            if (prepareIndex < 0)
            {
                violations.Add($"{fileName} must prepare reminder persistence through the transaction-bound scheduler seam.");
            }

            if (triggerIndex < 0 || triggerIndex < transactionIndex)
            {
                violations.Add($"{fileName} must invoke the pointer trigger only after ExecuteSerializableAsync returns.");
            }
        }

        string schedulerPath = ContextSystemHelpers.RepoPath(
            "Explore.Application",
            "Services",
            "EventLifecycleScheduler.cs");
        string scheduler = await File.ReadAllTextAsync(schedulerPath);
        if (!scheduler.Contains("MaterializeInCurrentTransactionAsync", StringComparison.Ordinal)
            || scheduler.Contains("notificationMaterializer.MaterializeAsync", StringComparison.Ordinal))
        {
            violations.Add("EventLifecycleScheduler must materialize reminder delivery only through the caller transaction seam.");
        }

        await Assert.That(violations).IsEmpty()
            .Because("reminder delivery is durable transaction state while TickerQ is a post-commit pointer accelerator.");
    }
}
