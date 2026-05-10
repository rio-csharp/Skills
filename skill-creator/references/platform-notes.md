# Platform Notes

Use this guide when a skill targets a specific runtime or distribution path.

## Cross-Platform Default

The safest portable skill uses:

- `SKILL.md`
- YAML frontmatter with `name` and `description`
- Optional `scripts/`, `references/`, and `assets/`

Avoid platform-specific frontmatter unless the user names the target platform or the existing skill already uses it.

## Codex

Codex discovers skills from configured skill directories. For local user skills, prefer `$CODEX_HOME/skills` when set, otherwise `~/.codex/skills`.

Codex skills may include local scripts and references. Keep instructions concise because skill bodies share context with system instructions, conversation history, and other loaded skills.

## Claude Code

Claude Code supports skills with `SKILL.md` and bundled resources, and adds platform-specific frontmatter for invocation control, dynamic context, subagent execution, path scoping, and tool permission pre-approval.

Useful fields include:

- `disable-model-invocation`: use for workflows the user should invoke manually, especially side-effectful tasks.
- `user-invocable`: use when a skill should not appear as a slash-invoked command.
- `context: fork` and `agent`: use when the skill should run in an isolated subagent context.
- `paths`: use when a skill should activate only for matching file paths.
- `shell`: use when inline shell commands must run under a specific shell.
- `allowed-tools`: pre-approves listed tools while the skill is active; it does not fully sandbox other tools.

For Claude Code testing, independent agent runs or CLI-based checks can be valuable, but do not rely on a heavy benchmark harness unless the skill needs it.

## Claude.ai

Claude.ai-style environments may not have subagents, a persistent browser, or the same local CLI tools. Adapt by:

- Running manual smoke tests instead of subagent evals.
- Presenting file outputs directly when browser review is unavailable.
- Avoiding scripts that assume a specific shell unless the environment provides it.

## API Or Container Use

For API/containerized skills:

- Keep the skill folder self-contained.
- Avoid relying on machine-specific absolute paths.
- Document runtime dependencies clearly.
- Use scripts that accept arguments and write outputs to caller-provided paths.
- Assume the skill may be uploaded, mounted, or copied into a clean container.
- Design for no external network access in the Claude API code execution environment.
- Do not require runtime package installation for API use; rely on preinstalled packages or bundle portable code.
- Remember that custom API skills are uploaded and managed separately from Claude.ai and Claude Code skills.

## Headless Environments

If there is no browser or GUI:

- Generate static artifacts instead of launching interactive viewers.
- Print file paths and concise summaries.
- Prefer command-line validation.

## Windows Notes

When writing commands in skills that may run on Windows:

- Avoid Unix-only shell syntax unless the skill is Unix-specific.
- Prefer Python scripts for portable file operations.
- Mention PowerShell alternatives only if the skill expects Windows users.
