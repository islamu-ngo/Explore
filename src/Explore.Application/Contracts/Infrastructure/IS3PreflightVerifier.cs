// ABOUTME: Application contract for provider-neutral S3-compatible storage preflight checks.
// ABOUTME: Keeps standard S3 verification behind Infrastructure without exposing SDK types.

using Explore.Application.Models.Storage;

namespace Explore.Application.Contracts.Infrastructure;

public interface IS3PreflightVerifier
{
    Task<S3PreflightResult> VerifyAsync(
        S3PreflightRequest request,
        CancellationToken cancellationToken = default);
}
