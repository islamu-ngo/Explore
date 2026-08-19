// ABOUTME: Contract for observing which policy revision the selected authorization provider is serving.
// ABOUTME: Lets the PEP stamp decisions with a revision and fail closed when the revision is unknowable.

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// How much the application actually knows about the policy revision behind a decision.
/// </summary>
public enum AuthorizationRevisionCertainty
{
    /// <summary>
    /// The revision could not be established. The provider may be serving a stale, partial, or
    /// out-of-band-edited policy set and nothing can rule that out.
    /// </summary>
    Unknown = 0,

    /// <summary>The revision was read from the provider itself and identifies the policy set in force.</summary>
    Observed = 1
}

/// <summary>
/// The policy revision a provider is serving, together with how certain that observation is.
/// </summary>
/// <param name="Value">
/// Opaque, low-cardinality revision identifier, or <c>null</c> when <paramref name="Certainty"/> is
/// <see cref="AuthorizationRevisionCertainty.Unknown"/>. Comparable only within one provider deployment:
/// it is derived from provider-internal hashes whose algorithm may change between provider versions.
/// </param>
/// <param name="Certainty">Whether <paramref name="Value"/> was actually observed.</param>
/// <param name="ObservedAt">When the observation was taken.</param>
public sealed record AuthorizationRevision(
    string? Value,
    AuthorizationRevisionCertainty Certainty,
    DateTimeOffset ObservedAt)
{
    /// <summary>The revision is not established. Callers must treat this as a fail-closed signal.</summary>
    public static AuthorizationRevision Unknown(DateTimeOffset observedAt) =>
        new(null, AuthorizationRevisionCertainty.Unknown, observedAt);

    /// <summary>The revision was read from the provider.</summary>
    public static AuthorizationRevision Observed(string value, DateTimeOffset observedAt) =>
        new(value, AuthorizationRevisionCertainty.Observed, observedAt);

    /// <summary>Whether the policy set behind a decision is identified.</summary>
    public bool IsCertain => Certainty == AuthorizationRevisionCertainty.Observed && Value is not null;
}

/// <summary>
/// Observes the policy revision the selected authorization provider is currently serving.
/// <para>
/// This exists because the provider that decides is not necessarily serving the policy the application
/// published. Removing the handler-owned local carve-out made that gap load-bearing: no local evaluator
/// answers around a stale or unpublished policy package any more, so "which policy decided this?" has to
/// be answerable rather than assumed.
/// </para>
/// </summary>
public interface IAuthorizationRevisionProvider
{
    /// <summary>
    /// Returns the current revision. Implementations must be cheap enough to call on every decision batch —
    /// cache the observation and refresh it on a bounded interval rather than querying the provider per call.
    /// Implementations never throw for provider unavailability; they report
    /// <see cref="AuthorizationRevisionCertainty.Unknown"/> instead.
    /// </summary>
    ValueTask<AuthorizationRevision> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the cached observation so the next call re-reads the provider.
    /// <para>
    /// Call this after anything that changes which policy set is in force — publishing the package,
    /// switching provider mode, or changing provider configuration. Without it, a successful publish
    /// would leave the previous revision (or a stale <see cref="AuthorizationRevisionCertainty.Unknown"/>,
    /// still denying sensitive actions) in place for the rest of the cache window, and the operator would
    /// see a fix that appears not to have worked.
    /// </para>
    /// <para>
    /// This invalidates the calling replica only. Convergence across replicas is bounded by the cache
    /// duration, not by this call.
    /// </para>
    /// </summary>
    void Invalidate();
}
