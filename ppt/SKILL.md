---
name: ppt
description: Create, read, modify, and manipulate PowerPoint (.pptx) presentations with line-by-line formatting commands, built-in style themes, layouts, shapes, backgrounds, and slide operations. Use when the user asks to create a PowerPoint presentation, read/extract slide content from .pptx files, add formatted slides with headings, bullets, code blocks, tables, images, shapes, or backgrounds, merge/split/reorder/remove slides, add speaker notes, or inspect presentation structure. Supports 5 built-in themes, 7 slide layouts, drawing shapes, and full slide manipulation. Do not use for .ppt (old binary format) files or PDF conversion (use the pdf skill instead).
---

# PPT

Work with PowerPoint (.pptx) files using the bundled C# helper. Create presentations slide-by-slide with explicit formatting commands, pick a visual theme, choose slide layouts, add shapes and backgrounds, and manipulate existing presentations.

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- <command> [options]
```

Prefer absolute paths for inputs and outputs when working outside the current directory.

## Start Here

1. **Identify the task**: read, create, modify, merge, split, remove, reorder, notes, extract-notes, or set-properties.
2. **Pick a style theme** if creating a new presentation.
3. **Choose a layout** for each slide (default is `title-content`).
4. **Run the command** with required options (`-i` for input, `-o` for output).
5. **Validate** the output file exists and looks correct.

## Commands

| Command | Purpose | Required options |
|---|---|---|
| `read` | List slide count and preview text from each slide. | `-i <pptx>` |
| `create` | Create a new .pptx from line-by-line formatting commands with a theme. | `-o <pptx> --content <lines> --from-lines` |
| `modify` | Append new slides to an existing .pptx from line format. | `-i <pptx> -o <pptx> --content <lines> --from-lines` |
| `merge` | Merge multiple .pptx files in order. | `-i <pptx1> <pptx2...> -o <pptx>` |
| `split` | Extract page range into separate .pptx files under output dir. | `-i <pptx> --range <range> -o <dir>` |
| `remove` | Remove selected slides. | `-i <pptx> --range <range> -o <pptx>` |
| `reorder` | Reorder slides. | `-i <pptx> --order <1,3,2...> -o <pptx>` |
| `notes` | Add speaker notes to a specific slide. | `-i <pptx> --slide N --content "text" -o <pptx>` |
| `extract-notes` | Extract all speaker notes to a text file. | `-i <pptx> -o <txt>` |
| `set-properties` | Set title, author, subject, keywords. | `-i <pptx> [--title X] [--author Y] ...` |
| `export` | Export .pptx to PDF (requires LibreOffice or PowerPoint). | `-i <pptx> -o <pdf>` |

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

### Text Commands

| Command | Result |
|---------|--------|
| `H1 text` to `H6 text` | Heading (theme-colored, sized). H1-H2 prefer the title area; H3+ prefer the content area. |
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
| `IMG path[,width[,height]]` | Insert image. Width/height can be `mm` or `N%` of slide. Omit both for auto-fit. |

### Style State Commands

| Command | Result |
|---------|--------|
| `COLOR #RRGGBB` | Set color for following text |
| `SIZE N` | Set font size in points for following text |
| `FONT Name` | Set font for following text |
| `ALIGN left` / `ALIGN center` / `ALIGN right` / `ALIGN justify` | Set alignment for following paragraphs |

### Layout Commands

| Command | Result |
|---------|--------|
| `LAYOUT name` | Set layout for current/next slide. See Layouts table below. |
| `POS x,y,w,h` | Position next shape explicitly. Values in EMU, `N%`, or `Nmm`. Resets after each shape. |

### Background Commands

| Command | Result |
|---------|--------|
| `BGCOLOR #RRGGBB` | Set solid background color for current slide |
| `BGIMAGE path` | Set background image for current slide |

### Shape Commands

| Command | Result |
|---------|--------|
| `SHAPE rect x,y,w,h` | Rectangle. Use `ROUND` before this for rounded corners. |
| `SHAPE ellipse x,y,w,h` | Ellipse / circle |
| `SHAPE line x1,y1,x2,y2` | Straight line |
| `SHAPE arrow x1,y1,x2,y2` | Arrow line with arrowhead at end |
| `FILL #RRGGBB` | Set fill color for next shape |
| `STROKE #RRGGBB,N` | Set stroke color and width (pt) for next shape |
| `ROUND N` | Enable rounded corners for next rectangle (radius in pt, approximate) |
| `CHART type data` | Insert chart. `type` = bar, column, pie, line, area. Data: `Title;Cat1,Cat2;Val1,Val2` |
| `ANIMATE type` | Add entrance animation to last shape (fade, flyin, appear) |
| `TRANSITION type` | Set slide transition effect (fade, push, wipe, split, etc.) |

### Layouts

