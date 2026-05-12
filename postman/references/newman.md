# Newman CLI Reference

The `newman` CLI runs Postman collections from the command line.

## Installation

```bash
npm install -g newman
```

Verify:
```bash
newman --version
```

## Commands

### newman run

Run a Postman collection file.

```bash
newman run <collection-file> [options]
```

## Options

| Option | Description |
|--------|-------------|
| `-e, --environment <path>` | Path to a Postman environment JSON file |
| `-d, --iteration-data <path>` | Path to a CSV or JSON data file for iterations |
| `-r, --reporters <list>` | Comma-separated reporters: `cli`, `html`, `json`, `junit` (default: `cli`) |
| `-o, --output <dir>` | Output directory for reporter files (used with html/json reporters) |
| `--folder <name>` | Run a specific folder within the collection |
| `--iteration-count <n>` | Number of times to run the collection |
| `--timeout <ms>` | Request timeout in milliseconds |
| `--delay-request <ms>` | Delay between each request in milliseconds |
| `-n, --iteration-data-var <name>` | Variable name in collection that holds the data |
| `--export-environment <path>` | Export final environment to a file |
| `--export-collection <path>` | Export the collection (with overrides) to a file |
| `--bail` | Stop on first test failure |
| `--disable-unicode` | Force ASCII output |
| `--global-var <key=value>` | Set a global variable (can be repeated) |
| `--color off` | Disable colored output |

## Reporters

### cli (default)
Shows progress in terminal. Use `--no-color` to disable colors.

### html
Generates an HTML report. Requires `-o <dir>`.

```bash
newman run collection.json -r html -o reports/
```

### json
Generates a JSON report. Requires `-o <dir>`.

```bash
newman run collection.json -r json -o reports/
```

### junit
Generates JUnit XML report (for CI integration). Requires `-o <file>`.

```bash
newman run collection.json -r junit -o reports/results.xml
```

## Exit Codes

- `0`: All tests passed.
- `1`: One or more tests failed.
- `2`: Fatal error (e.g., collection not found).

## Environment File Format

```json
{
  "id": "env-id",
  "name": "My Environment",
  "values": [
    { "key": "baseUrl", "value": "https://api.example.com", "type": "default" },
    { "key": "apiKey", "value": "", "type": "secret" }
  ]
}
```

## Data File Format (CSV)

```csv
username,password
user1,pass1
user2,pass2
```

## Examples

### Basic Run

```bash
newman run my-collection.json
```

### With Environment

```bash
newman run my-collection.json --environment dev-env.json
```

### With Data Iteration

```bash
newman run my-collection.json --iteration-data users.csv --iteration-count 10
```

### CI/CD (JUnit XML)

```bash
newman run my-collection.json -r junit -o reports/results.xml
```

### With Global Variables

```bash
newman run my-collection.json --global-var "apiVersion=v2" --global-var "debug=true"
```

### Silent Run (no reporter)

```bash
newman run my-collection.json --silent
```