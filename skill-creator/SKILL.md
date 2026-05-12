---
name: skill-creator
description: Create, audit, refactor, evaluate, optimize, and package Agent Skills. Use when users want to create a new skill, update or modularize an existing skill, improve skill triggering, add scripts/references/assets, run skill evals, compare versions, review security risks, or prepare a .skill package for sharing.
---

# Skill Creator

Create skills as discoverable, tested capability packages. `SKILL.md` is the entrypoint and minimum viable operating manual: keep enough command/procedure detail there for the common path, and move only deeper background or uncommon details into references.

Assume the skill author may be a weaker model. Prefer concrete templates, explicit checks, and runnable validation over taste-based guidance.

## Start Here

- New skill or normal rewrite: read [references/workflow.md](references/workflow.md) and [references/authoring-best-practices.md](references/authoring-best-practices.md).
- Existing skill update: inspect the current folder first; preserve the name unless the user asks otherwise; then use [references/workflow.md](references/workflow.md).
- Trigger/description work: read [references/triggering.md](references/triggering.md).
- Testing, evals, or review: read [references/validation-and-testing.md](references/validation-and-testing.md).
- Platform-specific fields or packaging target: read [references/platform-notes.md](references/platform-notes.md).
- Security, credentials, destructive commands, or shared skills: read [references/security-review.md](references/security-review.md).

## Core Workflow

1. Capture concrete use cases and near-misses.
2. Decide what belongs in `SKILL.md`, `scripts/`, `references/`, and `assets/`.
3. Draft or revise frontmatter and `SKILL.md`.
4. Add only useful bundled resources.
5. Validate locally.
6. Smoke-test on realistic prompts and iterate.
7. Package or hand off when the user is satisfied.

## Non-Negotiables

- Put trigger logic in frontmatter `description`, not only in the body.
- For skills about external tools, products, APIs, SDKs, services, or file formats, ground guidance in official documentation, primary specifications, or source code before writing reusable instructions. If no official docs exist, use the best available primary evidence such as source code, schemas, CLI help, exported examples, release notes, or direct tool behavior, and label confidence/known gaps. If useful, clone/read upstream source in a temporary working directory outside the skill folder, then summarize only stable findings into the skill.
- Keep operational essentials in `SKILL.md`: common commands, required arguments, stdin/stdout conventions, safety rules, and validation steps.
- Do not confuse bundled helper interfaces with external/vendor APIs. Label reference scope clearly.
- Default helper scripts to C# file-based apps run with `dotnet run --file ...` unless the user or platform gives a concrete reason not to.
- Require at least one light smoke test before heavier integration tests.
- Avoid decorative files such as README, changelog, install notes, logs, caches, and generated reports unless the target platform specifically needs them.

## Weak-Model Checklist

Before accepting a weaker-model draft, check:

- Can an agent choose the skill from `name` and `description` alone?
- Can an agent perform the primary workflow from `SKILL.md` without opening the wrong reference?
- Is `SKILL.md` neither a giant external API dump nor too thin to operate safely?
- For external-tool skills, does the guidance cite or summarize official docs, specs, or source-code findings instead of relying on memory or secondary posts?
- Are bundled helper commands clearly distinguished from external APIs/specs?
- If scripts exist, are they C# file-based apps by default and smoke-tested?
- Are high-risk commands, credentials, live systems, and destructive operations called out?

## Commands

Scaffold:

```bash
dotnet run --file <this-skill>/scripts/scaffold.cs -- <skill-name> --path <output-directory> --resources scripts,references,assets
```

Validate:

```bash
dotnet run --file <this-skill>/scripts/validate.cs -- <path-to-skill-folder>
```

Package:

```bash
dotnet run --file <this-skill>/scripts/package.cs -- <path-to-skill-folder> [output-directory]
```

If running from a directory that contains a `.csproj`, keep the explicit `--file` flag so .NET runs the file-based app instead of the surrounding project.

## Validation

After editing this skill, run:

```bash
dotnet run --file tests/test_scaffold.cs
dotnet run --file tests/test_validate.cs
dotnet run --file tests/test_package.cs
dotnet run --file scripts/validate.cs -- .
```

## Reference Files

- [references/authoring-best-practices.md](references/authoring-best-practices.md): anatomy, loading shape, body structure, resource design, and checklist.
- [references/workflow.md](references/workflow.md): end-to-end create/update workflow and scaffold guidance.
- [references/validation-and-testing.md](references/validation-and-testing.md): static validation, script checks, smoke tests, forward tests, and comparative tests.
- [references/triggering.md](references/triggering.md): trigger descriptions, near-miss prompts, and description review.
- [references/platform-notes.md](references/platform-notes.md): Claude Code, Claude.ai, API/container, Windows, and headless environment notes.
- [references/security-review.md](references/security-review.md): trust, permissions, side effects, secrets, packaging hygiene, and enterprise review.
- [tests](tests): single-file smoke tests for this skill's C# file-based apps.
