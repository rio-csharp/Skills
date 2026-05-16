---
name: docx
description: Create, read, modify, and convert Word (.docx) documents with explicit line-by-line formatting commands and built-in style themes. Use when the user asks to create a Word document, read/extract text from .docx files, add formatted content to existing documents, convert docx to HTML/txt/Markdown, insert tables or images, or inspect document metadata. Supports heading, paragraph, bold, italic, underline, strikethrough, code, quote, bullet, numbered, table, and image commands with 5 built-in themes. Do not use for .doc (old binary format) files or PDF conversion (use the pdf skill instead).
---

# Docx

Work with Word (.docx) files using the bundled C# helper. Add content line-by-line with explicit formatting commands, and pick a visual theme to control colors, fonts, and spacing.

## Start Here

1. Identify the task: read, create, modify, convert, insert-table, insert-image, or set-properties.
2. Pick a style theme if creating a new document.
3. Run the appropriate command below.
4. Validate output exists and is valid.

## Commands

### Read / Extract

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- read "<path-to-file.docx>"
```

Output: document metadata (title, author, subject, keywords, created, modified) and full text content with heading styles.

### Create Document

**Plain text (default):**
```bash
dotnet run --file <skill-path>/scripts/docx.cs -- create --output "<path-to-output.docx>" --content "<text content>"
```

**With line-by-line formatting and a theme:**
```bash
dotnet run --file <skill-path>/scripts/docx.cs -- create --output "<path-to-output.docx>" --content "<line-commands>" --from-lines --style <default|report|modern|minimal|elegant>
```

Use `\n` to separate lines. Each line starts with a **format command** followed by a space and the content.

### Modify Document

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- modify "<path-to-file.docx>" --content "<text to append>"
dotnet run --file <skill-path>/scripts/docx.cs -- modify "<path-to-file.docx>" --content "<line-commands>" --from-lines
```

Appends content to an existing .docx file. Use `--output` to write to a different file instead of modifying in place.

## Line Format Commands

Each line must start with a command. Content follows after the first space.

| Command | Result |
|---------|--------|
| `H1 text` to `H6 text` | Heading (theme-colored, sized) |
| `P text` | Normal paragraph |
| `B text` | **Bold** paragraph |
| `I text` | *Italic* paragraph |
| `U text` | Underlined paragraph |
| `S text` | ~~Strikethrough~~ paragraph |
| `BI text` | ***Bold + Italic*** paragraph |
| `CODE text` | Code block (theme background, monospace font) |
| `QUOTE text` | Blockquote with left border |
| `BULLET text` | Bulleted list item |
| `NUMBER text` | Numbered list item |
| `HR` | Horizontal rule |
| `BR` | Empty line (blank paragraph). **Use sparingly** — themes already provide paragraph spacing. Do not insert `BR` between normal paragraphs or after every heading; use it only when you explicitly need a blank line (e.g. to separate major sections). |
| `TABLE row1col1,row1col2;row2col1,row2col2` | Table (comma = column, semicolon = row) |
| `IMG path[,width[,height]]` | Insert image. Width can be `mm` or `N%` of page width. Omit both for auto-fit to page. Omit height to keep aspect ratio. |
| `COLOR #RRGGBB` | Set color for following text |
| `SIZE N` | Set font size in half-points (e.g., 24 = 12pt) for following text |
| `FONT Name` | Set font for following text |
| `ALIGN left` / `ALIGN center` / `ALIGN right` / `ALIGN justify` | Set alignment for following paragraphs |

### Example Line Content

```
H1 C# Async / Await Guide
P This is a normal paragraph.
B This is bold text.
I This is italic text.
QUOTE This is a blockquote with left border.
BULLET First bullet item
BULLET Second bullet item
NUMBER First numbered item
NUMBER Second numbered item
HR
CODE public async Task FetchDataAsync() { }
TABLE Name,Age;Alice,30;Bob,25
IMG C:\\path\\to\\diagram.png        # auto-fit to page width
IMG C:\\path\\to\\diagram.png,50%      # 50% of page width, auto height
IMG C:\\path\\to\\diagram.png,80,40    # fixed 80mm x 40mm
```

