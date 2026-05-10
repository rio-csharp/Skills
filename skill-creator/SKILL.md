---
name: skill-creator
description: Create, audit, refactor, evaluate, optimize, and package Agent Skills. Use when users want to create a new skill, update or modularize an existing skill, improve skill triggering, add scripts/references/assets, run skill evals, compare versions, review security risks, or prepare a .skill package for sharing.
---

# Skill Creator

Create skills as small, discoverable, tested capability packages. Treat `SKILL.md` as the entrypoint and navigation map; move deep details into referenced files that are loaded only when needed.

## Start Here

First determine the user's intent and where they are in the lifecycle:

- New skill: clarify purpose, triggers, target platform, reusable resources, and validation plan.
- Existing skill update: inspect the current folder, preserve its name unless the user asks otherwise, and identify whether the issue is triggering, workflow quality, resource design, safety, or packaging.
- Eval, benchmark, or smoke-test request: read [references/validation-and-testing.md](references/validation-and-testing.md).
- Trigger/description optimization: read [references/triggering.md](references/triggering.md).
- Platform-specific packaging or installation: read [references/platform-notes.md](references/platform-notes.md).
- Security, enterprise, or untrusted-skill review: read [references/security-review.md](references/security-review.md).

When the task is ordinary skill creation or editing, read [references/workflow.md](references/workflow.md) and [references/authoring-best-practices.md](references/authoring-best-practices.md) before making substantial changes.

## Core Workflow

1. Capture concrete use cases.
2. Design the skill's loading shape.
3. Draft or revise `SKILL.md`.
4. Add only useful bundled resources.
5. Validate locally.
6. Test on realistic prompts and iterate.
7. Package or hand off when the user is satisfied.

Skip steps only when they are clearly irrelevant. For example, a tiny style skill may not need scripts, while a file transformation skill should almost always have executable validation.

## 1. Capture Concrete Use Cases

Extract what you can from the current conversation before asking questions. Look for tools used, sequence of steps, corrections the user made, input files, output formats, and repeated instructions.

Clarify only the missing pieces that affect the design:

- What should the skill enable the agent to do?
- What real user prompts should trigger it?
- What prompts are adjacent but should not trigger it?
- What inputs, dependencies, secrets, or external systems are involved?
- What does a good output look like?
- Can success be checked automatically, or does it need human review?

Prefer 2-4 realistic examples over abstract capability lists. If the user is non-technical, explain terms like "eval" or "assertion" briefly before using them.

## 2. Design Progressive Disclosure

Design for three loading levels:

- Metadata: `name` and `description`, always visible to the agent.
- `SKILL.md`: short procedural instructions loaded when the skill triggers.
- Bundled resources: detailed docs, scripts, templates, and examples loaded or executed only as needed.

Keep `SKILL.md` focused on routing, workflow, constraints, and resource pointers. If the body approaches 500 lines, split details into one-hop reference files and link them from `SKILL.md` with clear "read this when..." guidance.

Use the right resource type:

- `scripts/`: deterministic or repeated operations, validators, converters, generators, benchmarks.
- `references/`: detailed policies, schemas, API notes, examples, playbooks, edge cases.
- `assets/`: templates, images, fonts, boilerplate projects, files to copy or transform into outputs.

Do not add decorative documentation such as `README.md`, changelogs, install notes, or process logs unless the target platform specifically needs them.

## 3. Write Frontmatter

Use YAML frontmatter at the top of `SKILL.md`.

Required cross-platform fields:

```yaml
---
name: example-skill
description: Do X for Y. Use when the user asks for A, B, or C, especially when Z.
---
```

Rules:

- Match `name` to the skill folder where possible.
- Use lowercase letters, numbers, and hyphens only.
- Keep `name` under 64 characters.
- Keep `description` non-empty and under 1024 characters.
- Do not include XML tags in `name` or `description`.
- Avoid reserved provider names such as `anthropic` or `claude` in new skill names.
- Put the main trigger guidance in `description`, not in a "when to use" body section.

