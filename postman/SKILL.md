---
name: postman
description: Manage Postman cloud collections through the official Postman API. Use when the user asks to create, list, export, edit, delete, validate, or run Postman collections. Prefer remote API operations and let Postman Desktop/Web sync changes naturally. Do not use for generic HTTP debugging; use curl instead.
---

# Postman

Manage Postman collections through the Postman API. This skill intentionally avoids local collection editing because Postman Desktop and cloud sync can normalize or cache collection JSON in surprising ways.

All remote operations require `POSTMAN_API_KEY`.

```bash
dotnet run --file <skill-path>/scripts/postman.cs <command> [options]
```

## Remote Collections

### List collections

```bash
dotnet run --file <skill-path>/scripts/postman.cs list-remote
```

### Create a collection

```bash
dotnet run --file <skill-path>/scripts/postman.cs create --name "My API"
```

Use `--workspace <workspace-id>` to create the collection in a specific workspace.

### List one collection

```bash
dotnet run --file <skill-path>/scripts/postman.cs list --collection <collection-uid>
```

### Add a folder

```bash
dotnet run --file <skill-path>/scripts/postman.cs add-folder --collection <collection-uid> --name "Notes"
```

### Add a request

```bash
dotnet run --file <skill-path>/scripts/postman.cs add-request \
  --collection <collection-uid> \
  --name "List Notes" \
  --method GET \
  --url "{{baseUrl}}/api/notes" \
  --folder "Notes"
```

Options:
- `--method, -m`: HTTP method. Default: `GET`.
- `--url, -u`: Request URL.
- `--body, -b`: Raw request body.
- `--header, -h`: Headers as `Key:Value,Key:Value`.
- `--folder, -f`: Add the request under this folder, creating it if needed.
- `--description, -d`: Request description.
- `--use-variable`: Replace a matching absolute URL prefix with a collection variable.

Variable URLs such as `{{baseUrl}}/api/notes` keep `raw` unchanged and store the host as `["{{baseUrl}}"]` without a `protocol`. Do not store the host as `["baseUrl"]` or add `protocol: "http"`; Postman may otherwise normalize the variable reference into a literal host.

### Remove a request or folder

```bash
dotnet run --file <skill-path>/scripts/postman.cs remove --collection <collection-uid> --name "List Notes"
```

### Export a remote collection

```bash
dotnet run --file <skill-path>/scripts/postman.cs pull --collection <collection-uid> --output collection.json
```

Use `--raw` to write only the collection object instead of the `{"collection": ...}` API wrapper.

### Validate a remote collection

```bash
dotnet run --file <skill-path>/scripts/postman.cs validate --collection <collection-uid>
```

### Delete a collection

```bash
dotnet run --file <skill-path>/scripts/postman.cs delete --collection <collection-uid>
```

Confirm with the user before deleting remote collections.

## Run Collections

The helper can export a remote collection to a temporary file and run it with Newman.

```bash
dotnet run --file <skill-path>/scripts/postman.cs run --collection <collection-uid>
```

Common options passed through to Newman:
- `--environment, -e`
- `--iteration-data, -d`
- `--reporters, -r`
- `--folder, -f`
- `--iteration-count`
- `--timeout`
- `--delay-request`
- `--export-environment`
- `--export-collection`

Install Newman separately if needed:

```bash
npm install -g newman
```

## API Key

Create a Postman API key from Postman's integration settings and expose it as `POSTMAN_API_KEY`.

PowerShell:

```powershell
$env:POSTMAN_API_KEY = "your-api-key"
```

Bash:

```bash
export POSTMAN_API_KEY="your-api-key"
```

The helper checks the process environment first, then User and Machine environment variables.

## Safety

- Remote mutations use the Postman API: fetch the collection, modify the in-memory JSON, then update the remote collection.
- Postman's update collection endpoint replaces the existing collection with the request body, so avoid parallel edits to the same collection.
- Confirm before deleting collections or running requests against production endpoints.
- Export before risky edits when the collection is important.

## Validation

```bash
dotnet run --file <skill-path>/scripts/postman.cs self-test
dotnet run --file <skill-path>/tests/smoke.cs <skill-path>
```

## Resources

- `scripts/postman.cs`: remote API helper.
- `references/postman-collection-format.md`: collection JSON format notes.
- `references/newman.md`: Newman CLI notes.
