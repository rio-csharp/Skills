#!/usr/bin/env python3
"""Validate an Agent Skill folder for common authoring issues."""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path

sys.dont_write_bytecode = True

NAME_RE = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
LINK_RE = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
RESOURCE_DIRS = {"scripts", "references", "assets"}
PACKAGE_CLUTTER = {
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
RESERVED_NAMES = {"anthropic", "claude"}
ALLOWED_FRONTMATTER = {
    "name",
    "description",
    "allowed-tools",
    "disable-model-invocation",
    "user-invocable",
    "context",
    "agent",
    "paths",
    "license",
    "metadata",
    "compatibility",
}


@dataclass
class ValidationResult:
    errors: list[str]
    warnings: list[str]

    @property
    def ok(self) -> bool:
        return not self.errors


def parse_simple_yaml(raw_yaml: str) -> dict[str, object]:
    """Parse the simple YAML shape used by skill frontmatter without dependencies."""
    data: dict[str, object] = {}
    lines = raw_yaml.splitlines()
    i = 0

    while i < len(lines):
        line = lines[i]
        i += 1
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if line.startswith((" ", "\t")):
            raise ValueError(f"unsupported indented frontmatter line: {line!r}")
        if ":" not in line:
            raise ValueError(f"frontmatter line must contain ':': {line!r}")

        key, value = line.split(":", 1)
        key = key.strip()
        value = value.strip()
        if not key:
            raise ValueError("frontmatter contains an empty key")

        if value in {">", "|", ">-", "|-"}:
            block_lines: list[str] = []
            while i < len(lines):
                continuation = lines[i]
                if not continuation.startswith((" ", "\t")) and continuation.strip():
                    break
                i += 1
                block_lines.append(continuation.strip())
            data[key] = "\n".join(block_lines) if value.startswith("|") else " ".join(block_lines)
            continue

        if not value and i < len(lines) and lines[i].startswith((" ", "\t")):
            nested: dict[str, str] = {}
            nested_list: list[str] = []
            while i < len(lines) and (lines[i].startswith((" ", "\t")) or not lines[i].strip()):
                continuation = lines[i].strip()
                i += 1
                if not continuation:
                    continue
                if continuation.startswith("- "):
                    nested_list.append(continuation[2:].strip().strip("\"'"))
                    continue
                if ":" in continuation:
                    nested_key, nested_value = continuation.split(":", 1)
                    nested[nested_key.strip()] = nested_value.strip().strip("\"'")
            data[key] = nested if nested else nested_list
            continue

        if value.startswith("[") and value.endswith("]"):
            items = value[1:-1].strip()
            data[key] = [item.strip().strip("\"'") for item in items.split(",") if item.strip()]
            continue

        if not value:
            data[key] = ""
        else:
            data[key] = value.strip("\"'")

    return data


def read_frontmatter(skill_md: Path) -> tuple[dict[str, object], str]:
    content = skill_md.read_text(encoding="utf-8")
    if not content.startswith("---"):
        raise ValueError("SKILL.md must start with YAML frontmatter delimited by ---")

    match = re.match(r"^---\r?\n(.*?)\r?\n---\r?\n?", content, re.DOTALL)
    if not match:
        raise ValueError("SKILL.md is missing closing frontmatter delimiter")

    raw_yaml = match.group(1).strip("\n")
    body = content[match.end():].lstrip("\r\n")
    data = parse_simple_yaml(raw_yaml)
    return data, body


def local_markdown_links(body: str) -> list[str]:
    links: list[str] = []
    for target in LINK_RE.findall(body):
        if "://" in target or target.startswith("#") or target.startswith("mailto:"):
            continue
        clean = target.split("#", 1)[0].strip()
        if clean:
            links.append(clean)
    return links


def validate_skill(skill_path: str | Path) -> ValidationResult:
    path = Path(skill_path).expanduser().resolve()
    errors: list[str] = []
    warnings: list[str] = []

    if not path.exists():
        return ValidationResult([f"skill folder does not exist: {path}"], [])
    if not path.is_dir():
        return ValidationResult([f"skill path is not a directory: {path}"], [])

    skill_md = path / "SKILL.md"
    if not skill_md.exists():
        return ValidationResult(["missing SKILL.md"], [])

    try:
        frontmatter, body = read_frontmatter(skill_md)
    except Exception as exc:
        return ValidationResult([str(exc)], [])

    unexpected = sorted(set(frontmatter) - ALLOWED_FRONTMATTER)
    if unexpected:
        warnings.append(
            "unexpected frontmatter field(s): "
            + ", ".join(unexpected)
            + "; confirm the target platform supports them"
        )

    name = frontmatter.get("name")
    description = frontmatter.get("description")

    if not isinstance(name, str) or not name.strip():
        errors.append("frontmatter.name must be a non-empty string")
    else:
        name = name.strip()
        if not NAME_RE.fullmatch(name):
            errors.append("frontmatter.name must use lowercase letters, numbers, and single hyphens")
        if len(name) > 64:
            errors.append("frontmatter.name must be 64 characters or fewer")
        reserved_hits = sorted(word for word in RESERVED_NAMES if word in name)
        if reserved_hits:
            errors.append(
                "frontmatter.name must not contain reserved word(s): " + ", ".join(reserved_hits)
            )
        if name != path.name:
            warnings.append(f"frontmatter.name '{name}' does not match folder name '{path.name}'")

    if not isinstance(description, str) or not description.strip():
        errors.append("frontmatter.description must be a non-empty string")
    else:
        description = " ".join(description.split())
        if len(description) > 1024:
            errors.append("frontmatter.description must be 1024 characters or fewer")
        if "<" in description or ">" in description:
            errors.append("frontmatter.description must not contain XML/HTML angle brackets")
        if len(description) < 50:
            warnings.append("description is very short; include concrete trigger contexts")
        lowered = description.lower()
        if "use when" not in lowered and "use this skill" not in lowered:
            warnings.append("description should explicitly say when to use the skill")
        if any(phrase in lowered for phrase in ("anything", "everything", "all tasks", "all requests")):
            warnings.append("description may be too broad; add boundaries or concrete triggers")

    if not body.strip():
        errors.append("SKILL.md body is empty")
    if "TODO" in body:
        warnings.append("SKILL.md still contains TODO placeholders")

    body_lines = body.splitlines()
    if len(body_lines) > 500:
        warnings.append("SKILL.md body is over 500 lines; consider moving details to references/")

    for link in local_markdown_links(body):
        target = (path / link).resolve()
        try:
            target.relative_to(path)
        except ValueError:
            warnings.append(f"local link points outside the skill folder: {link}")
            continue
        if not target.exists():
            errors.append(f"local link target does not exist: {link}")

    for child in path.iterdir():
        if child.is_dir() and child.name in RESOURCE_DIRS:
            visible_files = [item for item in child.rglob("*") if item.is_file() and item.name != ".gitkeep"]
            if not visible_files:
                warnings.append(f"{child.name}/ exists but has no useful files")
        elif child.is_dir() and child.name in PACKAGE_CLUTTER:
            warnings.append(f"package clutter directory found: {child.name}/")

    for clutter in PACKAGE_CLUTTER:
        if any(part == clutter for file_path in path.rglob("*") for part in file_path.relative_to(path).parts):
            warnings.append(f"package clutter appears under skill folder: {clutter}")

    return ValidationResult(errors, sorted(set(warnings)))


def print_result(result: ValidationResult) -> None:
    if result.ok:
        print("Skill validation passed.")
    else:
        print("Skill validation failed.")

    for error in result.errors:
        print(f"ERROR: {error}")
    for warning in result.warnings:
        print(f"WARNING: {warning}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate an Agent Skill folder.")
    parser.add_argument("skill_path", type=Path)
    args = parser.parse_args()

    result = validate_skill(args.skill_path)
    print_result(result)
    return 0 if result.ok else 1


if __name__ == "__main__":
    sys.exit(main())
