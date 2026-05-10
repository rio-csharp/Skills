#!/usr/bin/env python3
"""Create a minimal Agent Skill folder."""

from __future__ import annotations

import argparse
import os
import re
from pathlib import Path


RESOURCE_NAMES = {"scripts", "references", "assets"}
NAME_RE = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")


def normalize_name(value: str) -> str:
    name = value.strip().lower()
    name = re.sub(r"[^a-z0-9]+", "-", name)
    name = re.sub(r"-+", "-", name).strip("-")
    return name


def default_output_path() -> Path:
    codex_home = os.environ.get("CODEX_HOME")
    if codex_home:
        return Path(codex_home) / "skills"
    return Path.home() / ".codex" / "skills"


def parse_resources(value: str) -> list[str]:
    if not value:
        return []
    resources = [part.strip() for part in value.split(",") if part.strip()]
    unknown = sorted(set(resources) - RESOURCE_NAMES)
    if unknown:
        raise argparse.ArgumentTypeError(
            f"unknown resource folder(s): {', '.join(unknown)}; expected scripts,references,assets"
        )
    return resources


def create_skill(name: str, output_path: Path, resources: list[str], force: bool) -> Path:
    normalized = normalize_name(name)
    if not normalized or not NAME_RE.fullmatch(normalized):
        raise ValueError("skill name must contain lowercase letters, numbers, and hyphens")
    if len(normalized) > 64:
        raise ValueError("skill name must be 64 characters or fewer")

    skill_path = output_path / normalized
    if skill_path.exists() and not force:
        raise FileExistsError(f"{skill_path} already exists; pass --force to add missing template files")

    skill_path.mkdir(parents=True, exist_ok=True)

    skill_md = skill_path / "SKILL.md"
    if force or not skill_md.exists():
        skill_md.write_text(
            f"""---
name: {normalized}
description: TODO: Describe what this skill does and the concrete situations that should trigger it.
---

# {normalized.replace("-", " ").title()}

Use this skill when TODO.

## Workflow

1. TODO: Capture inputs and constraints.
2. TODO: Run or read the relevant resources.
3. TODO: Produce the expected output.
4. TODO: Validate the result.

## Resources

TODO: List bundled resources and when to use them.
""",
            encoding="utf-8",
        )

    for resource in resources:
        folder = skill_path / resource
        folder.mkdir(exist_ok=True)
        keep = folder / ".gitkeep"
        if force or not keep.exists():
            keep.write_text("", encoding="utf-8")

    return skill_path


def main() -> int:
    parser = argparse.ArgumentParser(description="Create a minimal Agent Skill folder.")
    parser.add_argument("name", nargs="+", help="Skill name or title. It will be normalized to kebab-case.")
    parser.add_argument(
        "--path",
        type=Path,
        default=default_output_path(),
        help="Directory where the skill folder should be created. Defaults to $CODEX_HOME/skills or ~/.codex/skills.",
    )
    parser.add_argument(
        "--resources",
        type=parse_resources,
        default=[],
        help="Comma-separated resource folders to create: scripts,references,assets.",
    )
    parser.add_argument("--force", action="store_true", help="Create missing template files in an existing folder.")
    args = parser.parse_args()

    try:
        skill_path = create_skill(" ".join(args.name), args.path.expanduser().resolve(), args.resources, args.force)
    except Exception as exc:
        print(f"Error: {exc}")
        return 1

    print(f"Created skill at {skill_path}")
    print("Next: edit SKILL.md, replace TODOs, then run quick_validate.py.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
