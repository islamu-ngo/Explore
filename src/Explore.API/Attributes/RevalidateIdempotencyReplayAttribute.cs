// ABOUTME: Marks capability-scoped writes that must re-run current access checks instead of returning cached replay.
// ABOUTME: Preserves key collision detection while allowing expiry and capability revocation to fail closed.

namespace Explore.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class RevalidateIdempotencyReplayAttribute : Attribute;
