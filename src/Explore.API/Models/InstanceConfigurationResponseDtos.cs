// ABOUTME: Typed API response contracts for setup-secret, SMTP, and provider configuration probes.
// ABOUTME: Keeps generated clients strongly typed instead of exposing anonymous object responses.

namespace Explore.API.Models;

public sealed record SetupSecretValidationResultDto(bool Valid);

public sealed record SmtpConnectionTestResultDto(bool Success, string Message);

public sealed record ProviderConfigurationStatusDto(bool Configured);
