// ABOUTME: Creates bounded non-reversible partitions for opaque authenticated provider identities.
// ABOUTME: Length-prefixes scheme and subject so equal subjects remain isolated across authentication schemes.

using System.Security.Cryptography;
using System.Text;

namespace Explore.API.Authentication;

internal static class AuthenticationPartitionFingerprint
{
    internal static string Create(string authenticationScheme, string opaqueSubject)
    {
        string canonical = $"{authenticationScheme.Length}:{authenticationScheme}|{opaqueSubject.Length}:{opaqueSubject}";
        return "provider:" + Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
