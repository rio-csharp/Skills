---
name: ppt
description: Create and read PowerPoint (.pptx) presentations with line-by-line formatting commands and built-in style themes. Use when the user asks to create a PowerPoint presentation, read/extract slide content from .pptx files, add formatted slides with headings, bullets, code blocks, tables, or images, or inspect presentation structure. Supports 5 built-in themes and slide-by-slide content creation. Do not use for .ppt (old binary format) files or PDF conversion (use the pdf skill instead).
---

# PPT

Work with PowerPoint (.pptx) files using the bundled C# helper. Create presentations slide-by-slide with explicit formatting commands, and pick a visual theme to control colors, fonts, and spacing.

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- <command> [options]
```

Prefer absolute paths for inputs and outputs when working outside the current directory.

## Start Here

1. **Identify the task**: read or create.
2. **Pick a style theme** if creating a new presentation.
3. **Run the command** with required options (`-i` for input, `-o` for output).
4. **Validate** the output file exists and looks correct.

## Commands

| Command | Purpose | Required options |
|---|---|---|
| `read` | List slide count and preview text from each slide. | `-i <pptx>` |
| `create` | Create a new .pptx from line-by-line formatting commands with a theme. | `-o <pptx> --content <lines> --from-lines` |

## Create Document

**Plain text (single slide):**
```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- create --output "report.pptx" --content "Hello\nWorld"
```

**With line-by-line formatting and a theme:**
```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- create --output "report.pptx" --content "H1 Title\nP Body" --from-lines --style modern
```

Use `\n` to separate lines. Each line starts with a **format command** followed by a space and the content.

## Line Format Commands

Each line must start with a command. Content follows after the first space.

| Command | Result |
|---------|--------|
| `H1 text` to `H6 text` | Heading (theme-colored, sized). H1-H2 use the title area; H3+ use the content area. |
| `P text` | Normal paragraph |
| `B text` | **Bold** paragraph |
| `I text` | *Italic* paragraph |
| `U text` | Underlined paragraph |
| `S text` | ~~Strikethrough~~ paragraph |
| `BI text` | ***Bold + Italic*** paragraph |
| `CODE text` | Code block (monospace font) |
| `QUOTE text` | Blockquote (italic, gray text) |
| `BULLET text` | Bulleted list item |
| `NUMBER text` | Numbered list item |
| `HR` | Horizontal rule |
| `BR` | Empty line (paragraph break). **Use sparingly** — themes already provide paragraph spacing. |
| `NEWSLIDE` | Start a new slide |
| `TABLE row1col1,row1col2;row2col1,row2col2` | Table (comma = column, semicolon = row) |
| `IMG path[,width[,height]]` | Insert image. Width can be `mm` or `N%` of slide width. Omit both for auto-fit. Omit height to keep aspect ratio. |
| `COLOR #RRGGBB` | Set color for following text |
| `SIZE N` | Set font size in points for following text |
| `FONT Name` | Set font for following text |
| `ALIGN left` / `ALIGN center` / `ALIGN right` / `ALIGN justify` | Set alignment for following paragraphs |

### Example Line Content

```
H1 Welcome
P This is a presentation about AI.
BULLET Fast
BULLET Reliable
BULLET Action-oriented
NEWSLIDE
H2 Core Features
P Our product can:
NUMBER Read files
NUMBER Execute commands
NUMBER Search the web
NEWSLIDE
H2 Code Example
CODE public async Task Run() {
CODE     var result = await DoWork();
CODE }
NEWSLIDE
H2 Thank You
P Questions?
```

## Built-in Style Themes

| Theme | Heading Color | Heading Font | Body Font | Code Font | Feel |
|-------|---------------|--------------|-----------|-----------|------|
| `default` | Blue `#2E74B5` | Helvetica Bold | Helvetica | Courier New | Standard business |
| `report` | Dark Blue `#1F4E79` | Helvetica Bold | Helvetica | Courier New | Serious corporate |
| `modern` | Cyan `#00B4D8` | Helvetica Bold | Helvetica | Courier New | Clean modern |
| `minimal` | Black `#212529` | Helvetica Bold | Helvetica | Courier New | Ultra minimal |
| `elegant` | Charcoal `#4A4A4A` | Times Bold | Times | Courier New | Elegant serif |

Each theme controls heading colors, fonts, spacing, table borders, quote styling, and code block appearance. Use `--style` on `create` to choose one. If omitted, `default` is used.

## Read Presentation

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- read -i "presentation.pptx"
```

Output: slide count and a preview of text from each slide.

## Workflow

1. **Plan**: Outline the slides and content before creating.
2. **Create**: Use `create` with `--from-lines` and line commands for rich formatting. Use `NEWSLIDE` to separate slides.
3. **Review**: Use `read` to verify slide count and content preview.
4. **Validate**: Check output file exists and opens correctly in PowerPoint.

## Tips

- Use `\n` for line breaks in command-line `--content` arguments.
- For complex presentations, write the line commands to a file first, then use `--content-file`.
- `NEWSLIDE` creates a new slide. Content before the first `NEWSLIDE` goes on slide 1.
- State commands (`COLOR`, `SIZE`, `FONT`, `ALIGN`) affect all following paragraphs until changed again.
- Commands are case-insensitive (`h1`, `H1`, `b`, `B` all work).
- When the user does not specify a style, choose one based on context: `report` for business documents, `modern` for tech content, `elegant` for formal writing, `minimal` for simple notes.

## Resources

- `scripts/ppt.cs`: bundled C# helper for PPT operations.
- `references/format.md`: PPTX format internals and limitations.

## Safety

- Read-only operations (`read`) do not modify source files.
- `create` operations create new files.
- No network operations or credential requirements.
- Image insert validates the image file exists before adding.

## Validation

After changing this skill, run:

```bash
dotnet run --file <skill-root>/tests/smoke.cs
dotnet run --file <repo-root>/skill-creator/scripts/validate.cs -- <skill-root>
```
