---
description: Run focused project-level tests and choose invariant-oriented evidence for each change.
---

# TUnit Testing Conventions

ISLAMU Event uses **TUnit** as its primary, modern test runner. We prioritize strict test quality over quantity, targeting core business invariants rather than brittle mock assertions.

---

## 1. Focused Developer Loop

During active development, never run full solution-level tests. Build the solution first, then run your target test class using a **TUnit tree-node filter**:

```bash
# 1. Build product code
dotnet build --configuration Release --verbosity quiet

# 2. Run target test class with tree-node filter
dotnet test --project tests/Explore.Application.Tests/Explore.Application.Tests.csproj \
  --configuration Release \
  --treenode-filter "/*/*/*<TestClass>/*"
```

> [!NOTE]
> `--treenode-filter` is the native TUnit test selector (~1.5s execution); VSTest `--filter` syntax is not interchangeable.

---

## 2. Invariant-Breaker Testing Strategy

Author tests that guard real platform invariants:
* **Domain State Machines**: Invalid transitions, capacity limits, and lifecycle events.
* **Tenant Isolation**: Queries that attempt cross-tenant reads or operate without ambient tenant context (see [Multi-Tenancy](../security-and-identity/multi-tenancy.md)).
* **Security & Fail-Closed Gates**: Unauthenticated write attempts, expired session tokens, or missing [Cerbos PDP](../security-and-identity/authorization.md) policies.
* **Concurrency & Race Conditions**: Optimistic concurrency tokens during ticket purchases or settings edits.
* **Anti-Resurrection Erasure**: Replaying erased user facts against restored databases (see [Disaster Recovery Invariant](../security-and-identity/privacy-erasure.md#the-golden-rule-of-disaster-recovery)).

> [!WARNING]
> Prohibited in tests:
> * Tautological mock mirroring (e.g. asserting `repository.Received(1)` without verifying state).
> * Scraping raw source code or CSS class text.
> * Testing underlying third-party framework behavior (e.g. testing whether EF Core honors cancellation tokens).

---

## 3. Documentation-Only Work

For Markdown or GitBook documentation changes (such as edits in `docs/public/`), do **not** run `dotnet build` or .NET test suites. Verification is strictly scoped to Markdown formatting, link integrity, and schema checks.

---

## Related Guides & Next Steps

* **[Local Development Guide](local-development.md)** — Set up your local developer tools.
* **[Clean Architecture Conventions](clean-architecture.md)** — Architectural layer responsibilities.
* **[Authorization & Access Control](../security-and-identity/authorization.md)** — Writing authorization tests.
* **[Privacy Erasure & Anti-Resurrection](../security-and-identity/privacy-erasure.md)** — Testing database restore gates.
