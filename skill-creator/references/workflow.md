# Skill Creation Workflow

Use this guide as the default path for creating or rewriting a skill.

## 1. Understand The Job

Extract context from the current conversation first. Ask only for missing information that changes the design.

Useful questions:

- What should the skill help the agent do?
- What are 2-4 realistic user prompts that should trigger it?
- What similar prompts should not trigger it?
- What files, tools, APIs, or environments are involved?
- What should the final output look like?
- How will we know it worked?

If the user says "turn this into a skill," mine the transcript for workflow steps, corrections, commands, file types, and expected outputs.

## 2. Choose The Resource Shape

For each example prompt, imagine doing the task from scratch and identify repeated or fragile parts.

- Repeated code or command sequence: create a script.
- Detailed knowledge, schema, policy, or API notes: create a reference.
- Reusable starting point or output material: create an asset.
- Short decision logic or core workflow: keep it in `SKILL.md`.

Do not create a folder just because the template allows it.

For weaker models, make the choice explicit:

- Need deterministic behavior or repeated local commands: create a C# file-based script.
- Need large factual reference material: create a reference file.
- Need output materials to copy or modify: create an asset.
- Need trigger logic or routing: keep it in `SKILL.md`.

## 3. Scaffold

Use the local scaffold script:

```bash
dotnet run --file <this-skill>/scripts/scaffold.cs -- <skill-name> --path <output-directory>
```

Optional folders:

```bash
dotnet run --file <this-skill>/scripts/scaffold.cs -- <skill-name> --path <output-directory> --resources scripts,references,assets
```

If creating a skill for the current Codex environment and the user did not specify a location, use `$CODEX_HOME/skills` when set, otherwise `~/.codex/skills`.

## 4. Draft

Write frontmatter first. The description should answer: "Would an agent looking only at this line know when to open the skill?"

If the authoring model is weak, force this sequence:

1. Write `name`.
2. Write `description` from the fill-in formula.
3. Write the body from the default skeleton.
4. Only then add references or scripts.

Then write the body:

- Start with the most common path.
- Include decision points only where needed.
- Link references with precise conditions.
- Say how to validate outputs.
- Keep platform-specific notes out unless they apply.

## 5. Build Resources

Create only resources that directly support the skill.

For scripts:

- Default to C# file-based apps so the created skill stays consistent with this toolkit.
- Add argument parsing.
- Avoid hardcoded absolute paths.
- Print clear errors.
- Add examples in `SKILL.md`.
- Run at least one representative command.

For references:

- Put a short title at the top.
- Add a table of contents if long.
- Use topic-specific files rather than one giant reference.
- Remove duplicate content from `SKILL.md`.

For assets:

- Keep original filenames clear.
- Note in `SKILL.md` when to copy or modify them.

## 6. Validate

Run:

```bash
dotnet run --file <this-skill>/scripts/validate.cs -- <path-to-skill-folder>
```

Fix errors. Review warnings and either address them or keep them intentionally.

## 7. Test

For a small skill, run one realistic manual task.

For weaker-model-authored skills, require this minimum test stack:

- One smoke test that exercises the most important path.
- One near-miss trigger example.
- One explicit check for the highest-risk pitfall in the skill.

For a complex or reusable skill, create a tiny eval set:

- Two prompts that should trigger.
- One near-miss that should not trigger.
- One edge case with missing or messy input.

Prefer evaluating real outputs over reading the skill and guessing. If subagents are available and the user has asked for agent-based validation, pass only the skill path and task prompt, not your intended answer.

## 8. Iterate

When a test fails:

- Fix the underlying workflow, not only that exact example.
- Add scripts when the model repeatedly reinvents the same helper.
- Split references when irrelevant details are loaded too often.
- Tighten the description if triggering is wrong.
- Remove instructions that cause wasted work.

Stop when the skill is clear, validated, and good enough for the user's intended use.

If the draft still feels vague, do not ask the weaker model to "polish it." Replace vague sections with concrete templates and rerun validation.

## 9. Package

Package with:

```bash
dotnet run --file <this-skill>/scripts/package.cs -- <path-to-skill-folder> [output-directory]
```

Package only the skill folder and useful resources. Do not include eval workspaces, logs, caches, or generated reports.
