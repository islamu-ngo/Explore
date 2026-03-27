# Task Checklist: Explore.Persistence Modernization & GDPR

**Last Updated**: 2026-03-26

## Phase 1: Schema Refactoring (JSONB Removal)
- [ ] **Task 1.1**: Remove `.ToJson()` from `InstancePolicySetConfiguration.cs`.
- [ ] **Task 1.2**: Remove `.ToJson()` from `TenantPolicySetConfiguration.cs`.
- [ ] **Task 1.3**: Remove `.ToJson()` from `OrganizationPolicySetConfiguration.cs`.
- [ ] **Task 1.4**: Generate and verify EF Core migration for JSONB -> Relational.

## Phase 2: GDPR PII Erasure
- [ ] **Task 2.1**: Add `ForgetPiiAsync(Guid userId)` to `IUserRepository` and implement in `UserRepository` using `ExecuteDeleteAsync`.
- [ ] **Task 2.2**: Add `ForgetPiiAsync(Guid actorId)` to `IActorRepository` and implement in `ActorRepository` using `ExecuteDeleteAsync`.
- [ ] **Task 2.3**: Add PII deletion for `OrganizationRepository` and `LocationRepository` where applicable.
- [ ] **Task 2.4**: Refactor `GenericRepository.HardDelete` to use `ExecuteDeleteAsync` by ID.

## Phase 3: LINQ & EF Core 10 Modernization
- [ ] **Task 3.1**: Implement `.ToLookup()` in `CategoryTypeCategoriesRepository` and `TagTypeTagsRepository`.
- [ ] **Task 3.2**: Apply `.Chunk(100)` to `UserRepository.GetUsersByIdsAsync`.
- [ ] **Task 3.3**: Apply `.Chunk()` to `ExternalApiKeyRepository.GetByOwners`.
- [ ] **Task 3.4**: Optimize `EventRepository.GetMyEventsWithDetails` using `.SelectMany()`.

## Phase 4: Validation
- [ ] **Task 4.1**: Verify all `Event.Persistence.IntegrationTests` pass.
- [ ] **Task 4.2**: Manually verify PII deletion in the DB for a test user.
- [ ] **Task 4.3**: Check SQL logs for efficient `ExecuteDeleteAsync` translation.