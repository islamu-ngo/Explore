// ABOUTME: Fail-closed registration callback verifier used until a provider-neutral verifier is supplied by tests or hosting.
// ABOUTME: Prevents unsigned provider callbacks from being accepted when no concrete proof mechanism is configured.

using Explore.Application.Contracts.Services.Registration;

namespace Explore.API.Services;

public sealed class RejectingRegistrationProviderCallbackVerifier : IRegistrationProviderCallbackVerifier
{
    public Task<RegistrationProviderCallbackVerificationResult> VerifyCallbackAsync(
        RegistrationProviderCallbackVerificationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new RegistrationProviderCallbackVerificationResult(false, "registration_callback_verifier_not_configured"));
}
