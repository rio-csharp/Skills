# Authoring Best Practices

Use this guide when creating, rewriting, or auditing a skill. The goal is a skill that triggers at the right time, loads only the context it needs, and gives the agent enough procedural knowledge to perform reliably.

## Quality Bar

A strong skill is:

- Discoverable from its frontmatter description.
- Useful at the entrypoint and rich only when needed.
- Specific about workflows, resources, validation, and side effects.
- Safe to inspect before use.
- Tested on realistic tasks, including near-misses.
- Free of surprise files, hidden network calls, and irrelevant docs.

## Skill Anatomy

Use this shape unless the target platform requires something else:

```text
skill-name/
├── SKILL.md
├── scripts/
├── references/
└── assets/
```

`SKILL.md` is required. The other folders are optional and should exist only when useful.

## Progressive Disclosure

Design for three levels:

- Frontmatter metadata: always available for trigger decisions.
- `SKILL.md` body: loaded after the skill triggers.
- Bundled resources: loaded or executed only when the task needs them.

Keep the body as a navigation map and short playbook, but do not make it so thin that an agent cannot operate the skill. `SKILL.md` should contain the commands, arguments, examples, safety rules, and validation steps needed for the common path.

Put detailed specs, schemas, external API notes, uncommon edge cases, and large example sets in `references/`.

If a skill wraps an external tool or API with a bundled helper script, distinguish the helper interface from the external API. A reference named `api.md` should say whose API it documents. Do not point agents to a vendor API reference when they need the bundled helper's command reference.

Prefer one-hop references. If `SKILL.md` links to `references/aws.md`, that file should contain what the agent needs for AWS rather than pointing through several more files.

## Frontmatter

Use this minimum form:

```yaml
---
name: my-skill
description: Do a specific kind of work. Use when the user asks for concrete trigger phrases, file types, domains, or workflows.
---
```

The description is the main trigger surface. Put trigger criteria there instead of hiding them in the body.

Description checklist:

- Say what the skill does.
- Say when to use it.
- Include user-facing synonyms and file types.
- Include important negative boundaries if the skill is easy to over-trigger.
- Stay under 1024 characters.
- Avoid XML tags and broad claims like "use for all coding tasks."

Name checklist:

- Use lowercase letters, numbers, and hyphens.
- Keep it short and action-oriented.
- Match the folder name.
- Avoid reserved/provider names unless the skill is genuinely owned by that provider.

## Body Structure

Good `SKILL.md` bodies usually include:

- Start-here routing.
- A compact workflow.
- The bundled commands or exact procedure needed for normal use.
- Resource map with "read/run this when..." instructions.
- Validation instructions.
- Safety or permission notes when relevant.
- Output expectations only when the task needs a consistent format.

For weaker-model authors, do not leave body structure open-ended. Give them a fixed section order and reject extra sections unless they carry their weight.

Avoid:

- A long "what this skill is" essay.
- Generic model advice.
- Duplicate content from references unless duplication is necessary for safe common-path execution.
- Install or changelog docs.
- Hidden assumptions about local paths, secrets, or live systems.

## Resource Design

Use `scripts/` when the operation should be deterministic, repeated, or hard to perform by hand. For this skill creator, default to C# file-based apps (`.cs` single-file programs run with `dotnet run --file ...`) unless the user explicitly wants another runtime or a platform constraint makes that awkward. Scripts should accept arguments, fail clearly, and be runnable without editing source code.

Use `references/` for information the agent may need to read: schemas, policies, detailed workflows, examples, APIs, and variant-specific guidance.

Use `assets/` for output materials: templates, starter projects, fonts, icons, source documents, or sample files that the agent copies or modifies.

## Degrees Of Freedom

Match instruction strictness to task risk:

- High freedom: creative writing, visual direction, strategy, subjective review.
- Medium freedom: workflows with preferred patterns but contextual decisions.
- Low freedom: file transforms, regulated formats, fragile command sequences, data migration, security-sensitive actions.

For low-freedom work, prefer scripts, templates, and explicit validation over prose.

## Examples

Include examples when they teach a pattern or define an output contract. Keep them realistic and compact.

```markdown
## Output Format

Use this shape for incident summaries:

- Summary: one sentence.
- Impact: affected users, systems, and time range.
- Root cause: what failed and why.
- Follow-up: owners and deadlines.
```

Do not include many near-identical examples. Put larger example sets in a reference file.

## Safety

Before adding tools, commands, or instructions with side effects, read `references/security-review.md`.

Never create skills that hide their purpose, exfiltrate data, bypass permissions, or make irreversible changes without clear user intent.

## Final Checklist

- `SKILL.md` has valid frontmatter.
- The description can trigger the skill without reading the body.
- `SKILL.md` contains enough command/procedure detail for the primary workflow.
- `SKILL.md` links to deeper resources with clear scope labels.
- Every referenced file exists.
- No unnecessary docs or generated clutter are included.
- Scripts have been smoke-tested.
- Validation passes.
- At least one realistic prompt has been tried or written down for the user.

## Anti-Patterns To Reject

Reject or rewrite drafts that show these patterns:

- The body contains the real trigger logic while `description` stays generic.
- `SKILL.md` hides essential command usage in a reference file.
- Reference names blur different layers, such as a helper CLI reference vs. an external API reference.
- `SKILL.md` duplicates large external specs instead of routing to them.
- The folder name and frontmatter `name` do not match.
- Scripts use a random runtime with no justification.
- The only test is a large integration script that mutates real data.
- The skill reads like a transcript of how it was created instead of a guide for future agents.
