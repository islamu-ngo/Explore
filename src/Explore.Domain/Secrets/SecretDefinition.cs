// ABOUTME: Static definition of a single secret-backed setting the platform knows about.
// ABOUTME: The SecretDefinitionRegistry is the source-of-truth for allowed keys, scopes, sources, and Infisical defaults.

using Explore.Domain.Enums;

namespace Explore.Domain.Secrets;

/// <summary>
/// Describes a secret-backed setting the platform understands.
/// <para>
/// The definition is the platform's policy record: it declares which scopes the secret may be bound at,
/// which <see cref="SecretSourceType"/>s are valid, whether the secret is required during bootstrap
/// (which forbids <see cref="SecretSourceType.InlineEncrypted"/>), and the default Infisical folder + key
/// under the user-specified layout.
/// </para>
/// <para>
/// The <see cref="SecretBinding"/> entity is the control-plane row that selects an active source; the
/// definition is the invariant shield that rejects illegal combinations before they reach the database.
/// </para>
/// </summary>
public sealed record SecretDefinition
{
    public required string Key { get; init; }

    public required IReadOnlyList<SecretScope> AllowedScopes { get; init; }

    public required IReadOnlyList<SecretSourceType> AllowedSources { get; init; }

    /// <summary>
    /// Infisical folder (e.g. "/postgresql"); combined with <see cref="DefaultInfisicalKey"/>
    /// forms the default <see cref="SecretBinding"/> reference when binding via
    /// <see cref="SecretSourceType.Infisical"/>.
    /// </summary>
    public required string DefaultInfisicalPath { get; init; }

    /// <summary>Infisical secret name in SCREAMING_SNAKE_CASE (e.g. "POSTGRESQL_PASSWORD").</summary>
    public required string DefaultInfisicalKey { get; init; }

    /// <summary>Default environment variable name (SCREAMING_SNAKE_CASE); usually equals <see cref="DefaultInfisicalKey"/>.</summary>
    public required string DefaultEnvironmentVariableName { get; init; }

    /// <summary>
    /// True when this secret is required before the persistence layer can start
    /// (e.g. postgresql.password). Bootstrap secrets MAY NOT be bound as
    /// <see cref="SecretSourceType.InlineEncrypted"/> — inline storage lives in the
    /// DB the secret itself is meant to unlock.
    /// </summary>
    public required bool IsBootstrapSecret { get; init; }

    /// <summary>Short human-readable description shown in the admin UI alongside state metadata.</summary>
    public required string Description { get; init; }
}
