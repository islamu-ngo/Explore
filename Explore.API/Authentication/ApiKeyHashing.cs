// ABOUTME: Provides deterministic hashing helpers for the Phase 0 API-key authentication spike.
// ABOUTME: Keeps raw API keys out of config storage while allowing constant-time verification in the handler.

using System.Security.Cryptography;
using System.Text;

namespace Explore.API.Authentication;

public static class ApiKeyHashing
{
    public static string ComputeHash(string rawApiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawApiKey);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawApiKey));
        return Convert.ToBase64String(bytes);
    }

    public static bool MatchesHash(string rawApiKey, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        try
        {
            var actualBytes = Convert.FromBase64String(ComputeHash(rawApiKey));
            var expectedBytes = Convert.FromBase64String(expectedHash);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
