// ABOUTME: Endpoint metadata marking write actions that require an Idempotency-Key request header.
// ABOUTME: Consumed by IdempotencyMiddleware and the PublicTransactional architecture governance tests.

namespace Explore.API.Attributes;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = false)]
public sealed class RequireIdempotencyKeyAttribute : Attribute;
