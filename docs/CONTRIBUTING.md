# Contribution Workflow

## Branch Strategy

```
main                    # Default branch
├── feature/xxx         # New features
├── bugfix/xxx          # Bug fixes
└── refactor/xxx        # Code improvements
```

## Commit Convention

```
type(scope): subject

body (optional)

footer (optional)
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

**Examples**:
```
feat(events): add prayer-relative scheduling
fix(federation): correct HTTP signature validation
docs(api): update endpoint documentation
```

## Pull Request Process

1. Create a branch from `main`
2. Implement changes with tests
3. Ensure all tests pass: `dotnet test`
4. Ensure code formatting: `dotnet format`
5. Create PR with description and linked issue
6. Request review from maintainers
7. Address feedback
8. Squash and merge when approved

## Issue Templates

- **Bug Report**: Describe bug, steps to reproduce, expected behavior
- **Feature Request**: Describe need, proposed solution, alternatives
- **Task**: Technical work without user-facing change
