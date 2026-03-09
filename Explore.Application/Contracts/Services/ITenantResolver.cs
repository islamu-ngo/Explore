// ABOUTME: Defines a composable tenant resolver step for the current execution context.
// ABOUTME: Keeps resolver orchestration separate from request storage and consumer-facing tenant access.

namespace Explore.Application.Contracts.Services;

public interface ITenantResolver
{
    string Name { get; }

    int Priority { get; }

    Guid? ResolveTenantId();
}
