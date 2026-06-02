// ABOUTME: Bounded provider inventory item for local-first storage reconciliation.
// ABOUTME: Contains safe metadata needed to compare backing objects with application records.

namespace Explore.Application.Models.Storage;

public sealed record FileStorageInventoryObject(
    string Provider,
    string ObjectKey,
    long SizeBytes,
    DateTime? LastModifiedUtc);
