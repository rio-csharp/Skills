---
name: pdf
description: Work with PDF files through bundled helpers. Use when Codex needs to inspect PDF metadata, extract text, copy/remove/split/merge pages, rotate pages, add text watermarks or stamps, compress, encrypt/decrypt, read/write metadata, list bookmarks, convert images into a PDF, extract embedded images or render pages as PNG, or weave pages from one PDF into another.
---

# PDF

Use the C# helper for deterministic PDF operations:

```bash
dotnet run --file <skill-root>/scripts/pdf.cs -- <command> [options]
```

Prefer absolute paths for inputs and outputs when working outside the current directory. Do not overwrite the user's original PDF unless they explicitly ask for in-place replacement.

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
| `images` | Extract embedded images from a PDF using the Python PyMuPDF helper. | `-i <pdf> -o <dir>` |
| `render` | Render PDF pages as PNG images using the Python PyMuPDF helper. | `-i <pdf> -o <dir>` |

Unsupported by the C# helper: `images`, `render`, `pdf2img`, and `ocr`. `images` and `render` are supported by `scripts/extract_images.py`; true OCR still needs a dedicated OCR tool. Use `text` only for PDFs that already contain extractable text.

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

# Extract embedded images from a PDF.
uv run --with pymupdf python <skill-root>/scripts/extract_images.py images -i input.pdf -o images_dir

# Render PDF pages as PNG at 150 DPI.
uv run --with pymupdf python <skill-root>/scripts/extract_images.py render -i input.pdf -o pages_dir --dpi 150
```

## Option Notes

- Page ranges are 1-based and accept forms like `1`, `1-3`, `8-`, and `"1-3 5 8-"`.
- `img2pdf` treats the first image after `-i` and any following positional image paths as inputs.
- `pages --mode copy` is the default; `pages --mode remove` writes all pages except the selected range.
- `compress --level` accepts `low`, `medium`, or `max`.
- `stamp --text` supports `{n}` for current page, `{N}` for total pages, and `{d}` for the current date.
- `metadata` without write fields reads metadata; with `--title`, `--author`, `--subject`, or `--keywords`, it writes a new PDF.
- `extract_images.py` requires `uv run --with pymupdf python` (not bare `python`) so PyMuPDF is available in the uv-managed environment.

## Validation

After changing this skill, run:

```bash
dotnet run --file <skill-root>/tests/smoke.cs
dotnet run --file <repo-root>/skill-creator/scripts/validate.cs -- <skill-root>
```
