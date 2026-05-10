# Skills

Reusable local skills for Claude Code / Codex-style agents.

Each skill lives in its own folder with a `SKILL.md` entrypoint and optional helpers such as `scripts/`, `references/`, and `tests/`.

## Included Skills

- [skill-creator](./skill-creator/) - Create, audit, refactor, validate, and package skills. Uses C# file-based helper scripts for scaffold, validation, and packaging.
- [dotnet-file-apps](./dotnet-file-apps/) - Work with modern C# file-based apps using `dotnet run --file`.
- [pdf](./pdf/) - Work with PDFs through bundled helpers: inspect metadata, extract text, split/merge/rotate, watermark, compress, encrypt/decrypt, edit metadata, convert images to PDF, extract embedded images, and render pages as PNG.
- [siyuan](./siyuan/) - Work with a local SiYuan workspace: search, read, create, update, move, export, and manage notebooks and documents through the local API.

## Repository Conventions

- Helper scripts default to C# file-based apps run with `dotnet run --file ...`.
- Some skills may also include focused Python helpers when a library is materially better suited to the task.
- Keep operational instructions in each skill's `SKILL.md`; the repository README is only a high-level index.

## Common Commands

Validate a skill:

```bash
dotnet run --file skill-creator/scripts/validate.cs -- <path-to-skill>
```

Scaffold a new skill:

```bash
dotnet run --file skill-creator/scripts/scaffold.cs -- <skill-name> --path .
```

Run the PDF smoke test:

```bash
dotnet run --file pdf/tests/smoke.cs
```
