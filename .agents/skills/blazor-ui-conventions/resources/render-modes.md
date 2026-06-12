ABOUTME: Minimal guidance for Blazor render modes used in this project.
ABOUTME: Captures defaults, governance rules, and prerendering caveats.

# Render Modes (Lean)

## Defaults
- Use `InteractiveAuto` for most interactive pages.
- Use `InteractiveServer` only when server-only dependencies are required.
- Use Static SSR for purely static content.

## Governance Rules (Project-Specific)
- Runtime render policy resolves per route group.
- Public SEO: `InteractiveAuto` with prerender.
- Other routes: `InteractiveAuto` without prerender.
- Onboarding routes must force `InteractiveServer`.
- Governance key `routing.render_policy.onboarding.disallow_interactive_server` must remain enabled.

## Prerendering Caveat
- `OnInitialized{Async}` can run twice; avoid side effects or guard them.

## Related
- [component-design.md](component-design.md)
- [state-management.md](state-management.md)
