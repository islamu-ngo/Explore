<!-- ABOUTME: Describes the standalone release-engineering command and trusted local tool-bundle boundary. -->
<!-- ABOUTME: Documents pinned git-cliff verification without runtime downloads or provider coupling. -->

# Release Engineering

`ISLAMU.ReleaseEngineering` is a standalone `net10.0` console project for
governed release tooling. It has no references to product projects.

```bash
export ISLAMU_RELEASE_TOOL_BUNDLE=/absolute/path/to/promoted/bundle
dotnet run --project eng/release/src/ISLAMU.ReleaseEngineering/ISLAMU.ReleaseEngineering.csproj -- verify-tools
```

The bundle directory must contain the platform executable at its lock-file name:
`git-cliff` on Linux x64 or `git-cliff.exe` on Windows x64. `verify-tools`
selects only the current approved platform from `toolchain.lock.json`, checks the
executable SHA-256, and requires the exact `git-cliff 2.13.1` version response.
Missing, malformed, mismatched, unsupported, noisy, failed, or hung tools fail
closed. The command never downloads a tool; bundle acquisition and promotion are
operator responsibilities outside this process.
