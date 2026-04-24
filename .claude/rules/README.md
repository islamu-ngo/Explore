<!-- ABOUTME: Index of path-scoped rules auto-loaded by Claude Code for this repository. -->
<!-- ABOUTME: Explains how rule files refine intent-scoped context without duplicating canonical docs. -->

# Path-Scoped Rules

Claude Code auto-loads rule files in this folder when the file being edited matches a rule's YAML `paths:` glob.

## How To Use These Rules

| Principle | Meaning |
|---|---|
| Intent first | `intents.yaml` is primary; path rules refine it |
| Canonical docs win | `docs/QUICK_REFERENCE.md` and `docs/GOVERNANCE.md` outrank every rule here |
| Cross-reference only | Rules point at canonical docs; they must not duplicate them |
| Surgical context | Keep rules specific to file paths, not whole-project summaries |

See [`_schema.md`](_schema.md) before adding or editing any rule file.

## Indexed Rules

| Rule | Paths | Focus |
|---|---|---|
| `blazor-server.md` | `Explore.Blazor/**/*.cs`, `Explore.Blazor/**/*.razor` | BFF, YARP, cookie auth, SSR |
| `blazor-client.md` | `Explore.Blazor.Client/**/*.cs`, `Explore.Blazor.Client/**/*.razor`, `Explore.Blazor.Client/**/*.razor.css` | MudBlazor v9, BEM, CSS isolation, HAL gating |
| `application-layer.md` | `Explore.Application/**/*.cs` | CQRS, MediatR, handler boundaries |
| `api-controllers.md` | `Explore.API/Controllers/**/*.cs` | route contracts and controller authoring |
| `api-hateoas.md` | `Explore.API/Hateoas/**/*.cs` | HAL policies, route-name alignment, affordances |
| `domain.md` | `Explore.Domain/**/*.cs` | entity modeling and invariants |
| `efcore-persistence.md` | `Explore.Persistence/**/*.cs`, except migrations | EF Core configs, repositories, filters |
| `efcore-migrations.md` | `Explore.Persistence/Migrations/**/*.cs` | migration discipline and seed sync |
| `tests.md` | `**/*Tests/*.cs`, `**/*UnitTests/*.cs`, `**/*IntegrationTests/*.cs`, `**/*.Tests/*.cs` | test execution and suite hygiene |

## Layout Notes

- This repo's actual code roots are `Explore.*`, not `Event.*`; globs here follow the real layout.
- `api-hateoas.md` uses `Explore.API/Hateoas/**/*.cs` because link policies live there and there are no `*HalLink*.cs` files today.
