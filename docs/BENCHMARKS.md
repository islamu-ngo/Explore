ABOUTME: Documents BenchmarkDotNet performance benchmarks and separates them from cold-start agent benchmarks.
ABOUTME: Defines run commands, interpretation rules, and limits so benchmark results are not treated as production SLO proof.

# Benchmarks

> **Audience:** Contributors | Operators | AI agents
> **Status:** Mixed
> **Owner:** Contributor Experience
> **Last Verified:** 2026-05-06
> **Source Anchors:** `Event.Benchmarks/`, `Event.Benchmarks/Configuration/BenchmarkConfig.cs`, `Event.Benchmarks/Program.cs`, `Directory.Packages.props`, `.claude/benchmarks/README.md`, `dev/pause/performance-optimization/performance-optimization-context.md`

This repository has two benchmark families with different goals:

| Benchmark Family | Purpose | Primary Source |
|---|---|---|
| Runtime microbenchmarks | Compare hot-path implementation choices inside the product codebase. | `Event.Benchmarks/` |
| Cold-start agent benchmarks | Evaluate whether a fresh AI agent can make architecture-compliant changes using only repo context. | `.claude/benchmarks/` |

Do not mix these result types. Runtime benchmark numbers are controlled-run microbenchmark evidence. Cold-start benchmark results are contribution-context evidence.

## Runtime Benchmark Project

`Event.Benchmarks` is a `net10.0` BenchmarkDotNet console project. It references `Explore.Application`, `Explore.Domain`, and `Explore.Persistence` so benchmark scenarios can exercise real application types without running the full API or Blazor hosts.

BenchmarkDotNet is centrally pinned in `Directory.Packages.props` and the project uses `BenchmarkSwitcher` from `Event.Benchmarks/Program.cs`, so filters are passed through BenchmarkDotNet arguments.

### Current Suites

| Suite | What It Compares | Source |
|---|---|---|
| Serialization | Source-generated `System.Text.Json` serialization/deserialization vs reflection-based serialization for `EventListDto`. | `Event.Benchmarks/Benchmarks/SerializationBenchmarks.cs` |
| EF Core query construction | Tracked query construction, no-tracking query construction, and compiled query invocation. | `Event.Benchmarks/Benchmarks/EfCoreQueryBenchmarks.cs` |
| Caching collections | Lookup behavior for `FrozenDictionary`, `Dictionary`, and `ConcurrentDictionary`, plus enumeration behavior for `FrozenDictionary` and `Dictionary`, at fixed sizes. | `Event.Benchmarks/Benchmarks/CachingBenchmarks.cs` |
| Collection processing | List/span/array lookup, LINQ vs manual loops, and `FrozenSet.Contains`. | `Event.Benchmarks/Benchmarks/CollectionBenchmarks.cs` |
| MediatR pipeline | `PerformanceBehavior<TRequest,TResponse>` overhead compared with direct handler invocation. | `Event.Benchmarks/Benchmarks/MediatRPipelineBenchmarks.cs` |
| String processing | Substring/span slicing, string concatenation, `StringBuilder`, contains, and GUID formatting. | `Event.Benchmarks/Benchmarks/StringProcessingBenchmarks.cs` |

## How To Run Runtime Benchmarks

Run benchmarks from the repository root and use Release configuration:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet run --configuration Release --project Event.Benchmarks -- --filter "*"
```

Useful BenchmarkDotNet filters:

```bash
dotnet run --configuration Release --project Event.Benchmarks -- --list flat
dotnet run --configuration Release --project Event.Benchmarks -- --filter "*Serialization*"
dotnet run --configuration Release --project Event.Benchmarks -- --filter "*Caching*"
```

The shared benchmark config enables memory, threading, and exception diagnosers plus GitHub Markdown, HTML, and full JSON exporters. No custom artifact path is configured, so BenchmarkDotNet writes to its default artifact directory for the run.

## Interpreting Runtime Results

Use runtime benchmark results to compare implementation choices under controlled microbenchmark conditions:

- Compare relative means, allocations, and diagnoser output between benchmark methods in the same suite.
- Keep hardware, runtime, branch, environment variables, and local machine load stable when comparing runs.
- Re-run a benchmark after changing the hot path and compare against a baseline from the same machine or CI runner.
- Treat large regressions as investigation triggers, not automatic release blockers, unless the change has an agreed performance budget.

### What Runtime Benchmarks Do Not Prove

Runtime microbenchmarks are not production load tests:

- They do not prove API P95/P99 latency, throughput, or production SLO compliance.
- They do not model multi-instance traffic, reverse proxies, Keycloak, Redis, MinIO, browser behavior, or real tenant concurrency.
- EF Core benchmarks in this project build or invoke query shapes; they do not execute a representative live database workload.
- Synthetic collection/string/cache inputs are implementation-relative and should not be treated as user workload evidence.
- BenchmarkDotNet reports are evidence for a run, not permanent claims about future runtime versions or infrastructure.

Use [OPERATIONS.md](OPERATIONS.md), [TROUBLESHOOTING.md](TROUBLESHOOTING.md), and production telemetry for runtime incident analysis. Use dedicated load or end-to-end tests when the question is capacity, concurrency, or service-level behavior.

## Cold-Start Agent Benchmarks

`.claude/benchmarks/README.md` documents a separate manual benchmark suite for AI-agent contribution quality. Those scenarios test whether a fresh agent can classify intent, read required context, stay in scope, run expected verification, and satisfy acceptance criteria.

Cold-start benchmark results should be recorded under `dev/_journal/benchmark-reports/` as described by `.claude/benchmarks/README.md`. They are not runtime performance measurements and should not be cited as product throughput or latency evidence.

## Related Documentation

- [OPERATIONS.md](OPERATIONS.md) - runtime operation and observability context.
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - issue diagnosis and practical recovery.
- [TESTING.md](TESTING.md) - test project and verification command policy.
- [GOVERNANCE.md](GOVERNANCE.md) - engineering governance and context-system benchmark references.
- [../.claude/benchmarks/README.md](../.claude/benchmarks/README.md) - cold-start agent benchmark procedure.
