// ABOUTME: Response DTOs for instance onboarding endpoints replacing anonymous types.
// ABOUTME: Provides structured, documented response shapes for setup validation, connection tests, and configuration status.

namespace Explore.Application.DTOs.Onboarding;

/// <summary>
/// Response for the setup secret validation endpoint.
/// </summary>
public class SecretValidationResponseDto
{
    public bool Valid { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Response for storage and SMTP connection test endpoints.
/// </summary>
public class ConnectionTestResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response for the auth provider configuration status check endpoint.
/// </summary>
public class AuthProviderConfiguredResponseDto
{
    public bool Configured { get; set; }
}