| Layout | Description |
|--------|-------------|
| `title-content` | Title (top) + Content (center-bottom). **Default.** |
| `title` | Full-slide large title only |
| `title-only` | Title only, larger content area |
| `two-content` | Title + Left column + Right column |
| `comparison` | Title + Left header + Left content + Right header + Right content |
| `section-header` | Large centered title + subtitle |
| `blank` | No default shapes (use `POS` or `SHAPE`) |

### Example Line Content

```
LAYOUT title-only
H1 Welcome
NEWSLIDE
LAYOUT title-content
H2 Core Features
P Our product can:
BULLET Fast
BULLET Reliable
BULLET Action-oriented
NEWSLIDE
LAYOUT two-content
H2 Comparison
P Left side content
P More left content
NEWSLIDE
LAYOUT blank
BGCOLOR #F0F0F0
SHAPE rect 10%,10%,80%,20%
FILL #2E74B5
STROKE #000000,2
SHAPE ellipse 40%,40%,20%,20%
FILL #FF0000
NEWSLIDE
H2 Sales Report
CHART bar Sales;Q1,Q2,Q3,Q4;120,150,180,200
NEWSLIDE
H2 Market Share
CHART pie Share;A,B,C;40,35,25
NEWSLIDE
H2 Thank You
P Questions?
ANIMATE fade
TRANSITION fade
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

## Modify Presentation

Append slides to an existing presentation:

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- modify -i "existing.pptx" -o "updated.pptx" --content "NEWSLIDE\nH1 New Section\nP New content" --from-lines
```

## Merge Presentations

Combine multiple .pptx files in order:

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- merge -i "part1.pptx" "part2.pptx" "part3.pptx" -o "combined.pptx"
```

## Split Presentation

Extract specific slides into separate files:

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- split -i "presentation.pptx" --range "1-3,5" -o "output_dir/"
```

## Remove Slides

Delete selected slides:

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- remove -i "presentation.pptx" --range "2,4" -o "trimmed.pptx"
```

## Reorder Slides

Change slide order:

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- reorder -i "presentation.pptx" --order "3,1,2" -o "reordered.pptx"
```

## Speaker Notes

Add notes to a slide:

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- notes -i "presentation.pptx" --slide 1 --content "Remember to mention Q3 results" -o "with_notes.pptx"
```

Extract all notes:

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- extract-notes -i "presentation.pptx" -o "notes.txt"
```

## Set Properties

Update document metadata:

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- set-properties -i "presentation.pptx" --title "Annual Report" --author "Jane Doe" -o "updated.pptx"
```

## Export to PDF

Convert a .pptx to PDF (requires LibreOffice or Microsoft PowerPoint):

```bash
dotnet run --file <skill-root>/scripts/ppt.cs -- export -i "presentation.pptx" -o "output.pdf"
```

**Fallback priority:**
1. LibreOffice (`soffice --headless --convert-to pdf`) — fastest, works on all platforms
2. PowerPoint COM automation — Windows only, requires Office installed

If neither is available, install LibreOffice and ensure `soffice` is in your PATH.

## Workflow

1. **Plan**: Outline the slides, layouts, and content before creating.
2. **Create**: Use `create` with `--from-lines` and line commands for rich formatting. Use `NEWSLIDE` to separate slides and `LAYOUT` to control slide structure.
3. **Enhance**: Add shapes, backgrounds, and images for visual impact.
4. **Manipulate**: Use `modify`, `merge`, `remove`, `reorder` to adjust existing presentations.
5. **Review**: Use `read` to verify slide count and content preview.
6. **Validate**: Check output file exists and opens correctly in PowerPoint.

## Tips

- Use `\n` for line breaks in command-line `--content` arguments.
- For complex presentations, write the line commands to a file first, then use `--content-file`.
- `NEWSLIDE` creates a new slide. Content before the first `NEWSLIDE` goes on slide 1.
- State commands (`COLOR`, `SIZE`, `FONT`, `ALIGN`, `LAYOUT`, `FILL`, `STROKE`, `ROUND`) affect all following elements until changed again. `ANIMATE` and `TRANSITION` apply immediately to the current slide/shape.
- Commands are case-insensitive (`h1`, `H1`, `b`, `B` all work).
- `POS` affects the next shape only (text or drawing). If the target shape already exists from a layout, POS is ignored for text shapes.
- `SHAPE` commands always create new shapes and support `POS` as an alternative to inline coordinates.
- When the user does not specify a style, choose one based on context: `report` for business documents, `modern` for tech content, `elegant` for formal writing, `minimal` for simple notes.

## Resources

- `scripts/ppt.cs`: bundled C# helper for PPT operations.
- `references/format.md`: PPTX format internals and limitations.

## Safety

- Read-only operations (`read`, `extract-notes`) do not modify source files.
- `create` operations create new files.
- `modify`, `merge`, `remove`, `reorder`, `notes`, `set-properties` write to the output file (copy input if different from output).
- No network operations or credential requirements.
- Image insert validates the image file exists before adding.

## Validation

After changing this skill, run:

```bash
dotnet run --file <skill-root>/tests/smoke.cs
dotnet run --file <repo-root>/skill-creator/scripts/validate.cs -- <skill-root>
```
