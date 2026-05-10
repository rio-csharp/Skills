---
name: dotnet-file-apps
description: Run, write, debug, explain, and convert C# file-based apps with `dotnet run --file` or `dotnet <file>.cs`. Use when the user wants to execute a standalone `.cs` file without a `.csproj`, asks about `#:package` / `#:sdk` / `#:property` / `#:project` directives, wants to use NuGet packages in a single-file C# program, needs to pass arguments to a file-based app, wants to pipe C# through stdin with `dotnet run -`, or needs to convert a file-based app into a regular project with `dotnet project convert`.
---

# .NET File-Based Apps

Use this skill to help another agent work with single-file C# programs that run directly through the .NET CLI. Keep `SKILL.md` short, and load references only when the task needs deeper syntax or edge-case detail.

## Start Here

1. Identify whether the user wants to run, author, debug, explain, or convert a file-based app.
2. Confirm the target artifact is a standalone `.cs` file rather than an existing `.csproj` project.
3. Prefer the explicit command form `dotnet run --file <path>` when giving instructions or running commands in directories that might already contain a project file.
4. Read the relevant reference before doing anything non-trivial:
   - Command forms and execution behavior: [references/commands.md](references/commands.md)
   - File directives and examples: [references/directives.md](references/directives.md)
   - Limits, traps, and project conversion: [references/gotchas-and-conversion.md](references/gotchas-and-conversion.md)

## Core Workflow

### Running An Existing File

1. Locate the `.cs` file path.
2. Inspect the top of the file for directives such as `#:package`, `#:sdk`, `#:property`, or `#:project`.
3. Run it with `dotnet run --file <path>` or `dotnet <path>.cs`.
4. If the user supplied program arguments, append them after `--`.
5. Report output or errors clearly, including whether the problem is CLI usage, missing SDK/runtime support, package restore, or compilation.

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

## Reference Files

- [references/commands.md](references/commands.md): explicit and shorthand command forms, stdin usage, and argument passing.
- [references/directives.md](references/directives.md): `#:package`, `#:sdk`, `#:property`, `#:project`, shebang, and runnable examples.
- [references/gotchas-and-conversion.md](references/gotchas-and-conversion.md): common mistakes, SDK/version expectations, package restore notes, and conversion to projects.
- [tests/test_run_file_app.cs](tests/test_run_file_app.cs): smoke test for direct file execution.
- [tests/test_stdin_file_app.cs](tests/test_stdin_file_app.cs): smoke test for stdin execution.
- [tests/test_convert_file_app.cs](tests/test_convert_file_app.cs): smoke test for project conversion.
