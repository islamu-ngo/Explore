<!-- ABOUTME: Documents the shared CI/CD implementation surface used by forge-native adapters. -->
<!-- ABOUTME: Keeps GitHub and mirror-provider CI/CD aligned with one reviewed implementation home. -->

# CI/CD Implementation

`.ci` is the repository-owned CI/CD implementation layer that can be reused across forges.

GitHub-native discovery files stay in their required location:

- GitHub Actions: `.github/workflows/`

All shared scripts, policy validators, evidence writers, local composite actions, and cross-forge CI/CD definitions live here so they are reviewed and owned as one CI/CD surface. Codeberg and other mirrors should point their CI/CD settings at this folder when the provider supports custom pipeline locations.

Current layout:

- `.ci/actions/deploy-coolify/` contains the shared GitHub Coolify deployment composite action.
- `.ci/scripts/` contains file-based C# policy and evidence scripts invoked with `dotnet run <script>.cs -- ...`.
- `.ci/spectral.yaml` contains the project-owned OpenAPI lint ruleset.

Do not make `.github` a symlink to `.ci`: GitHub discovers workflows from `.github/workflows`, and local reusable workflow calls also use that path.
