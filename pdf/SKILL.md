---
name: pdf
description: Work with PDF files through bundled helpers for metadata inspection, text extraction, page manipulation, merging, splitting, rotation, watermarking, compression, encryption, image conversion, and more. Use when the user needs to read PDF metadata, extract text or embedded images, copy/remove/reorder/split/merge pages, rotate pages, add text watermarks or stamps, compress or encrypt/decrypt PDFs, convert images to PDF, or render pages as PNG. Do not use for OCR (use a dedicated OCR tool) or creating PDFs from scratch (use the docx skill and convert if needed).
---

# PDF

Work with PDF files using the bundled C# helper for deterministic operations, and a Python helper for image extraction and page rendering.

```bash
dotnet run --file <skill-root>/scripts/pdf.cs -- <command> [options]
```

Prefer absolute paths for inputs and outputs when working outside the current directory. Do not overwrite the user's original PDF unless they explicitly ask for in-place replacement.

## Start Here

1. **Identify the task**: info, text, pages, merge, split, rotate, watermark, compress, encrypt, decrypt, metadata, bookmarks, img2pdf, weave, stamp, create, images, or render.
2. **Pick the right helper**: Most tasks use the C# helper. `images` and `render` require the Python helper (`extract_images.py`).
3. **Run the command** with required options (`-i` for input, `-o` for output).
4. **Validate** the output file exists and looks correct.

## Commands

| Command | Purpose | Required options |
|---|---|---|
| `info` | Print page count, PDF version, metadata, and size; add `-o <json>` to write JSON. | `-i <pdf>` |
| `text` | Extract embedded text with PdfPig. This is not OCR. | `-i <pdf>` |
| `pages` | Copy or remove selected pages. | `-i <pdf> --range <range> -o <pdf>` |
| `merge` | Merge PDFs in order. | `-i <pdf1> <pdf2...> -o <pdf>` |
| `split` | Extract a page range into a new PDF under an output directory. | `-i <pdf> --range <range> -o <dir>` |
| `rotate` | Rotate selected pages by 90, 180, or 270 degrees. | `-i <pdf> --angle <deg> -o <pdf>` |
| `watermark` | Add large text watermark to every page. | `-i <pdf> --text <text> -o <pdf>` |
| `compress` | Rewrite PDF with iText compression. | `-i <pdf> -o <pdf>` |
| `encrypt` | Add password protection. | `-i <pdf> --password <password> -o <pdf>` |
| `decrypt` | Remove password protection when the password is known. | `-i <pdf> --password <password> -o <pdf>` |
| `metadata` | Read metadata, or write title/author/subject/keywords to a new PDF. | `-i <pdf>` |
| `bookmarks` | List top-level bookmarks. | `-i <pdf> --list` |
| `img2pdf` | Convert image files into PDF pages. | `-i <image1> [image2...] -o <pdf>` |
| `weave` | Insert donor PDF pages into a main PDF after mapped main pages. | `-i <main.pdf> --donor <donor.pdf> --mapping <pages> -o <pdf>` |
| `stamp` | Add text such as page numbers, headers, or footers. | `-i <pdf> --text <template> -o <pdf>` |
| `create` | Create a new PDF from line-by-line formatting commands with a theme. | `-o <pdf> --content <lines> --from-lines` |
| `images` | Extract embedded images from a PDF using the Python PyMuPDF helper. | `-i <pdf> -o <dir>` |
| `render` | Render PDF pages as PNG images using the Python PyMuPDF helper. | `-i <pdf> -o <dir>` |

Unsupported by the C# helper: `pdf2img` and `ocr`. `images` and `render` are supported by `scripts/extract_images.py`; true OCR still needs a dedicated OCR tool. Use `text` only for PDFs that already contain extractable text.

## Examples