Make descriptions specific and trigger-aware: include what the skill does, when to use it, key file types, domains, task names, and near-synonyms users might say. Do not make the description so broad that it steals unrelated tasks.

Platform-specific fields such as `allowed-tools`, `disable-model-invocation`, `user-invocable`, `model`, `effort`, `context`, `agent`, `paths`, or dynamic context belong only when the target runtime supports them. Check [references/platform-notes.md](references/platform-notes.md) before adding them.

## 4. Write The Body

Write for another agent that is already capable. Give the minimum non-obvious guidance needed to do the job well.

Prefer:

- Imperative, action-oriented instructions.
- Short workflows with decision points.
- Concrete examples where output shape matters.
- Resource pointers that say when to read or run a file.
- Rationale only where it changes behavior.

Avoid:

- Long explanations of general knowledge.
- Overfit instructions that only solve one eval prompt.
- Repeating the same details in `SKILL.md` and references.
- Excessive `MUST`/`NEVER` language where a short rationale would guide better.
- Hidden side effects or surprising actions.

For fragile tasks, reduce degrees of freedom with scripts, fixed templates, or explicit checklists. For creative or context-dependent tasks, give principles and examples rather than rigid recipes.

## 5. Add Resources

Start with resources that remove repeated work or reduce mistakes.

Scripts should:

- Accept paths and options as arguments instead of hardcoding local paths.
- Print clear success and failure messages.
- Return non-zero exit codes on failure.
- Be tested by actually running representative commands.
- Be referenced from `SKILL.md` so the agent knows when to use them.

References should:

- Be one hop from `SKILL.md`; avoid chains where references point to more hidden references.
- Have clear filenames by topic or variant.
- Include a table of contents when long.
- Contain detailed facts and examples that would bloat `SKILL.md`.

Assets should:

- Be files the agent copies, modifies, or uses in final outputs.
- Stay out of context unless inspection is necessary.

## 6. Validate

Run the local validator after any meaningful edit:

```bash
python <this-skill>/scripts/quick_validate.py <path-to-skill-folder>
```

Fix validation failures before packaging. Treat warnings as design prompts: they may be acceptable, but should be intentional.

For scripts, run representative script commands. For reference-heavy skills, verify every linked file exists and can be reached from `SKILL.md`.

## 7. Test And Iterate

Use realistic prompts, not toy prompts. Include near-misses when testing triggering.

For simple changes, run a small manual smoke test. For high-value, fragile, or reusable skills, use the validation guidance in [references/validation-and-testing.md](references/validation-and-testing.md).

When improving from feedback:

- Generalize from the complaint rather than patching only the exact test.
- Inspect transcripts or intermediate artifacts when available.
- Add scripts when multiple test runs independently reinvent the same code.
- Remove instructions that increase token cost without changing behavior.
- Re-run the relevant tests after changes.

## Package

Package only after validation passes and the user is ready:

```bash
python -m scripts.package_skill <path-to-skill-folder> [output-directory]
```

Run this from the `skill-creator` directory or make sure Python can import its `scripts` package.

## Reference Files

- [references/authoring-best-practices.md](references/authoring-best-practices.md): anatomy, progressive disclosure, writing style, resource design, and quality checklist.
- [references/workflow.md](references/workflow.md): end-to-end creation, scaffold, draft, validate, test, iterate, and package workflow.
- [references/validation-and-testing.md](references/validation-and-testing.md): static validation, script checks, smoke tests, forward tests, and comparative tests.
- [references/triggering.md](references/triggering.md): trigger descriptions, near-miss prompts, and description review.
- [references/platform-notes.md](references/platform-notes.md): Claude Code, Claude.ai, API/container, and headless environment differences.
- [references/security-review.md](references/security-review.md): trust, tool permissions, side effects, secrets, enterprise review, and packaging safety.
