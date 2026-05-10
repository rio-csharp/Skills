# Validation And Testing

Use this guide when checking a skill before handoff or after substantial edits.

## Validation Layers

Use the lightest layer that gives real confidence:

- Static validation: frontmatter, naming, linked files, folder hygiene.
- Script validation: run helper scripts on representative inputs.
- Manual smoke test: follow the skill on one realistic prompt.
- Forward test: ask an independent agent to use the skill on realistic prompts.
- Comparative evaluation: compare old vs new versions when regressions matter.

## Static Validation

Run:

```bash
dotnet run --file <this-skill>/scripts/validate.cs -- <path-to-skill-folder>
```

Static validation should catch:

- Missing `SKILL.md`.
- Invalid YAML frontmatter.
- Missing or malformed `name` and `description`.
- Overlong or broad descriptions.
- Missing linked references.
- Empty resource folders.
- Common packaging clutter.

## Manual Smoke Test

Use one realistic prompt. Follow the skill as written, not from memory. Note:

- Did the description match the task?
- Did `SKILL.md` point to the right resources?
- Did any instruction feel ambiguous?
- Did the agent need knowledge that was not available?
- Did scripts run successfully?
- Was validation possible?

## Forward Testing

Use forward testing when the skill is complex, high leverage, or likely to be reused.

If subagents are available and the user has authorized agent-based testing, pass only:

- Skill path.
- User-like task prompt.
- Input files if needed.
- Output location or expected artifact type.

Do not pass your diagnosis, intended fix, hidden expected answer, or previous reasoning unless the task specifically requires it. The goal is to learn whether the skill carries enough context on its own.

## Comparative Testing

Use old-vs-new comparison for existing skills when changes might regress behavior.

Compare:

- Output correctness.
- Time and tool usage.
- Whether the new skill loads fewer irrelevant details.
- Whether the new description triggers more accurately.
- Whether side effects are clearer and safer.

## Test Prompt Design

Use realistic prompts:

- Include messy filenames, partial context, or user shorthand.
- Cover the most common happy path.
- Include at least one edge case.
- Include near-misses for trigger testing.
- Avoid prompts that reveal the implementation strategy.

## When Not To Over-Test

Do not build a heavy eval harness for a tiny or subjective skill unless the user asks. A concise smoke test and checklist may be better than a complicated benchmark that nobody will maintain.

## Test Record

For substantial work, leave a small note in the final response:

- What validation ran.
- What passed.
- What was not tested and why.
- Any residual risks.

For this `skill-creator`, keep automated tests focused on behavior that is easy to regress and annoying to catch by eye:

- Name normalization and scaffold output shape.
- Frontmatter parsing and validator rules.
- Packaging exclusions and archive layout.

Run them with:

```bash
dotnet run --file <this-skill>/tests/test_scaffold.cs
dotnet run --file <this-skill>/tests/test_validate.cs
dotnet run --file <this-skill>/tests/test_package.cs
```
