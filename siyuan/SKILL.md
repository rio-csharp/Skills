---
name: siyuan
description: Work with a local SiYuan (思源笔记) workspace through the bundled C# CLI helper. Use when the user explicitly mentions SiYuan/思源, asks to search, read, create, update, move, export, or manage SiYuan documents/notebooks, or provides SiYuan block/document IDs. Requires a running local SiYuan service and API token.
---

# SiYuan

Use this skill for the user's SiYuan knowledge base. The primary interface is the bundled C# CLI helper, not direct HTTP calls.

```bash
dotnet run --file <skill-root>/scripts/siyuan.cs -- <command> [args]
```

## Configuration

The helper reads `SIYUAN_URL` and `SIYUAN_TOKEN`.

- `SIYUAN_URL`: defaults to `http://127.0.0.1:6806`.
- `SIYUAN_TOKEN`: from SiYuan Settings, About, API Token.

Do not ask for the token unless a live operation needs it. Never print or store the token in repo files.

## Start Here

1. For exploration, start read-only: `ls`, `search`, `search-docs`, `tree`, `docs`, `cat`, `outline`, `info`.
2. For edits, confirm exact notebook/doc/block IDs before running write commands.
3. For unsupported SiYuan kernel endpoints, use `raw` or add a command to `scripts/siyuan.cs`.
4. Read [references/siyuan-api.md](references/siyuan-api.md) only for low-level SiYuan kernel API details, not for normal CLI usage.

## Command Reference

| Command | Args | Use For | Notes |
|---|---|---|---|
| `help` | `[command]` | Show all commands or one command description. | Safe, no token needed. |
| `ls` | none | List notebooks. | First command to discover notebook names/IDs. |
| `tree` | `notebook [path]` | Show document tree with titles and IDs. | `notebook` can be name or ID. |
| `docs` | `notebook [path]` | Show a flat doc listing under a path. | Useful when tree is too much. |
| `cat` | `id` | Export a document as Markdown. | Use after search/tree to inspect content. |
| `info` | `id` | Print block/doc metadata JSON. | Useful to confirm root title and IDs. |
| `get-doc` | `id` | Print raw document JSON. | More verbose than `cat`. |
| `search` | `query [page]` | Full-text search blocks/content. | Returns matched count and snippets. |
| `search-docs` | `query` | Search document titles. | Prefer this when looking for a note by title. |
| `path` | `id` | Get human-readable path by ID. | Returns hPath. |
| `full-path` | `id` | Get full human-readable path including parents. | Use before moving or editing nested docs. |
| `breadcrumb` | `id` | Print block breadcrumb JSON. | Useful for block context. |
| `outline` | `id` | Show document headings. | Good quick overview before reading whole doc. |
| `recent` | none | Show recently updated blocks. | Read-only discovery. |
| `tags` | `[keyword]` | List/search tags. | Read-only. |
| `templates` | `[keyword]` | Search templates. | Read-only. |
| `backlinks` | `id` | Get backlinks for a block/doc. | Read-only. |
| `history` | `notebook path` | Get document history. | `path` is usually like `/Doc.sy`. |
| `sql` | `statement` | Execute a SiYuan SQL query. | Prefer `SELECT ... LIMIT ...`; write SQL is high risk. |
| `create` | `notebook path [--parent id]` | Create a document from stdin Markdown. | Requires Markdown piped on stdin. |
| `update-md` | `id` | Replace document content from stdin Markdown. | Preserves document ID and links; first H1 renames doc. |
| `rename` | `id title` | Rename a document. | Confirm ID first. |
| `mv` | `toID fromID [fromID ...]` | Move documents by ID. | Confirm source and target IDs first. |
| `duplicate` | `id` | Duplicate a document. | Useful before risky edits. |
| `rm` | `id [id ...]` | Remove documents by ID. | High risk; confirm exact IDs. |
| `rm-path` | `notebook path` | Remove document by notebook/path. | High risk; prefer ID deletion when possible. |
| `insert-block` | `parentID dataType data [--previous ID] [--next ID]` | Insert a block. | `dataType` is usually `markdown`. |
| `move-block` | `id [--parent ID] [--previous ID]` | Move a block. | Confirm block ID and destination. |
| `attrs` | `id` | Get block attributes. | Read-only. |
| `set-attrs` | `id key=value [key=value ...]` | Set attributes on one block. | Write operation. |
| `set-attrs-batch` | `key=value ... --where sql_where` | Set attributes on blocks matching SQL WHERE. | High risk bulk operation; use narrow WHERE. |
| `export` | `id format` | Export document. | Formats: `md`, `sy`, `html`, `docx`, `pdf`, `epub`, `textile`, `org`, `odt`, `rtf`, `asciidoc`. |
| `open-nb` | `notebook` | Open/mount a notebook. | Write-ish workspace operation. |
| `close-nb` | `notebook` | Close/unmount a notebook. | Write-ish workspace operation. |
| `create-nb` | `name` | Create a notebook. | Write operation. |
| `rename-nb` | `notebook name` | Rename a notebook. | Confirm notebook ID/name. |
| `get-nb-conf` | `notebook` | Get notebook config JSON. | Read-only. |
| `set-nb-conf` | `notebook confJSON` | Set notebook config. | High risk; preserve unknown fields. |
| `remove-nb` | `notebook` | Remove a notebook. | Very high risk; require explicit user confirmation. |
| `raw` | `endpoint [json]` | Call any SiYuan kernel API endpoint. | Endpoint is without `/api`; use only when helper lacks a command. |

