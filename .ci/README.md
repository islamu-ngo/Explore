<!-- ABOUTME: Documents the shared CI/CD implementation surface used by forge-native adapters. -->
<!-- ABOUTME: Keeps GitHub, Forgejo, Codeberg, and Tangled entrypoints aligned with one reviewed implementation home. -->

# CI/CD Implementation

`.ci` is the repository-owned implementation layer for CI/CD helpers that can be reused across forges.

Provider-native discovery files stay in their required locations:

- GitHub Actions: `.github/workflows/`
- Forgejo Actions: `.forgejo/workflows/`
- Codeberg Woodpecker: `.woodpecker/`
- Tangled Spindle: `.tangled/workflows/`

The provider files should stay thin. Shared scripts, policy validators, evidence writers, and local composite actions live here so they are reviewed and owned as one CI/CD surface.

Current layout:

- `.ci/actions/deploy-coolify/` contains the shared GitHub Coolify deployment composite action.
- `.ci/scripts/` contains file-based C# policy and evidence scripts invoked with `dotnet run <script>.cs -- ...`.
- `.ci/spectral.yaml` contains the project-owned OpenAPI lint ruleset.

Do not make `.github` a symlink to `.ci`: GitHub discovers workflows from `.github/workflows`, and local reusable workflow calls also use that path.
