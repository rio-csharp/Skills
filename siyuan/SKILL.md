---
name: siyuan
description: Interact with SiYuan (思源笔记) local API — search/read/create/update/delete notes, execute SQL queries, manage notebooks. Use when the user mentions 思源, siyuan, notes, notebooks, or wants to manage their note-taking workspace. Requires SiYuan running at http://127.0.0.1:6806 with API token configured.
---

# SiYuan Skill

All SiYuan interactions go through the bundled C# script. Do NOT call the SiYuan HTTP API directly with curl, Python, or ad-hoc code.

**Script:** `dotnet-script "<skill-base>/scripts/siyuan.csx" <command> [args]`

## Configuration

Ask the user for `$SIYUAN_URL` (default `http://127.0.0.1:6806`) and `$SIYUAN_TOKEN` (from SiYuan Settings → About → API Token). Save to `settings.json` → `env` if provided.

## Core Commands

| Command | Args | Description |
|---------|------|-------------|
| `ls` | — | List all notebooks |
| `tree` | `notebook [path]` | Document tree with titles and IDs |
| `docs` | `notebook [path]` | Flat doc listing |
| `cat` | `id` | Export doc as markdown |
| `info` | `id` | Block/doc metadata |
| `get-doc` | `id` | Get doc content (JSON) |
| `insert-block` | `parentID dataType data [--previous ID] [--next ID]` | Insert block |
| `mv` | `toID fromID [fromID ...]` | Move docs by ID |
| `sql` | `statement` | Execute SQL query |
| `search` | `query [page]` | Full-text search |
| `search-docs` | `query` | Search doc titles |
| `path` | `id` | HPath by ID |
| `full-path` | `id` | Full HPath with parents |
| `breadcrumb` | `id` | Block breadcrumb |
| `outline` | `id` | Doc heading outline |
| `create` | `notebook path [--parent id]` | Create doc from stdin markdown |
| `update-md` | `id` | Replace doc content from stdin markdown |
| `rename` | `id title` | Rename doc |
| `tags` | `[keyword]` | List/search tags |
| `export` | `id` | Export doc in various formats |
| `open-nb` | `notebook` | Open/mount a notebook |
| `close-nb` | `notebook` | Close/unmount a notebook |
| `remove-nb` | `notebook` | Remove a notebook permanently |
| `create-nb` | `name` | Create a new notebook |
| `rename-nb` | `notebook name` | Rename a notebook |
| `get-nb-conf` | `notebook` | Get notebook config |
| `set-nb-conf` | `notebook confJSON` | Set notebook config |
| `attrs` | `id` | Get block attributes |
| `set-attrs` | `id key=value [key=value...]` | Set block attributes |
| `raw` | `endpoint [json]` | Call any API endpoint |
| `duplicate` | `id` | Duplicate a doc |
| `rm` | `id [id ...]` | Remove docs by ID |
| `rm-path` | `notebook path` | Remove doc by path |

Use `dotnet-script "<skill-base>/scripts/siyuan.csx" help` for the full list.

## ID Format

Document IDs are timestamps like `20260402222543-0ex8h5n`. In path contexts they end with `.sy`; omit `.sy` in ID parameters.

## Deletion

Use `rm` (calls `removeDocByID`). Do NOT use `deleteBlock` — that only deletes blocks **inside** docs, not the docs themselves.

## Creating Nested Docs

Use `create --parent <parentID>` — it auto-constructs the path from the parent title so SiYuan nests correctly.

## Updating Doc Content

Use `update-md` (calls `updateBlock` API) — preserves parent-child links and the doc path. Avoid delete + recreate.

## Large Results

For large SQL results, add `LIMIT` / `pageSize` to the query.

## When the Script Lacks a Feature

Add commands to `scripts/siyuan.csx` rather than working around the script with raw HTTP calls.