## Common Examples

List notebooks:

```bash
dotnet run --file <skill-root>/scripts/siyuan.cs -- ls
```

Search for notes:

```bash
dotnet run --file <skill-root>/scripts/siyuan.cs -- search "keyword"
dotnet run --file <skill-root>/scripts/siyuan.cs -- search-docs "title"
```

Read a document:

```bash
dotnet run --file <skill-root>/scripts/siyuan.cs -- cat 20260402222543-0ex8h5n
dotnet run --file <skill-root>/scripts/siyuan.cs -- outline 20260402222543-0ex8h5n
```

Create a document from Markdown:

```bash
@"
# New Note

Content.
"@ | dotnet run --file <skill-root>/scripts/siyuan.cs -- create "Notebook Name" "New Note"
```

Create a nested document:

```bash
@"
# Child Note
"@ | dotnet run --file <skill-root>/scripts/siyuan.cs -- create "Notebook Name" "Child Note" --parent 20260402222543-0ex8h5n
```

Update a document without changing its ID:

```bash
@"
# Updated Title

Updated content.
"@ | dotnet run --file <skill-root>/scripts/siyuan.cs -- update-md 20260402222543-0ex8h5n
```

Query with SQL:

```bash
dotnet run --file <skill-root>/scripts/siyuan.cs -- sql "SELECT id, content, hpath FROM blocks WHERE type='d' AND content LIKE '%keyword%' LIMIT 20"
```

Call an uncovered kernel API endpoint:

```bash
dotnet run --file <skill-root>/scripts/siyuan.cs -- raw /query/sql '{"stmt":"SELECT id FROM blocks LIMIT 5"}'
```

## Safety Rules

- Prefer read-only commands before writes: `ls`, `search`, `search-docs`, `tree`, `docs`, `cat`, `info`, `outline`.
- Treat `rm`, `rm-path`, `remove-nb`, `set-nb-conf`, `set-attrs-batch`, broad `sql`, and `raw` as high risk.
- Before high-risk commands, confirm exact target IDs/names and the user's intent.
- For deletion, use `rm` or `rm-path` for documents. Do not use SiYuan `deleteBlock` to remove a document; it deletes blocks inside documents.
- For large SQL/search results, add `LIMIT`, page numbers, or narrow predicates.
- If the helper lacks a feature, add a command to `scripts/siyuan.cs` rather than writing one-off HTTP code, unless the user explicitly asks for a quick raw call.

## Validation

After editing this skill or its helper, run the lightweight checks:

```bash
dotnet run --file <skill-root>/scripts/siyuan.cs -- help
dotnet run --file <skill-root>/tests/test_siyuan.cs
```

When SiYuan is running and a disposable test notebook is acceptable, run the live integration suite:

```bash
dotnet run --file <skill-root>/tests/test_siyuan_integration.cs -- --require-live
```
