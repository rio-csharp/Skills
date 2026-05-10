# Commands

Use this reference when the user needs to run a file-based app or when another agent might confuse the new command forms with normal project execution.

## Fast Triage

Use this mental split before picking a command:

- A real `.cs` file on disk: `dotnet run --file <path>` by default.
- A very small stdin snippet: `dotnet run -`.
- A normal project with `.csproj`: this skill may not be the right tool.

## Preferred Command Forms

Use these in order of safety:

```bash
dotnet run --file path/to/app.cs
dotnet path/to/app.cs
```

Both run a standalone `.cs` file without a `.csproj`. The first form is safer in generated instructions because it is explicit and avoids ambiguity when the current directory already contains a project.

## Passing Program Arguments

Use `--` before program arguments:

```bash
dotnet run --file app.cs -- hello 123
dotnet app.cs -- hello 123
```

Everything after `--` is passed to the C# program rather than interpreted by the CLI.

## Reading Source From Stdin

The CLI can run code piped through standard input:

```bash
echo "Console.WriteLine(\"hi\");" | dotnet run -
```

Use this only for tiny examples, ad hoc demos, or generated code that does not need to live in a file.

`dotnet run -` is the stdin form. Do not rewrite it to `dotnet run --file -`.

## What To Say To The User

When teaching:

- Explain that `--file` is the explicit file-based app switch.
- Explain that `dotnet app.cs` is shorthand for file-based execution.
- Explain that `dotnet run` without `--file` means "run a project," not "run a loose `.cs` file."

If the listener seems weak or out-of-date, explicitly say:

- ".NET can run a loose `.cs` file directly."
- "`dotnet run --file` is the safest syntax to teach first."
- "`dotnet run -` is specifically for stdin."

## When To Prefer `--file`

Prefer `dotnet run --file` when:

- The working directory may contain a `.csproj` or `.sln`.
- You are generating commands for another agent to follow.
- You want the instruction to be robust across environments.
- You need to emphasize that this is a file-based app, not a project.
