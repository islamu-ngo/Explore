<!-- ABOUTME: Domain journal for testing harnesses, TUnit execution, Testcontainers, and SDK environments. -->
<!-- ABOUTME: Captures durable findings on test runners, rate-limit bypassing, and containerized integration testing. -->

# Testing & Environment Knowledge Ledger

> **Scope**: TUnit test runner, `Event.*Tests`, `Explore.Diagnostic`, Testcontainers, Podman/Docker, and CI environment.

---

## 1. Architectural Decisions

- **Forward-Only Ratchets**: `ApiLiabilityRatchetTests` enforces an exact allowlist for legacy debt. Introducing a new occurrence fails, and fixing an issue without removing its ratchet entry also fails. Never relax ratchets.
- **Testing Environment Rate Limiting**: ASP.NET Core rate limiting is completely disabled in the `Testing` environment (`NoLimiter` policy) because `WebApplicationFactory` test clients lack loopback IP resolution.
- **Targeted Project Test Execution**: Tests must always be run at the project level (`dotnet test --project <path>.csproj`), never at the whole-solution level.

---

## 2. Technical Insights & Patterns

- **TUnit Runner Filtering Quirks**: TUnit / Microsoft Testing Platform does NOT accept standard VSTest `--filter` syntax. Use full project execution (`dotnet test --project ...`) or TUnit's native tree-node filter syntax (`--treenode-filter "/*/*/*TestClassName/*"`).
- **All-Skipped Visual Tests Exit Code**: When every test matched by a filter is marked with a manual skip reason, TUnit reports `Zero tests ran` and exits with code `8`. Treat this as a skip status, not a build failure.
- **SDK Workload Corruption vs Code Regression**: If `dotnet build` fails before compilation with `MSB4242` ("missing manifests likely removed by package management"), the root cause is corrupted host SDK workloads, not repository code. Run `dotnet workload repair`.
- **Podman Testcontainers Endpoint Configuration**: Running Testcontainers under Podman requires exporting:
  ```bash
  export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock
  export TESTCONTAINERS_RYUK_DISABLED=true
  export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/run/user/$(id -u)/podman/podman.sock
  ```
  Without these variables, suites fail with `DockerUnavailableException`.

---

## 3. Failed Approaches & Lessons

- **Overnight Solution-Level Test Runs in Clean-Room Context**: Running `dotnet test` solution-wide causes massive context output and memory consumption. Always isolate test execution to the single relevant test project.
- **PowerShell Syntax in Bash Shell**: Shell verification commands must use strictly POSIX Bash syntax. Never use PowerShell constructs or aliases.
