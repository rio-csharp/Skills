#!/usr/bin/env python3
"""Package an Agent Skill folder as a .skill zip archive."""

from __future__ import annotations

import argparse
import fnmatch
import sys
import zipfile
from pathlib import Path

sys.dont_write_bytecode = True

try:
    from scripts.quick_validate import validate_skill
except ModuleNotFoundError:
    from quick_validate import validate_skill


EXCLUDE_DIRS = {
    ".git",
    ".hg",
    ".svn",
    "__pycache__",
    "node_modules",
    ".venv",
    "venv",
    "dist",
    "build",
    "evals",
    "tmp",
    "temp",
}
EXCLUDE_FILES = {".DS_Store", "Thumbs.db"}
EXCLUDE_GLOBS = {"*.pyc", "*.pyo", "*.log", "*.tmp", "*.skill"}


def should_exclude(rel_path: Path) -> bool:
    if any(part in EXCLUDE_DIRS for part in rel_path.parts):
        return True
    if rel_path.name in EXCLUDE_FILES:
        return True
    return any(fnmatch.fnmatch(rel_path.name, pattern) for pattern in EXCLUDE_GLOBS)


def package_skill(skill_path: str | Path, output_dir: str | Path | None = None) -> Path:
    source = Path(skill_path).expanduser().resolve()
    result = validate_skill(source)
    if not result.ok:
        raise ValueError("validation failed: " + "; ".join(result.errors))

    destination_dir = Path(output_dir).expanduser().resolve() if output_dir else Path.cwd().resolve()
    destination_dir.mkdir(parents=True, exist_ok=True)
    archive_path = destination_dir / f"{source.name}.skill"

    with zipfile.ZipFile(archive_path, "w", zipfile.ZIP_DEFLATED) as archive:
        for file_path in sorted(source.rglob("*")):
            if not file_path.is_file():
                continue
            rel_to_skill = file_path.relative_to(source)
            if should_exclude(rel_to_skill):
                continue
            archive_name = str(Path(source.name) / rel_to_skill).replace("\\", "/")
            archive.write(file_path, archive_name)

    return archive_path


def main() -> int:
    parser = argparse.ArgumentParser(description="Package an Agent Skill folder as a .skill archive.")
    parser.add_argument("skill_path", type=Path)
    parser.add_argument("output_dir", type=Path, nargs="?")
    args = parser.parse_args()

    try:
        archive = package_skill(args.skill_path, args.output_dir)
    except Exception as exc:
        print(f"Error: {exc}")
        return 1

    print(f"Packaged skill: {archive}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
