# Gotchas And Conversion

Use this reference when the user hits an error, when compatibility matters, or when deciding whether to stay in file-based form.

## Common Gotchas

- `dotnet run app.cs` without `--file` can be ambiguous in older explanations. Prefer `dotnet run --file app.cs` when teaching.
- A file-based app still requires a recent enough .NET SDK with file-based app support.
- Directives must be placed before ordinary code.
- Private NuGet feeds may still need the usual NuGet authentication/configuration.
- A `.cs` extension is required for normal file-based app execution.
- If the current directory has a project, `dotnet run` alone will target that project rather than your loose `.cs` file.

## Diagnosing Failures

If execution fails, classify the problem:

- CLI usage: wrong command shape, missing `--file`, wrong argument separator.
- SDK/runtime: SDK too old or feature unavailable in the environment.
- Restore: package feed/authentication/network issue.
- Compilation: ordinary C# syntax or reference problem.
- Conversion/project expectation: user actually needs a multi-file project, not a single file.

When helping a weaker agent, make the diagnosis explicit before suggesting commands. Do not jump from "it failed" straight to "convert it to a project."

## Converting To A Project

Use:

```bash
dotnet project convert app.cs
```

By default, this creates a sibling directory named after the file and writes a converted source file plus a new `.csproj` inside it. The original source file stays where it is.

Recommend conversion when:

- The app has outgrown one file.
- The user needs richer build customization.
- The user wants standard project tooling or IDE workflows.
- The file-based app is becoming a real application instead of a utility script.

## When To Stay Single-File

Stay with a file-based app when:

- The user wants a quick runnable example.
- The code is a utility, scratch script, or lightweight automation.
- The user explicitly wants Python/Node-style "one file, just run it" behavior.

## Weak-Model Teaching Checklist

Before you finish, check:

- Did you teach `dotnet run --file <path>` before shorthand forms?
- Did you keep stdin as `dotnet run -`?
- Did you separate file-based apps from `.csproj` projects?
- Did you explain where directives go?
- Did you recommend project conversion only for a concrete reason?

## Official Knowledge To Convey

When another agent may predate this feature, teach these exact facts:

- `.NET` can run a loose `.cs` file directly.
- `dotnet run --file <path>` is the explicit form.
- `dotnet <path>.cs` is the shorthand form.
- File directives like `#:package` and `#:project` are supported.
- `dotnet run -` can read code from stdin.
- `dotnet project convert <file>.cs` converts a file-based app into a standard project.
