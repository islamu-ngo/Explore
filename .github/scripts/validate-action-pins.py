# ABOUTME: Validates that external GitHub Actions references are immutable full SHA pins.
# ABOUTME: Allows local reusable workflows while requiring same-line version comments for external actions.

from __future__ import annotations

import re
import sys
from pathlib import Path


USES_PATTERN = re.compile(r"^(?P<indent>\s*)uses:\s*(?P<value>[^\s#]+)(?P<suffix>.*)$")
FULL_SHA_PATTERN = re.compile(r"^[0-9a-fA-F]{40}$")


def iter_workflow_files(root: Path) -> list[Path]:
    return sorted(root.glob("*.yml")) + sorted(root.glob("*.yaml"))


def normalize_uses_value(raw_value: str) -> str:
    return raw_value.strip().strip('"').strip("'")


def validate_file(path: Path) -> list[str]:
    failures: list[str] = []

    for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        match = USES_PATTERN.match(line)
        if match is None:
            continue

        uses_value = normalize_uses_value(match.group("value"))
        suffix = match.group("suffix")

        if uses_value.startswith("./"):
            continue

        if "@" not in uses_value:
            failures.append(f"{path}:{line_number}: external action '{uses_value}' is missing an @ref")
            continue

        _, action_ref = uses_value.rsplit("@", 1)
        if not FULL_SHA_PATTERN.fullmatch(action_ref):
            failures.append(f"{path}:{line_number}: external action '{uses_value}' is not pinned to a full 40-character SHA")
            continue

        if not re.search(r"#\s*v\d", suffix, flags=re.IGNORECASE):
            failures.append(f"{path}:{line_number}: external action '{uses_value}' is missing a same-line version comment such as '# v4'")

    return failures


def main() -> int:
    workflow_root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(".github/workflows")
    failures: list[str] = []

    for workflow_file in iter_workflow_files(workflow_root):
        failures.extend(validate_file(workflow_file))

    if failures:
        print("GitHub Actions pin validation failed:")
        for failure in failures:
            print(f"- {failure}")
        return 1

    print("All external GitHub Actions references are pinned to full SHAs with version comments.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
