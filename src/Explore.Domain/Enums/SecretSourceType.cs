// ABOUTME: Declares WHERE a secret value is fetched from for a given SecretBinding.
// ABOUTME: DB stores only this marker + that source's metadata - never the value itself.

namespace Explore.Domain.Enums;

public enum SecretSourceType
{
    /// <summary>Value lives in Infisical; DB stores environment + path + key reference.</summary>
    Infisical = 0,

    /// <summary>Value lives in an environment variable; DB stores the variable name.</summary>
    EnvironmentVariable = 1,
}
