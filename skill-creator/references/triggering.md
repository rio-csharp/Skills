# Triggering And Description Design

Use this guide when writing or improving the `description` field.

## Why Description Matters

The skill description is visible before the body loads. If it is vague, the skill may not trigger. If it is too broad, it may trigger for unrelated work and waste context.

The body should not contain essential "when to use" criteria because the body is unavailable until after the trigger decision.

## Description Formula

Use this pattern:

```text
Do [specific capability] for [domain/files/workflow]. Use when the user asks to [trigger actions], works with [file types/tools], needs [specialized outcome], or mentions [common synonyms]. Do not use for [near-miss boundary] when helpful.
```

Examples:

```yaml
description: Create and edit regulated incident reports from logs, timelines, and stakeholder notes. Use when the user asks for incident summaries, postmortems, RCA drafts, severity reports, action items, or timeline reconstruction from operational evidence.
```

```yaml
description: Transform spreadsheet files with formulas, tables, validation, charts, and clean formatting. Use when the user asks to modify .xlsx/.xls/.csv data, add calculated columns, reconcile worksheets, create charts, or deliver an updated spreadsheet file.
```

## Should-Trigger Coverage

Include realistic phrasing:

- Formal task names.
- Casual language.
- File extensions and artifact names.
- Domain terms.
- Tool names.
- Output deliverables.
- Implicit needs where the user does not name the skill.

## Should-Not-Trigger Boundaries

Add boundaries only when likely confusion exists. Examples:

- A spreadsheet skill should not trigger for a pasted two-row table that can be answered inline unless the user wants a file.
- A PowerPoint skill should not trigger for generic presentation advice unless the user wants a deck artifact.
- A browser automation skill should not trigger for static web research unless interaction or visual inspection is required.

## Trigger Eval Set

For important skills, draft 12-20 queries:

- 6-10 should-trigger prompts.
- 6-10 near-miss should-not-trigger prompts.
- Include messy, realistic details.
- Avoid obviously irrelevant negatives.

Bad negative: "Write a fibonacci function."

Better negative for a spreadsheet skill: "Can you explain what a pivot table is? No need to edit my file yet."

## Description Review Checklist

- Could an agent select the skill using only name and description?
- Does it include both capability and trigger contexts?
- Does it avoid generic claims?
- Does it include file types or tool names where relevant?
- Does it mention near-misses only when necessary?
- Is it under 1024 characters?
- Is it written for user intent rather than implementation internals?
