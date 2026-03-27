# Context: Explore.Persistence LINQ, GDPR & Schema Refactoring

**Last Updated**: 2026-03-26

## Key Files & Targets
- `Explore.Persistence/Configurations/Entities/InstancePolicySetConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/TenantPolicySetConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/OrganizationPolicySetConfiguration.cs`
- `Explore.Persistence/Repositories/GenericRepository.cs`
- `Explore.Persistence/Repositories/UserRepository.cs`
- `Explore.Persistence/Repositories/ActorRepository.cs`

## Architectural Decisions
- **Relational over JSON**: Explicitly abandoning `JSONB` for Policy Sets to favor traditional relational columns. This improves query performance and simplifies indexing, though it increases column count.
- **GDPR Compliance**: Adoption of the "extension table" pattern for PII (`UserPii`, `ActorPii`) allows for "forgetting" a user (GDPR Right to Erasure) by simply deleting the Pii record while maintaining referential integrity for non-identifying data (e.g. audit logs referencing the `UserId`).
- **Direct Deletion**: `ExecuteDeleteAsync` is preferred for PII erasure to ensure atomic, non-tracked deletion that is faster and more efficient for bulk or targeted cleanup.

## Essential Interface Signatures
```csharp
// IUserRepository.cs
Task<int> ForgetPiiAsync(Guid userId); // Deletes UserPii record

// IActorRepository.cs
Task<int> ForgetPiiAsync(Guid actorId); // Deletes ActorPii record

// IGenericRepository.cs
Task HardDeleteByIdAsync(TKey id); // New method to delete without loading
```

## JSONB Removal Strategy
The `.ToJson()` call in `InstancePolicySetConfiguration` currently looks like:
```csharp
builder.OwnsOne(x => x.Modules, modules => {
    modules.ToJson("modules_policy");
});
```
Removing `.ToJson("modules_policy")` will revert the mapping to "Owned Types" which by default flattens properties into columns in the `instance_policy_sets` table.

## GDPR Entities
Entities with dedicated PII tables that must be handled:
1. `UserPii` (UserId)
2. `ActorPii` (ActorId)
3. `OrganizationPii` (OrganizationId)
4. `LocationPii` (LocationId)