```bash
# Inspect a PDF, or write JSON with -o.
dotnet run --file <skill-root>/scripts/pdf.cs -- info -i input.pdf
dotnet run --file <skill-root>/scripts/pdf.cs -- info -i input.pdf -o info.json

# Extract embedded text.
dotnet run --file <skill-root>/scripts/pdf.cs -- text -i input.pdf -o text.txt

# Extract pages 1-3 and 5.
dotnet run --file <skill-root>/scripts/pdf.cs -- pages -i input.pdf --range "1-3 5" -o excerpt.pdf

# Remove page 1.
dotnet run --file <skill-root>/scripts/pdf.cs -- pages -i input.pdf --range 1 --mode remove -o without-first-page.pdf

# Merge PDFs in order.
dotnet run --file <skill-root>/scripts/pdf.cs -- merge -i first.pdf second.pdf third.pdf -o merged.pdf

# Split selected pages into an output directory.
dotnet run --file <skill-root>/scripts/pdf.cs -- split -i input.pdf --range "2-4" -o split-output

# Rotate all pages.
dotnet run --file <skill-root>/scripts/pdf.cs -- rotate -i input.pdf --angle 90 -o rotated.pdf

# Add a watermark.
dotnet run --file <skill-root>/scripts/pdf.cs -- watermark -i input.pdf --text "CONFIDENTIAL" -o watermarked.pdf

# Add page numbers.
dotnet run --file <skill-root>/scripts/pdf.cs -- stamp -i input.pdf --text "Page {n} of {N}" --pos footer -o stamped.pdf

# Set metadata.
dotnet run --file <skill-root>/scripts/pdf.cs -- metadata -i input.pdf --title "Report" --author "Team" -o with-metadata.pdf

# Convert images to a PDF.
dotnet run --file <skill-root>/scripts/pdf.cs -- img2pdf -i page1.png page2.jpg -o images.pdf

# Create a styled PDF from line commands.
dotnet run --file <skill-root>/scripts/pdf.cs -- create --output report.pdf --content "H1 Report\nP Summary here.\nBULLET Point 1\nBULLET Point 2" --from-lines --style modern

# Create a PDF with a table and code block.
dotnet run --file <skill-root>/scripts/pdf.cs -- create --output doc.pdf --content-file lines.txt --from-lines --style report

# Extract embedded images from a PDF.
uv run --with pymupdf python <skill-root>/scripts/extract_images.py images -i input.pdf -o images_dir

# Render PDF pages as PNG at 150 DPI.
uv run --with pymupdf python <skill-root>/scripts/extract_images.py render -i input.pdf -o pages_dir --dpi 150
```

## Workflow

1. **Create**: Use `create` with `--from-lines` to generate new styled PDFs.
2. **Inspect**: Use `info` to understand page count, version, and metadata before modifying.
3. **Extract**: Use `text` for text extraction, `images` or `render` for visual content.
4. **Manipulate**: Use `pages`, `merge`, `split`, `rotate`, or `weave` to reorganize content.
5. **Enhance**: Use `watermark`, `stamp`, or `metadata` to add annotations or properties.
6. **Protect**: Use `encrypt` or `compress` to secure or optimize the file.
7. **Validate**: Check output file exists, has expected page count, and opens correctly.

## Line Format Commands

Use `--from-lines` with `create` to build rich PDFs line-by-line. Each line starts with a command followed by a space and content.

