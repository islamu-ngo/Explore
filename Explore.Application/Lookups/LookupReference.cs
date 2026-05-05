// ABOUTME: Primitive API-facing lookup reference for normalized enum-style lookup tables.
// ABOUTME: Keeps response contracts on stable IDs/codes/names without exposing EF entities.

namespace Explore.Application.Lookups;

public readonly record struct LookupReference(int Id, string Code, string Name);
