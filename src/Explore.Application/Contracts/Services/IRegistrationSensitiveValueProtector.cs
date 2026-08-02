// ABOUTME: Defines the application boundary for versioned protection of sensitive registration values.
// ABOUTME: Keeps plaintext transient while exposing only opaque ciphertext and purpose-version metadata.

namespace Explore.Application.Contracts.Services;

public interface IRegistrationSensitiveValueProtector
{
    RegistrationProtectedValue Protect(string plaintext);
    string Unprotect(string ciphertext, int keyVersion);
}

public sealed record RegistrationProtectedValue(string Ciphertext, int KeyVersion);
