// ABOUTME: Registry-validated factory + mutation helpers for SecretBinding.
// ABOUTME: Enforces SecretDefinitionRegistry invariants (allowed scope/source, scope-id consistency, metadata exclusivity).

using Explore.Domain.Enums;

namespace Explore.Domain.Secrets;

public partial class SecretBinding
{
    /// <summary>
    /// Creates a binding that references a secret stored in Infisical.
    /// </summary>
    /// <exception cref="ArgumentException">Unknown setting key, disallowed scope/source, or scope/scopeId inconsistency.</exception>
    public static SecretBinding CreateInfisical(
        string settingKey,
        SecretScope scope,
        Guid? scopeId,
        string environment,
        string path,
        string key,
        bool isLocked = false)
    {
        var definition = GetValidDefinition(settingKey, scope, scopeId, SecretSourceType.Infisical);

        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Infisical environment is required.", nameof(environment));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Infisical path is required.", nameof(path));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Infisical key is required.", nameof(key));
        _ = definition; // definition accepted by the check above; reference here keeps factory verbose-documented.

        return new SecretBinding
        {
            SettingKey = settingKey,
            Scope = scope,
            ScopeId = scopeId,
            SourceType = SecretSourceType.Infisical,
            InfisicalEnvironment = environment,
            InfisicalPath = path,
            InfisicalKey = key,
            IsLocked = isLocked,
        };
    }

    /// <summary>
    /// Creates a binding that stores an already-encrypted ciphertext inline in the settings database.
    /// The caller is responsible for producing <paramref name="ciphertext"/> via Data Protection.
    /// </summary>
    /// <exception cref="ArgumentException">Bootstrap secrets forbid InlineEncrypted; DB cannot decrypt a secret it needs to unlock.</exception>
    public static SecretBinding CreateInlineEncrypted(
        string settingKey,
        SecretScope scope,
        Guid? scopeId,
        byte[] ciphertext,
        int ciphertextVersion,
        bool isLocked = false)
    {
        GetValidDefinition(settingKey, scope, scopeId, SecretSourceType.InlineEncrypted);

        if (ciphertext is null || ciphertext.Length == 0)
            throw new ArgumentException("Inline ciphertext must be non-empty.", nameof(ciphertext));
        if (ciphertextVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(ciphertextVersion), "Ciphertext version must be >= 1.");

        return new SecretBinding
        {
            SettingKey = settingKey,
            Scope = scope,
            ScopeId = scopeId,
            SourceType = SecretSourceType.InlineEncrypted,
            InlineCiphertext = ciphertext,
            InlineCiphertextVersion = ciphertextVersion,
            IsLocked = isLocked,
        };
    }

    /// <summary>
    /// Creates a binding that points at a process environment variable.
    /// </summary>
    public static SecretBinding CreateEnvironmentVariable(
        string settingKey,
        SecretScope scope,
        Guid? scopeId,
        string variableName,
        bool isLocked = false)
    {
        GetValidDefinition(settingKey, scope, scopeId, SecretSourceType.EnvironmentVariable);

        if (string.IsNullOrWhiteSpace(variableName))
            throw new ArgumentException("Environment variable name is required.", nameof(variableName));

        return new SecretBinding
        {
            SettingKey = settingKey,
            Scope = scope,
            ScopeId = scopeId,
            SourceType = SecretSourceType.EnvironmentVariable,
            EnvironmentVariableName = variableName,
            IsLocked = isLocked,
        };
    }

    /// <summary>
    /// Switches this binding to point at an Infisical reference, resetting all competing metadata
    /// and clearing the last-validation state so re-validation is forced.
    /// </summary>
    public void SwitchToInfisical(string environment, string path, string key)
    {
        GetValidDefinition(SettingKey, Scope, ScopeId, SecretSourceType.Infisical);

        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Infisical environment is required.", nameof(environment));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Infisical path is required.", nameof(path));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Infisical key is required.", nameof(key));

        ClearMetadata();
        SourceType = SecretSourceType.Infisical;
        InfisicalEnvironment = environment;
        InfisicalPath = path;
        InfisicalKey = key;
        ResetValidation();
    }

    /// <summary>
    /// Switches this binding to store an inline-encrypted ciphertext, resetting all competing metadata
    /// and clearing the last-validation state.
    /// </summary>
    public void SwitchToInlineEncrypted(byte[] ciphertext, int ciphertextVersion)
    {
        GetValidDefinition(SettingKey, Scope, ScopeId, SecretSourceType.InlineEncrypted);

        if (ciphertext is null || ciphertext.Length == 0)
            throw new ArgumentException("Inline ciphertext must be non-empty.", nameof(ciphertext));
        if (ciphertextVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(ciphertextVersion), "Ciphertext version must be >= 1.");

        ClearMetadata();
        SourceType = SecretSourceType.InlineEncrypted;
        InlineCiphertext = ciphertext;
        InlineCiphertextVersion = ciphertextVersion;
        ResetValidation();
    }

    /// <summary>
    /// Switches this binding to reference an environment variable, resetting all competing metadata
    /// and clearing the last-validation state.
    /// </summary>
    public void SwitchToEnvironmentVariable(string variableName)
    {
        GetValidDefinition(SettingKey, Scope, ScopeId, SecretSourceType.EnvironmentVariable);

        if (string.IsNullOrWhiteSpace(variableName))
            throw new ArgumentException("Environment variable name is required.", nameof(variableName));

        ClearMetadata();
        SourceType = SecretSourceType.EnvironmentVariable;
        EnvironmentVariableName = variableName;
        ResetValidation();
    }

    /// <summary>
    /// Records the outcome of a validation attempt. Error messages must be pre-sanitised by the caller
    /// — they are rendered in the admin UI and must NOT contain secret values.
    /// </summary>
    public void RecordValidation(SecretValidationResult result, Guid? validatedBy, string? sanitisedError = null)
    {
        if (result == SecretValidationResult.Failure && string.IsNullOrWhiteSpace(sanitisedError))
            throw new ArgumentException("A sanitised error message is required when recording a failure.", nameof(sanitisedError));

        LastValidationResult = result;
        LastValidatedAt = DateTime.UtcNow;
        LastValidatedBy = validatedBy;
        LastValidationError = result == SecretValidationResult.Failure ? sanitisedError : null;
    }

    /// <summary>
    /// Validates the tuple (settingKey, scope, scopeId, sourceType) against the registry.
    /// Throws on the first violation with a message identifying the offending rule.
    /// </summary>
    private static SecretDefinition GetValidDefinition(
        string settingKey,
        SecretScope scope,
        Guid? scopeId,
        SecretSourceType sourceType)
    {
        if (string.IsNullOrWhiteSpace(settingKey))
            throw new ArgumentException("Setting key is required.", nameof(settingKey));

        var definition = SecretDefinitionRegistry.TryGet(settingKey)
            ?? throw new ArgumentException(
                $"Unknown secret key '{settingKey}'. Add a definition to SecretDefinitionRegistry first.",
                nameof(settingKey));

        if (!definition.AllowedScopes.Contains(scope))
            throw new ArgumentException(
                $"Scope '{scope}' is not allowed for secret '{settingKey}'. " +
                $"Allowed: {string.Join(", ", definition.AllowedScopes)}.",
                nameof(scope));

        switch (scope)
        {
            case SecretScope.Instance when scopeId.HasValue:
                throw new ArgumentException(
                    "Instance-scoped bindings must have a null scopeId.", nameof(scopeId));
            case SecretScope.Tenant when !scopeId.HasValue:
                throw new ArgumentException(
                    "Tenant-scoped bindings require a non-null scopeId (the tenant id).", nameof(scopeId));
        }

        if (!definition.AllowedSources.Contains(sourceType))
        {
            // Bootstrap secrets cannot be InlineEncrypted - this is a domain invariant
            // (the DB cannot unlock itself). That is an InvalidOperationException (business rule),
            // not an ArgumentException (caller error).
            if (definition.IsBootstrapSecret && sourceType == SecretSourceType.InlineEncrypted)
            {
                throw new InvalidOperationException(
                    $"Bootstrap secret '{settingKey}' cannot be InlineEncrypted. " +
                    $"The database cannot unlock its own connection-string secrets. " +
                    $"Allowed sources: {string.Join(", ", definition.AllowedSources)}.");
            }

            throw new ArgumentException(
                $"Source '{sourceType}' is not allowed for secret '{settingKey}'. " +
                $"Allowed: {string.Join(", ", definition.AllowedSources)}.",
                nameof(sourceType));
        }

        return definition;
    }

    /// <summary>Clears every metadata field; use before switching SourceType.</summary>
    private void ClearMetadata()
    {
        InfisicalEnvironment = null;
        InfisicalPath = null;
        InfisicalKey = null;
        EnvironmentVariableName = null;
        InlineCiphertext = null;
        InlineCiphertextVersion = null;
    }

    /// <summary>Resets validation state to NotValidated; switching source always forces a re-validate.</summary>
    private void ResetValidation()
    {
        LastValidationResult = SecretValidationResult.NotValidated;
        LastValidationError = null;
        LastValidatedAt = null;
        LastValidatedBy = null;
    }
}