## Built-in Style Themes

| Theme | Heading Color | Heading Font | Body Font | Code Font | Feel |
|-------|---------------|--------------|-----------|-----------|------|
| `default` | Blue `#2E74B5` | Arial | Arial | Consolas | Standard business |
| `report` | Dark Blue `#1F4E79` | Calibri | Calibri | Courier New | Serious corporate |
| `modern` | Cyan `#00B4D8` | Segoe UI | Segoe UI | Fira Code | Clean modern |
| `minimal` | Black `#212529` | Helvetica Neue | Helvetica Neue | SF Mono | Ultra minimal |
| `elegant` | Charcoal `#4A4A4A` | Georgia | Georgia | Courier New | Elegant serif |

Each theme controls heading colors, fonts, spacing, table borders, quote styling, and code block appearance. Use `--style` on `create` to choose one. If omitted, `default` is used.

## Insert Table (Command-Line)

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- insert-table "<path-to-file.docx>" --rows 3 --cols 3 --data "Name,Age,City\nAlice,30,NYC\nBob,25,LA" --header
```

Inserts a table into an existing document. `--data` uses `\n` for rows and commas for columns. Use `--header` to style the first row as a header. Use `--output` to write to a different file.

## Insert Image

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- insert-image "<path-to-file.docx>" --image "<path-to-image.png>" --width 100 --height 80
```

Inserts an image into an existing document. Supported formats: PNG, JPEG, GIF, BMP, TIFF. Width/height are in millimeters. Use `--output` to write to a different file.

## Convert Document

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- convert "<path-to-file.docx>" --format <txt|html|md>
```

Converts a .docx file to plain text, HTML (preserves formatting), or Markdown. PDF conversion is not supported (use the pdf skill instead).

## Set Document Properties

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- set-properties "<path-to-file.docx>" --title "My Report" --author "John Doe" --subject "Quarterly Review" --keywords "report, Q1"
```

Updates document metadata (title, author, subject, keywords).

## Workflow

1. **Read**: Extract text/metadata first to understand document structure.
2. **Create**: Use `create` with `--from-lines` and line commands for rich formatting. Pick a `--style` that matches the document purpose.
3. **Insert**: Use `insert-table` and `insert-image` to add structured content.
4. **Style**: Use `set-properties` to set title, author, and other metadata.
5. **Convert**: If the user needs HTML, text, or Markdown output, use the `convert` command.
6. **Validate**: Check output file was created and has content.

## Tips

- Use `\n` for line breaks in command-line `--content` arguments.
- For complex documents, write the line commands to a file first, then read it and pass as `--content`.
- State commands (`COLOR`, `SIZE`, `FONT`, `ALIGN`) affect all following paragraphs until changed again.
- Commands are case-insensitive (`h1`, `H1`, `b`, `B` all work).
- **Avoid over-using `BR`**. Built-in themes already apply spacing between paragraphs and headings. Adding `BR` after every paragraph or heading creates excessive whitespace. Use `BR` only when you intentionally want a blank paragraph (e.g. between major sections).
- When the user does not specify a style, choose one based on context: `report` for business documents, `modern` for tech content, `elegant` for formal writing, `minimal` for simple notes.

## Resources

- `scripts/docx.cs`: bundled helper for all docx operations.
- `references/format.md`: DOCX format internals and limitations.

## Safety

- Read-only operations (read, convert) do not modify source files.
- Create/modify operations create new files or modify copies.
- No network operations or credential requirements.
- Image insert validates the image file exists before modifying the document.

## Validation

- Check output file exists after create/modify/convert/insert-table/insert-image.
- For read operations, verify extracted text is non-empty.
- Confirm HTML conversion produces valid output files.
- Test line-format creation by opening the .docx and checking heading/bold/italic rendering.
