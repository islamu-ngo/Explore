// ABOUTME: Wires ASP.NET Core Data Protection to persist its key ring in the primary database via ExploreDbContext.
// ABOUTME: Used to Protect/Unprotect inline-encrypted secret bindings and any other Data Protection consumers (auth cookies, anti-forgery).

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Persistence.Extensions;

/// <summary>
/// Extension methods to register ASP.NET Core Data Protection with a database-backed key ring.
/// </summary>
/// <remarks>
/// Keys persisted via <c>PersistKeysToDbContext&lt;ExploreDbContext&gt;()</c> are shared across every
/// host that connects to the same database (API and Blazor Server) without requiring a shared
/// filesystem or Redis. This works for the minimal deployment (API + Blazor + Postgres only).
///
/// <para>
/// Security note: storing Data Protection keys in the same database as the ciphertext they protect
/// is a UI-leak / app-compromise defense, not a full database-compromise defense. An attacker who
/// obtains the database holds both halves. This is an intentional, documented trade-off for
/// self-hosted operators - see <c>docs/SECRETS.md</c>.
/// </para>
///
/// <para>
/// High-value bootstrap secrets (Postgres credentials, setup secret) MUST NOT be stored as
/// <see cref="Explore.Domain.Enums.SecretSourceType.InlineEncrypted"/>. The
/// <see cref="Explore.Domain.Secrets.SecretDefinitionRegistry"/> enforces this invariant by excluding
/// <c>InlineEncrypted</c> from their allowed sources.
/// </para>
/// </remarks>
public static class DataProtectionServiceCollectionExtensions
{
    /// <summary>
    /// Default application name for the Data Protection key ring.
    /// Must match across all hosts that share keys (API and Blazor).
    /// </summary>
    public const string DefaultApplicationName = "islamu-event";

    /// <summary>
    /// Registers ASP.NET Core Data Protection with keys persisted in the primary Postgres database
    /// via <see cref="ExploreDbContext"/>. Sets a stable application name so API and Blazor share
    /// the same key ring.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="applicationName">
    /// Optional application name override. Defaults to <see cref="DefaultApplicationName"/>.
    /// Must be identical across every host that shares the key ring.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddExploreDataProtection(
        this IServiceCollection services,
        string? applicationName = null)
    {
        services
            .AddDataProtection()
            .SetApplicationName(applicationName ?? DefaultApplicationName)
            .PersistKeysToDbContext<ExploreDbContext>();

        return services;
    }
}
