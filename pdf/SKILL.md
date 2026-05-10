---
name: pdf
description: Manipulate PDF files — extract text/images/pages, merge, split, rotate, watermark, compress, encrypt, convert images, OCR, and more. Use when the user asks to work with PDF files: extracting, merging, splitting, converting, annotating, signing, or any PDF operation. Supports Windows.
---

# PDF Skill

All PDF operations go through the Python script. PyMuPDF (via `pymupdf`) powers most operations; `pdfplumber` handles layout-aware text extraction.

**Script:** `.venv/Scripts/python.exe "<skill-base>/scripts/pdf.py" <command> [options]`

The script auto-creates a venv at `D:\Repos\Skills\.venv` on first run. If the venv exists but libraries are missing, run: `.venv/Scripts/python.exe -m pip install pymupdf pdfplumber`

## Core Commands

| Command | Description |
|---------|-------------|
| `info` | Show page count, metadata, file size |
| `text` | Extract text (plain or layout-aware via pdfplumber) |
| `images` | Extract embedded images |
| `pages` | Extract specific pages or page ranges |
| `merge` | Merge multiple PDFs into one |
| `split` | Split PDF by page ranges or bookmarks |
| `rotate` | Rotate pages by angle |
| `watermark` | Add text or image watermark |
| `compress` | Compress PDF and reduce file size |
| `encrypt` | Add password protection |
| `decrypt` | Remove password protection |
| `metadata` | Read or write PDF metadata |
| `bookmarks` | List, add, or edit bookmarks (outline) |
| `pdf2img` | Convert PDF pages to images |
| `img2pdf` | Convert images to PDF |
| `ocr` | Run OCR on PDF pages |
| `weave` | Insert pages from one PDF into another |
| `stamp` | Add text stamp (header/footer/page numbers) |

## Common Options

- `--input`, `-i`: Input PDF path (required for most commands)
- `--output`, `-o`: Output path (default: auto-generated with `_out` suffix)
- `--password`, `-p`: Password for encrypted PDFs

## Examples

```bash
# Extract text from PDF
python pdf.py text -i document.pdf

# Extract text preserving layout (pdfplumber)
python pdf.py text -i document.pdf --layout

# Extract a specific page range
python pdf.py pages -i document.pdf --range 1-5 -o pages_1_5.pdf

# Merge multiple PDFs
python pdf.py merge -i doc1.pdf doc2.pdf doc3.pdf -o merged.pdf

# Split at bookmark level 1 headings
python pdf.py split -i document.pdf --bookmarks -o split_dir/

# Compress with maximum reduction
python pdf.py compress -i large.pdf --level=max -o small.pdf

# Add text watermark
python pdf.py watermark -i document.pdf --text "CONFIDENTIAL" --opacity 0.3 -o marked.pdf

# Encrypt with password
python pdf.py encrypt -i document.pdf --password secret123 -o secured.pdf

# Convert to images at 300 DPI
python pdf.py pdf2img -i document.pdf --dpi 300 --output images/

# Run OCR on scanned PDF
python pdf.py ocr -i scanned.pdf --lang eng --output text.txt
```

## Operations Reference

### `info`
Shows: page count, PDF version, title, author, subject, keywords, creator, producer, creation/modification dates, file size.

### `text`
- `--layout`: Use pdfplumber for layout-aware extraction (better for tables/columns)
- `--page`: Extract from specific page only
- `--fmt`: Output format — `txt` (plain), `md` (markdown), `json`

### `images`
Extracts all embedded images. Output dir contains numbered image files with metadata JSON.

### `pages`
- `--range`: Page range, e.g. `1-3`, `5`, `8-`
- `--mode`: `copy` (extract pages) or `remove` (remove those pages)

### `merge`
Input order is preserved. Use `--shuffle` to sort by filename.

### `split`
- `--range`: e.g. `1-3 5 8-` (pages 1-3, page 5, page 8 to end)
- `--bookmarks`: Split at each top-level bookmark, one file per bookmark
- `--size`: Split by approximate file size (MB), producing multiple files

### `rotate`
- `--angle`: 90, 180, or 270
- `--range`: Pages to rotate (default: all)

### `watermark`
- `--text`: Text watermark string
- `--image`: Image file path for image watermark
- `--opacity`: 0.0–1.0
- `--angle`: Text rotation in degrees
- `--pos`: `center`, `tile`, or `x,y` offset

### `compress`
- `--level`: `low` (fast), `medium`, `max` (slowest but smallest)

### `encrypt`
- `--password`: User password (required)
- `--owner`: Owner password (default: same as user)
- `--bits`: 40, 128, or 256 (encryption strength)

### `decrypt`
- `--password`: Password to unlock (required)

### `metadata`
- Read: `python pdf.py metadata -i file.pdf`
- Write: `python pdf.py metadata -i file.pdf --title "New Title" --author "Author Name" ...`

### `bookmarks`
- `--list`: Show all bookmarks/outline
- `--add`: Add bookmark — `--title "Chapter 1" --page 5 --level 1`
- `--remove`: Remove bookmark by title

### `pdf2img`
- `--dpi`: Resolution (default: 150)
- `--format`: `png` or `jpg` (default: `png`)
- `--pages`: Specific pages (default: all)
- `--output`: Output directory or pattern like `page_{n}.png`

### `img2pdf`
Input can be a folder of images or glob pattern. Output is a single PDF.

### `ocr`
- `--lang`: Tesseract language code(s), e.g. `eng`, `chi_sim+eng` (default: `eng`)
- `--output`: Output .txt file
- `--preprocess`: Apply image preprocessing (binarize, deskew) before OCR

### `weave`
Insert pages from a "donor" PDF into the main PDF at specified positions.

### `stamp`
- `--text`: Stamp text (e.g. page number, date, custom string)
- `--template`: Use patterns like `{n}` (page num), `{N}` (total pages), `{d}` (date)
- `--pos`: `header`, `footer`, `center`
- `--font`: Font size (default: auto based on position)

## Important Notes

- All file paths are absolute or relative to current working directory.
- Image watermarks support PNG with transparency.
- Encrypted PDFs must be decrypted before most operations.
- `--output` is required when input is also output (in-place not allowed).
- Large PDFs: use `--lazy` flag to process pages on-demand (lower memory).
