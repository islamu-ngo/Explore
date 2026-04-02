// ABOUTME: Generic implementation of IAuthorizableResourceDescriptor using delegate-based property extraction.
// ABOUTME: Each instance is configured with lambdas for extracting resource ID, attributes, and scope from a DTO.

namespace Explore.Application.Authorization;

/// <summary>
/// Generic implementation of <see cref="IAuthorizableResourceDescriptor{TResource}"/>
/// that extracts authorization metadata using delegate functions.
/// <para>
/// Create instances via the <see cref="ResourceDescriptors"/> catalog rather than constructing directly.
/// Each descriptor is immutable and thread-safe.
/// </para>
/// </summary>
/// <typeparam name="TResource">The DTO type from which authorization metadata is extracted.</typeparam>
public sealed class ResourceDescriptor<TResource> : IAuthorizableResourceDescriptor<TResource>
{
    private static readonly IReadOnlyDictionary<string, object> EmptyAttributes =
        new Dictionary<string, object>().AsReadOnly();

    private readonly Func<TResource, string> _getResourceId;
    private readonly Func<TResource, IReadOnlyDictionary<string, object>>? _getResourceAttributes;
    private readonly Func<TResource, AuthorizationScope>? _getScope;

    /// <summary>
    /// Creates a new resource descriptor with the given extraction functions.
    /// </summary>
    /// <param name="kind">The Cerbos resource kind string. Must match a <see cref="ResourceKinds"/> constant.</param>
    /// <param name="getResourceId">Extracts the unique resource ID (typically primary key) from the DTO.</param>
    /// <param name="getResourceAttributes">
    /// Extracts resource attributes for Cerbos policy evaluation.
    /// When <c>null</c>, returns an empty dictionary.
    /// </param>
    /// <param name="getScope">
    /// Extracts the authorization scope for per-tenant/org policy resolution.
    /// When <c>null</c>, returns <see cref="AuthorizationScope.Empty"/>.
    /// </param>
    public ResourceDescriptor(
        string kind,
        Func<TResource, string> getResourceId,
        Func<TResource, IReadOnlyDictionary<string, object>>? getResourceAttributes = null,
        Func<TResource, AuthorizationScope>? getScope = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentNullException.ThrowIfNull(getResourceId);

        Kind = kind;
        _getResourceId = getResourceId;
        _getResourceAttributes = getResourceAttributes;
        _getScope = getScope;
    }

    /// <inheritdoc />
    public string Kind { get; }

    /// <inheritdoc />
    public string GetResourceId(TResource resource) => _getResourceId(resource);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetResourceAttributes(TResource resource) =>
        _getResourceAttributes?.Invoke(resource) ?? EmptyAttributes;

    /// <inheritdoc />
    public AuthorizationScope GetScope(TResource resource) =>
        _getScope?.Invoke(resource) ?? AuthorizationScope.Empty;
}
