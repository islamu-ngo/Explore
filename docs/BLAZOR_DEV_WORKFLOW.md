<!-- ABOUTME: How-to guide for the Blazor UI development and visual-verification cycle (stop → build → run → wait → inspect). -->
<!-- ABOUTME: Extracted from AGENTS.md to keep the root bootloader lean while preserving the full workflow for UI work. -->

# Blazor UI Development Workflow

> **Category:** How-to (Diataxis)
> **Audience:** AI agents and developers modifying Blazor components, CSS, or MudBlazor layouts that require visual verification.
> **Last Updated:** 2026-04-23

When making Blazor UI / CSS changes that need visual verification, follow this **stop → build → run → wait → inspect** cycle every time. Skipping any step produces stale DLLs, locked processes, or a blank page.

---

## 1. The Five-Step Cycle

```bash
# 1. Stop all running dotnet processes (DLLs are locked while running)
Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2

# 2. Build
dotnet build --configuration Release --verbosity quiet

# 3. Start the Aspire AppHost (launches all child services)
Start-Process -FilePath "dotnet" -ArgumentList "run","--project","src/Explore.AppHost/Explore.AppHost.csproj" -WorkingDirectory "C:\ISLAMU\GitHub\Event" -WindowStyle Hidden

# 4. Wait for the site to be ready (~15-20 seconds)
Start-Sleep -Seconds 20
Invoke-WebRequest -Uri "https://localhost:7177" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 10

# 5. Inspect (see §2 below)
```

---

## 2. Visual Inspection

After the site is up, inspect the affected page in a browser at the relevant desktop and mobile widths. Verify focus behavior, keyboard navigation, RTL, dark mode, responsive layout, and reduced motion when those surfaces changed.

---

## 3. Key Notes

| Concern | What to Know |
|---|---|
| App URL | `https://localhost:7177` |
| Process management | Aspire AppHost spawns child `dotnet` processes — stop ALL `dotnet` processes before rebuild, not just the AppHost |
| Enhanced navigation | Blazor enhanced navigation interferes with `page.goto()` — use `page.reload()` instead |
| Scoped CSS | Changes to `*.razor.css` require a full rebuild (not hot-reload) |
| MudBlazor version | v9 — match existing component API; see [`blazor-ui-conventions`](../.agents/skills/blazor-ui-conventions/SKILL.md) |
| CSS isolation + BEM | See [`blazor-css-isolation`](../.agents/skills/blazor-css-isolation/SKILL.md) |

---

## 4. Cross-References

- Component / render-mode conventions → [`docs/BLAZOR.md`](BLAZOR.md)
- BFF auth / YARP / token forwarding → [`docs/SECURITY-MODEL.md`](SECURITY.md), [`blazor-bff-patterns`](../.agents/skills/blazor-bff-patterns/SKILL.md)
- UI conventions (MudBlazor, BEM, theming) → [`blazor-ui-conventions`](../.agents/skills/blazor-ui-conventions/SKILL.md)
- Accessibility requirements → [`docs/ACCESSIBILITY.md`](ACCESSIBILITY.md)
- Design tokens, CSS layers, wrappers → [`design-system`](../.agents/skills/design-system/SKILL.md)