| Command | Result |
|---------|--------|
| `H1 text` to `H6 text` | Heading (theme-colored, sized) |
| `P text` | Normal paragraph |
| `B text` | **Bold** paragraph |
| `I text` | *Italic* paragraph |
| `U text` | Underlined paragraph |
| `S text` | ~~Strikethrough~~ paragraph (simulated) |
| `BI text` | ***Bold + Italic*** paragraph |
| `CODE text` | Code block (theme background, monospace font) |
| `QUOTE text` | Blockquote with left border |
| `BULLET text` | Bulleted list item |
| `NUMBER text` | Numbered list item |
| `HR` | Horizontal rule |
| `BR` | Empty line (paragraph break). **Use sparingly** — themes already provide paragraph spacing. |
| `TABLE row1col1,row1col2;row2col1,row2col2` | Table (comma = column, semicolon = row) |
| `IMG path[,width[,height]]` | Insert image. Width can be `mm` or `N%` of page width. Omit both for auto-fit. Omit height to keep aspect ratio. |
| `COLOR #RRGGBB` | Set color for following text |
| `SIZE N` | Set font size in points for following text |
| `FONT Name` | Set font for following text (use iText7 standard font names) |
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
IMG C:\path\to\diagram.png        # auto-fit to page width
IMG C:\path\to\diagram.png,50%      # 50% of page width, auto height
IMG C:\path\to\diagram.png,80,40    # fixed 80mm x 40mm
```

## Built-in Style Themes

| Theme | Heading Color | Heading Font | Body Font | Code Font | Feel |
|-------|---------------|--------------|-----------|-----------|------|
| `default` | Blue `#2E74B5` | Helvetica Bold | Helvetica | Courier | Standard business |
| `report` | Dark Blue `#1F4E79` | Helvetica Bold | Helvetica | Courier | Serious corporate |
| `modern` | Cyan `#00B4D8` | Helvetica Bold | Helvetica | Courier | Clean modern |
| `minimal` | Black `#212529` | Helvetica Bold | Helvetica | Courier | Ultra minimal |
| `elegant` | Charcoal `#4A4A4A` | Times Bold | Times | Courier | Elegant serif |

Each theme controls heading colors, fonts, spacing, table borders, quote styling, and code block appearance. Use `--style` on `create` to choose one. If omitted, `default` is used.

## Tips

- Page ranges are 1-based and accept forms like `1`, `1-3`, `8-`, and `"1-3 5 8-"`.
- `img2pdf` treats the first image after `-i` and any following positional image paths as inputs.
- `pages --mode copy` is the default; `pages --mode remove` writes all pages except the selected range.
- `compress --level` accepts `low`, `medium`, or `max`.
- `stamp --text` supports `{n}` for current page, `{N}` for total pages, and `{d}` for the current date.
- `metadata` without write fields reads metadata; with `--title`, `--author`, `--subject`, or `--keywords`, it writes a new PDF.
- Always use `-o` to write to a new file rather than overwriting the original PDF.
- For password-protected PDFs, you must provide `--password` to both `encrypt` and `decrypt` commands.
- `create --content` uses `\n` for line breaks in command-line arguments. For complex documents, use `--content-file` to read from a file.
- `create --from-lines` supports the same command set as the docx skill (see Line Format Commands below).
- State commands (`COLOR`, `SIZE`, `FONT`, `ALIGN`) affect all following paragraphs until changed again.
- `extract_images.py` requires `uv run --with pymupdf python` (not bare `python`) so PyMuPDF is available in the uv-managed environment.

## Resources

- `scripts/pdf.cs`: bundled C# helper for core PDF operations (info, text, pages, merge, split, rotate, watermark, compress, encrypt, decrypt, metadata, bookmarks, img2pdf, weave, stamp, create).
- `scripts/extract_images.py`: bundled Python helper for image extraction and page rendering (requires PyMuPDF).
- `references/format.md`: PDF format internals, helper capabilities, and limitations.

## Safety

- Read-only operations (`info`, `text`, `bookmarks --list`) do not modify source files.
- All write operations (`pages`, `merge`, `split`, `rotate`, `watermark`, `compress`, `encrypt`, `decrypt`, `metadata`, `img2pdf`, `weave`, `stamp`, `create`) create new files or modify copies when `-o` is used.
- `extract_images.py` validates the input PDF exists before processing.
- No network operations or credential requirements.

## Validation

After changing this skill, run:

```bash
dotnet run --file <skill-root>/tests/smoke.cs
dotnet run --file <repo-root>/skill-creator/scripts/validate.cs -- <skill-root>
```
