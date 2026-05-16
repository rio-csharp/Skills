# PDF Format Internals

PDF (Portable Document Format) is a binary file format developed by Adobe. Key points for working with the bundled helpers:

## Structure Overview

A PDF file consists of:
- **Header**: PDF version (e.g., `%PDF-1.4`)
- **Body**: A sequence of indirect objects (pages, fonts, images, annotations, etc.)
- **Cross-Reference Table (XRef)**: Offsets to each object for fast random access
- **Trailer**: Metadata (size, root catalog, info dictionary) and startxref offset

PDFs are not text files. Editing them with a text editor will corrupt the XRef table and render the file unreadable.

## Bundled Helper Capabilities

The `pdf.cs` C# helper supports:

- **Metadata inspection** (`info`): page count, version, title, author, subject, keywords, creator, producer, creation/modification dates
- **Text extraction** (`text`): extract embedded text using PdfPig (not OCR)
- **Page manipulation** (`pages`, `merge`, `split`, `rotate`, `weave`): copy, remove, reorder, split, merge, and rotate pages using iText7
- **Watermarking and stamping** (`watermark`, `stamp`): add text overlays with template variables
- **Compression** (`compress`): rewrite with iText7 compression levels (low, medium, max)
- **Encryption/Decryption** (`encrypt`, `decrypt`): password-protect or unlock PDFs using 128-bit RC4
- **Metadata editing** (`metadata`): read or write title, author, subject, keywords
- **Bookmark listing** (`bookmarks --list`): read top-level outline/bookmarks
- **Image-to-PDF** (`img2pdf`): convert PNG/JPEG images to PDF pages using ImageSharp + iText7
- **PDF Creation** (`create`): generate styled PDFs from line-by-line formatting commands with built-in themes (headings, paragraphs, bold/italic, code blocks, quotes, lists, tables, images, colors, fonts, alignment)

The `extract_images.py` Python helper supports:

- **Image extraction** (`images`): extract embedded images from PDF pages using PyMuPDF
- **Page rendering** (`render`): rasterize PDF pages to PNG images at configurable DPI using PyMuPDF

## PDF Version Compatibility

- The C# helper uses **iText7** (v9.6.0) for write operations and **PdfPig** (v0.1.14) for text extraction.
- Supported PDF versions: 1.0 through 2.0 (most common: 1.4, 1.7).
- iText7 may upgrade the PDF version when rewriting (e.g., compression or encryption).

## Text Extraction Limitations

- `text` extracts **embedded text only**. Scanned documents (image-based PDFs) contain no extractable text.
- Text order may not match visual reading order, especially for multi-column or complex layouts.
- For scanned PDFs, use a dedicated OCR tool (e.g., Tesseract) instead.

## Image Handling

- **Embedded images** in PDFs use various compression formats: DCT (JPEG), CCITT (fax), JPX (JPEG2000), FlateDecode (ZIP), and raw bitmaps.
- The C# helper (`img2pdf`) converts **external image files** (PNG, JPEG, GIF, BMP, TIFF) into PDF pages.
- The Python helper (`extract_images.py`) extracts **embedded images** from existing PDFs.
- Page rendering (`render`) rasterizes the entire page (text + vector + images) to a PNG bitmap.

## Encryption

- The helper uses 128-bit RC4 encryption (`iText7 WriterProperties.SetStandardEncryption`).
- Owner password grants full permissions; user password restricts access based on permission flags.
- The helper sets all permissions (`0xFFFFFF`) when encrypting.

## Metadata Access

- Standard metadata fields: Title, Author, Subject, Keywords, Creator, Producer.
- Dates are stored in PDF-specific format (`D:YYYYMMDDHHmmSSOHH'mm'`).
- The helper normalizes dates to string representation in JSON output.

## Limitations

- No OCR capability. Image-based PDFs require external OCR tools.
- No interactive form (AcroForm) editing.
- No digital signature creation or validation.
- `bookmarks --add` is not supported; only `--list` works.
- `split --bookmarks` is not supported; use `--range` instead.
- Image extraction and page rendering require PyMuPDF (Python); no equivalent C# implementation is bundled.
