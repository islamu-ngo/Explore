// ABOUTME: Application boundary for executing report decisions through local or provider-backed enforcement.
// ABOUTME: Keeps retry, idempotency, and provider response handling outside controllers and handlers.

using Explore.Application.Features.EventReporting.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IReportDecisionExecutor
{
    Task<ReportDecisionExecutionResult> ExecuteAsync(
        ReportDecisionExecutionEnvelope envelope,
        CancellationToken cancellationToken = default);
}
