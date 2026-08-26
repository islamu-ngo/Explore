// ABOUTME: Marks write endpoints whose response bodies must never enter generic idempotency storage.
// ABOUTME: Lets application-owned idempotency protect one-time secret disclosure without HTTP replay.

namespace Explore.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SuppressIdempotencyResponseStorageAttribute : Attribute;
