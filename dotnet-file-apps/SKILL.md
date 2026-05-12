---
name: dotnet-file-apps
description: Run, write, debug, explain, and convert C# file-based apps with `dotnet run --file` or the `dotnet app.cs` shorthand. Use when the user wants to execute a standalone `.cs` file without a project file, asks about `#:package` / `#:sdk` / `#:property` / `#:project` directives, wants to use NuGet packages in a single-file C# program, needs to pass arguments to a file-based app, wants to pipe C# through stdin with `dotnet run -`, or needs to convert a file-based app into a regular project with `dotnet project convert`.
---

# .NET File-Based Apps

Use this skill to help another agent work with single-file C# programs that run directly through the .NET CLI. Keep `SKILL.md` short, and load references only when the task needs deeper syntax or edge-case detail.

Assume the agent using this skill may predate the feature or may be weak at distinguishing file-based apps from normal `.csproj` workflows. Use explicit command shapes and explicit decision steps.

## Start Here

1. Identify whether the user wants to run, author, debug, explain, or convert a file-based app.
2. Confirm the target artifact is a standalone `.cs` file rather than an existing `.csproj` project.
3. Prefer the explicit command form `dotnet run --file <path>` when giving instructions or running commands in directories that might already contain a project file.
4. Read the relevant reference before doing anything non-trivial:
   - Command forms and execution behavior: [references/commands.md](references/commands.md)
   - File directives and examples: [references/directives.md](references/directives.md)
   - Limits, traps, and project conversion: [references/gotchas-and-conversion.md](references/gotchas-and-conversion.md)

## Decision Table

Use this table before acting:

- Existing `.cs` file to execute:
  Use `dotnet run --file <path>` by default.
- Existing `.cs` file plus program arguments:
  Use `dotnet run --file <path> -- <args>`.
- Tiny code snippet coming from stdin:
  Use `dotnet run -`.
- Single-file app needs packages, SDK changes, or project references:
  Add file directives at the top of the file and read [references/directives.md](references/directives.md).
- User actually has a multi-file app or wants long-term project tooling:
  Explain file-based apps briefly, then consider `dotnet project convert`.

## Core Workflow

### Running An Existing File

1. Locate the `.cs` file path.
2. Inspect the top of the file for directives such as `#:package`, `#:sdk`, `#:property`, or `#:project`.
3. Run it with `dotnet run --file <path>` or `dotnet <path>.cs`.
4. If the user supplied program arguments, append them after `--`.
5. Report output or errors clearly, including whether the problem is CLI usage, missing SDK/runtime support, package restore, or compilation.

For weak-model-safe execution, prefer this exact order:

1. Check whether the target is a real file path, stdin snippet, or actually a project.
2. If it is a file, default to `dotnet run --file <path>`.
3. Only mention `dotnet <path>.cs` after the explicit form is already clear.
4. Only use `dotnet run -` for stdin snippets.
5. If anything fails, classify the failure before suggesting a fix.

### Writing A New File-Based App

1. Create a `.cs` file, not a `.csproj`.
2. Add file directives only when needed.
3. Keep examples minimal and runnable.
4. If the file needs packages or project references, place directives at the top of the file before regular code.
5. Smoke-test by actually running the file.

### Converting To A Project

If the user outgrows the single-file form, use `dotnet project convert <file>.cs` and then work in the generated project normally. Read [references/gotchas-and-conversion.md](references/gotchas-and-conversion.md) first when explaining tradeoffs.

## Important Rules

- Prefer `dotnet run --file <path>` in agent-authored instructions because it is unambiguous.
- Mention `dotnet <path>.cs` as a convenience form, not the safest default.
- Do not assume the feature exists in older SDKs; verify the local SDK when the environment matters.
- Keep trigger guidance in frontmatter, not in a separate "when to use" essay.
- When teaching an older-model agent, explain the exact command shape and where directives must appear.

## Anti-Patterns

Do not do these things:

- Do not use `dotnet run app.cs` as the default recommended syntax.
- Do not rewrite stdin execution as `dotnet run --file -`.
- Do not treat a loose `.cs` file like a `.csproj` project.
- Do not place directives after normal code.
- Do not recommend converting to a project unless the single-file form is becoming a bad fit.

## Minimal Correct Examples

Use these when the other agent seems confused:

```bash
dotnet run --file hello.cs
dotnet run --file hello.cs -- a b c
echo "Console.WriteLine(\"hi\");" | dotnet run -
dotnet project convert hello.cs
```

## Reference Files

- [references/commands.md](references/commands.md): explicit and shorthand command forms, stdin usage, and argument passing.
- [references/directives.md](references/directives.md): `#:package`, `#:sdk`, `#:property`, `#:project`, shebang, and runnable examples.
- [references/gotchas-and-conversion.md](references/gotchas-and-conversion.md): common mistakes, SDK/version expectations, package restore notes, and conversion to projects.
- [tests/test_run_file_app.cs](tests/test_run_file_app.cs): smoke test for direct file execution.
- [tests/test_stdin_file_app.cs](tests/test_stdin_file_app.cs): smoke test for stdin execution.
- [tests/test_convert_file_app.cs](tests/test_convert_file_app.cs): smoke test for project conversion.
