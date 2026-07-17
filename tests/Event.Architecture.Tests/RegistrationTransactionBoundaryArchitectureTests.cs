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
}
