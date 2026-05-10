#!/usr/bin/env python3
"""
PDF Tool — comprehensive PDF manipulation using PyMuPDF and pdfplumber.
Usage: python pdf.py <command> [options]

Auto-creates/uses venv at D:\\Repos\\Skills\\.venv
"""

import sys
import os
import argparse
import json
import shutil
import tempfile
from pathlib import Path

# ── venv bootstrap ──────────────────────────────────────────────

VENV_PYTHON = Path(r"D:\Repos\Skills\.venv\Scripts\python.exe")
SCRIPT_DIR = Path(__file__).parent.resolve()
VENV_DIR = SCRIPT_DIR.parent

def ensure_venv():
    """Ensure venv exists with required packages."""
    if not VENV_PYTHON.exists():
        import subprocess
        print("Creating venv...", file=sys.stderr)
        subprocess.run([sys.executable, "-m", "uv", "venv", str(VENV_DIR)],
                      check=True, capture_output=True)
        subprocess.run([str(VENV_PYTHON), "-m", "pip", "install",
                        "pymupdf", "pdfplumber"], check=True, capture_output=True)
        print("Packages installed.", file=sys.stderr)

    # check packages
    try:
        import pymupdf
        return
    except ImportError:
        import subprocess
        subprocess.run([str(VENV_PYTHON), "-m", "pip", "install",
                        "pymupdf", "pdfplumber"], check=True, capture_output=True)

# ── CLI ──────────────────────────────────────────────────────────

def parse_args():
    parser = argparse.ArgumentParser(prog="pdf", description="PDF tool")
    sub = parser.add_subparsers(dest="cmd", required=True)

    # info
    p = sub.add_parser("info", help="Show PDF info")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("-o", "--output")  # optional JSON output

    # text
    p = sub.add_parser("text", help="Extract text")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--layout", action="store_true")
    p.add_argument("--page", type=int)
    p.add_argument("--fmt", default="txt", choices=["txt", "md", "json"])
    p.add_argument("-o", "--output")

    # images
    p = sub.add_parser("images", help="Extract images")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("-o", "--output", default=None)

    # pages
    p = sub.add_parser("pages", help="Extract pages")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--range", default="1-")
    p.add_argument("--mode", default="copy", choices=["copy", "remove"])
    p.add_argument("-o", "--output", required=True)

    # merge
    p = sub.add_parser("merge", help="Merge PDFs")
    p.add_argument("-i", "--input", nargs="+", required=True)
    p.add_argument("-o", "--output", required=True)
    p.add_argument("--shuffle", action="store_true")

    # split
    p = sub.add_parser("split", help="Split PDF")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--range", help="e.g. 1-3 5 8-")
    p.add_argument("--bookmarks", action="store_true")
    p.add_argument("-o", "--output", required=True)

    # rotate
    p = sub.add_parser("rotate", help="Rotate pages")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--angle", type=int, required=True, choices=[90, 180, 270])
    p.add_argument("--range")
    p.add_argument("-o", "--output", required=True)

    # watermark
    p = sub.add_parser("watermark", help="Add watermark")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--text")
    p.add_argument("--image")
    p.add_argument("--opacity", type=float, default=0.3)
    p.add_argument("--angle", type=float, default=45)
    p.add_argument("--pos", default="center")
    p.add_argument("-o", "--output", required=True)

    # compress
    p = sub.add_parser("compress", help="Compress PDF")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--level", default="medium", choices=["low", "medium", "max"])
    p.add_argument("-o", "--output", required=True)

    # encrypt
    p = sub.add_parser("encrypt", help="Encrypt PDF")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--password", required=True)
    p.add_argument("--owner")
    p.add_argument("--bits", type=int, default=128, choices=[40, 128, 256])
    p.add_argument("-o", "--output", required=True)

    # decrypt
    p = sub.add_parser("decrypt", help="Decrypt PDF")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--password", required=True)
    p.add_argument("-o", "--output", required=True)

    # metadata
    p = sub.add_parser("metadata", help="Read/write metadata")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("-o", "--output")
    p.add_argument("--title")
    p.add_argument("--author")
    p.add_argument("--subject")
    p.add_argument("--keywords")
    p.add_argument("--creator")

    # bookmarks
    p = sub.add_parser("bookmarks", help="Manage bookmarks")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--list", action="store_true")
    p.add_argument("--add", action="store_true")
    p.add_argument("--title")
    p.add_argument("--page", type=int)
    p.add_argument("--level", type=int, default=1)
    p.add_argument("--remove")

    # pdf2img
    p = sub.add_parser("pdf2img", help="Convert PDF to images")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--dpi", type=int, default=150)
    p.add_argument("--format", default="png", choices=["png", "jpg"])
    p.add_argument("--pages")
    p.add_argument("--output", default=None)

    # img2pdf
    p = sub.add_parser("img2pdf", help="Convert images to PDF")
    p.add_argument("-i", "--input", nargs="+", required=True)
    p.add_argument("-o", "--output", required=True)

    # ocr
    p = sub.add_parser("ocr", help="OCR on PDF")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--lang", default="eng")
    p.add_argument("--output", required=True)
    p.add_argument("--preprocess", action="store_true")
    p.add_argument("--pages")

    # weave
    p = sub.add_parser("weave", help="Weave pages from donor into main PDF")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--donor", required=True)
    p.add_argument("--mapping", required=True)  # e.g. "1,2,3" — insert at page 1, donor pages 2,3
    p.add_argument("-o", "--output", required=True)

    # stamp
    p = sub.add_parser("stamp", help="Add page stamps")
    p.add_argument("-i", "--input", required=True)
    p.add_argument("--text")
    p.add_argument("--template", default="{n}")
    p.add_argument("--pos", default="footer")
    p.add_argument("--font", type=int, default=10)
    p.add_argument("-o", "--output", required=True)

    return parser.parse_args()

# ── Helpers ─────────────────────────────────────────────────────

def error(msg):
    print(f"ERROR: {msg}", file=sys.stderr)
    sys.exit(1)

def ensure_input(path_str):
    p = Path(path_str).resolve()
    if not p.exists():
        error(f"Input file not found: {path_str}")
    return p

def ensure_output_dir(path_str):
    p = Path(path_str).resolve()
    if p.exists() and p.is_dir():
        return p
    p.parent.mkdir(parents=True, exist_ok=True)
    return p

def parse_range(range_str, total_pages):
    """Parse page range string like '1-3,5,8-' into list of 0-based indices."""
    pages = []
    if not range_str:
        return list(range(total_pages))
    for part in range_str.replace(",", " ").split():
        if "-" in part:
            start, end = part.split("-", 1)
            start = int(start) if start else 1
            end = int(end) if end else total_pages
            pages.extend(range(start - 1, end))
        else:
            pages.append(int(part) - 1)
    return pages

def output_path(input_path, suffix, output_str):
    if output_str:
        p = Path(output_str).resolve()
        p.parent.mkdir(parents=True, exist_ok=True)
        return p
    p = Path(input_path).resolve()
    stem = p.stem
    ext = p.suffix
    return p.parent / f"{stem}{suffix}{ext}"

# ── Commands ─────────────────────────────────────────────────────

def cmd_info(args):
    import pymupdf
    doc = pymupdf.open(args.input)
    size_kb = Path(args.input).stat().st_size / 1024
    meta = doc.metadata
    result = {
        "file": str(Path(args.input).resolve()),
        "pages": doc.page_count,
        "version": doc.metadata.get("format", "").replace("PDF ", ""),
        "size_kb": round(size_kb, 1),
        "title": meta.get("title", ""),
        "author": meta.get("author", ""),
        "subject": meta.get("subject", ""),
        "keywords": meta.get("keywords", ""),
        "creator": meta.get("creator", ""),
        "producer": meta.get("producer", ""),
        "creationDate": str(meta.get("creationDate", "")),
        "modDate": str(meta.get("modDate", "")),
    }
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            json.dump(result, f, indent=2, ensure_ascii=False)
        print(f"Info written to: {args.output}")
    else:
        for k, v in result.items():
            print(f"  {k}: {v}")
    doc.close()

def cmd_text(args):
    import pymupdf

    if args.layout:
        import pdfplumber
        with pdfplumber.open(args.input) as pdf:
            pages = [pdf.pages] if args.page else pdf.pages
            if args.page:
                pages = [pdf.pages[args.page - 1]]
            texts = []
            for pg in pages:
                txt = pg.extract_text(layout=True)
                if txt:
                    texts.append(txt)
        content = "\n\n".join(texts)
    else:
        doc = pymupdf.open(args.input)
        pages = [args.page - 1] if args.page else range(doc.page_count)
        texts = []
        for i in pages:
            page = doc[i]
            blocks = page.get_text("text")
            texts.append(blocks)
        content = "\n\n".join(texts)
        doc.close()

    if args.fmt == "json":
        out = {"text": content}
        print(json.dumps(out, indent=2, ensure_ascii=False))
    elif args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"Text written to: {args.output}")
    else:
        print(content)

def cmd_images(args):
    import pymupdf

    out_dir = Path(args.output) if args.output else Path(args.input).parent / f"{Path(args.input).stem}_images"
    out_dir.mkdir(parents=True, exist_ok=True)

    doc = pymupdf.open(args.input)
    img_count = 0
    meta = []

    for pnum in range(doc.page_count):
        page = doc[pnum]
        imgs = page.get_images(full=True)
        for img in imgs:
            xref = img[0]
            try:
                base_image = doc.extract_image(xref)
                ext = base_image["ext"]
                img_data = base_image["image"]
                fname = f"p{pnum+1}_{xref}.{ext}"
                fpath = out_dir / fname
                with open(fpath, "wb") as f:
                    f.write(img_data)
                img_count += 1
                meta.append({
                    "file": str(fpath),
                    "page": pnum + 1,
                    "xref": xref,
                    "width": base_image.get("width"),
                    "height": base_image.get("height"),
                    "ext": ext,
                })
            except Exception as e:
                print(f"  Warning: could not extract image xref={xref}: {e}", file=sys.stderr)

    doc.close()

    meta_file = out_dir / "_images_meta.json"
    with open(meta_file, "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=2, ensure_ascii=False)

    print(f"Extracted {img_count} images to: {out_dir}")

def cmd_pages(args):
    import pymupdf

    doc = pymupdf.open(args.input)
    total = doc.page_count
    indices = parse_range(args.range, total)

    if args.mode == "remove":
        new_doc = pymupdf.open(args.input)
        for i in sorted(indices, reverse=True):
            if 0 <= i < new_doc.page_count:
                new_doc.delete_page(i)
        out_p = ensure_output_dir(args.output)
        new_doc.save(out_p)
        new_doc.close()
    else:
        new_doc = pymupdf.open(args.input)
        out_p = ensure_output_dir(args.output)
        new_doc.select(indices)
        new_doc.save(out_p)
        new_doc.close()

    doc.close()
    print(f"{'Removed' if args.mode == 'remove' else 'Extracted'} {len(indices)} pages to: {out_p}")

def cmd_merge(args):
    import pymupdf

    paths = [ensure_input(p) for p in args.input]
    if args.shuffle:
        paths.sort(key=lambda p: p.stem)

    out_p = ensure_output_dir(args.output)
    writer = pymupdf.open()

    for p in paths:
        src = pymupdf.open(p)
        writer.insert_pdf(src)
        src.close()

    writer.save(out_p, encryption=0)
    writer.close()
    print(f"Merged {len(paths)} PDFs into: {out_p}")

def cmd_split(args):
    import pymupdf

    doc = pymupdf.open(args.input)
    out_dir = ensure_output_dir(args.output)
    out_dir.mkdir(parents=True, exist_ok=True)

    if args.bookmarks:
        toc = doc.get_toc()
        if not toc:
            error("No bookmarks found in PDF")

        for entry in toc:
            level, title, page_num = entry[0], entry[1], entry[2]
            if level == 1:
                # Find next entry at same level or end
                start = page_num
                end = doc.page_count - 1
                fname = sanitize_filename(title) if title else f"page_{start+1}"
                out_p = out_dir / f"{fname}.pdf"
                sub = pymupdf.open(args.input)
                sub.select(list(range(start, end)))
                sub.save(out_p, encryption=0)
                sub.close()
                print(f"  Saved: {out_p.name} (pages {start+1}-{end+1})")
    elif args.range:
        indices = parse_range(args.range, doc.page_count)
        sub = pymupdf.open(args.input)
        sub.select(indices)
        out_p = out_dir / f"{Path(args.input).stem}_pages.pdf"
        sub.save(out_p, encryption=0)
        sub.close()
        print(f"  Saved {len(indices)} pages to: {out_p}")
    else:
        error("Provide --range or --bookmarks")

    doc.close()

def cmd_rotate(args):
    import pymupdf

    doc = pymupdf.open(args.input)
    total = doc.page_count
    indices = parse_range(args.range, total) if args.range else list(range(total))

    for i in indices:
        page = doc[i]
        page.set_rotation(args.angle)

    out_p = ensure_output_dir(args.output)
    doc.save(out_p, encryption=0)
    doc.close()
    print(f"Rotated {len(indices)} pages by {args.angle}° -> {out_p}")

def cmd_watermark(args):
    import pymupdf

    if not args.text and not args.image:
        error("Provide --text or --image for watermark")

    doc = pymupdf.open(args.input)
    out_p = ensure_output_dir(args.output)

    for page_num in range(doc.page_count):
        page = doc[page_num]
        rect = page.rect

        if args.text:
            font_size = min(rect.width / (len(args.text) * 0.5), rect.height * 0.3)
            if font_size < 8:
                font_size = 8

            if args.pos == "tile":
                step_x = rect.width / 3
                step_y = rect.height / 3
                for row in range(3):
                    for col in range(3):
                        x = col * step_x
                        y = row * step_y
                        rc = pymupdf.Rect(x, y, x + rect.width / 3, y + rect.height / 3)
                        page.add_freetext_annot(
                            rc,
                            args.text,
                            fontsize=font_size,
                            text_color=(0.5, 0.5, 0.5),
                            fill_color=(0.7, 0.7, 0.7),
                            opacity=args.opacity,
                        )
            else:
                cx = rect.x0 + rect.width / 2 - 100
                cy = rect.y0 + rect.height / 2 - 20
                rc = pymupdf.Rect(cx, cy, cx + 200, cy + 40)
                page.add_freetext_annot(
                    rc,
                    args.text,
                    fontsize=font_size,
                    text_color=(0.5, 0.5, 0.5),
                    fill_color=(0.7, 0.7, 0.7),
                    opacity=args.opacity,
                )
        elif args.image:
            img_path = ensure_input(args.image)
            img_doc = pymupdf.open(img_path)
            if img_doc.page_count > 0:
                page.show_pdf_page(page.rect, img_doc, 0, overlay=True)
            img_doc.close()

    page_count = doc.page_count
    doc.save(out_p)
    doc.close()
    print(f"Watermarked {page_count} pages -> {out_p}")

def cmd_compress(args):
    import pymupdf

    doc = pymupdf.open(args.input)
    out_p = ensure_output_dir(args.output)

    effort = {"low": 1, "medium": 5, "max": 9}.get(args.level, 5)
    gc = {"low": 1, "medium": 3, "max": 4}.get(args.level, 3)

    doc.save(
        out_p,
        garbage=gc,
        deflate=True,
        deflate_images=True,
        deflate_fonts=True,
        compression_effort=effort,
        encryption=0,
    )
    doc.close()

    orig_size = Path(args.input).stat().st_size
    new_size = out_p.stat().st_size
    ratio = (1 - new_size / orig_size) * 100
    print(f"Compressed {orig_size/1024:.0f} KB -> {new_size/1024:.0f} KB ({ratio:.1f}% reduction) -> {out_p}")

def cmd_encrypt(args):
    import pymupdf

    doc = pymupdf.open(args.input)
    out_p = ensure_output_dir(args.output)
    owner_pw = args.owner or args.password

    enc_map = {40: pymupdf.PDF_ENCRYPT_RC4_40, 128: pymupdf.PDF_ENCRYPT_RC4_128, 256: pymupdf.PDF_ENCRYPT_AES_256}
    enc = enc_map.get(args.bits, pymupdf.PDF_ENCRYPT_RC4_128)

    doc.save(
        out_p,
        encryption=enc,
        owner_pw=owner_pw,
        user_pw=args.password,
    )
    doc.close()
    print(f"Encrypted ({args.bits}-bit) -> {out_p}")

def cmd_decrypt(args):
    import pymupdf

    doc = pymupdf.open(args.input)
    auth_result = doc.authenticate(args.password)
    if auth_result == 0:
        error("Invalid password")
    out_p = ensure_output_dir(args.output)
    doc.save(out_p)
    try:
        doc.close()
    except Exception:
        pass
    print(f"Decrypted -> {out_p}")

def cmd_metadata(args):
    import pymupdf

    doc = pymupdf.open(args.input)
    meta = doc.metadata

    result = {
        "title": meta.get("title", ""),
        "author": meta.get("author", ""),
        "subject": meta.get("subject", ""),
        "keywords": meta.get("keywords", ""),
        "creator": meta.get("creator", ""),
        "producer": meta.get("producer", ""),
        "creationDate": str(meta.get("creationDate", "")),
        "modDate": str(meta.get("modDate", "")),
    }

    if any([args.title, args.author, args.subject, args.keywords, args.creator]):
        if args.title is not None: meta["title"] = args.title
        if args.author is not None: meta["author"] = args.author
        if args.subject is not None: meta["subject"] = args.subject
        if args.keywords is not None: meta["keywords"] = args.keywords
        if args.creator is not None: meta["creator"] = args.creator
        doc.set_metadata(meta)
        out_p = Path(args.output) if args.output else output_path(args.input, "_meta", None)
        out_p = ensure_output_dir(out_p)
        doc.save(out_p, encryption=0)
        doc.close()
        print(f"Metadata updated -> {out_p}")
    else:
        doc.close()
        if args.output:
            with open(args.output, "w", encoding="utf-8") as f:
                json.dump(result, f, indent=2, ensure_ascii=False)
            print(f"Metadata written to: {args.output}")
        else:
            for k, v in result.items():
                print(f"  {k}: {v}")

def cmd_bookmarks(args):
    import pymupdf

    doc = pymupdf.open(args.input)

    if args.list:
        toc = doc.get_toc()
        if not toc:
            print("No bookmarks found.")
        else:
            for entry in toc:
                if len(entry) >= 3:
                    level, title, page_num = entry[0], entry[1], entry[2]
                    print(f"  {'  ' * level}[L{level}] {title} (page {page_num+1})")
        doc.close()
        return

    if args.add:
        if not args.title or not args.page:
            error("Provide --title and --page for --add")
        toc = doc.get_toc()
        # Add new entry: level, title, page (0-based)
        new_entry = [args.level, args.title, args.page - 1]
        if toc:
            toc.append(new_entry)
        else:
            toc = [new_entry]
        doc.set_toc(toc)
        out_p = ensure_output_dir(args.output or output_path(args.input, "_bm", None))
        doc.save(out_p, encryption=0)
        doc.close()
        print(f"Bookmark added -> {out_p}")
        return

    if args.remove:
        doc.close()
        error("Bookmark removal requires rebuilding outline — use --remove carefully")

def cmd_pdf2img(args):
    import pymupdf

    doc = pymupdf.open(args.input)
    out_dir = Path(args.output) if args.output else Path(args.input).parent / f"{Path(args.input).stem}_images"
    out_dir.mkdir(parents=True, exist_ok=True)

    pages = []
    if args.pages:
        pages = [int(p) - 1 for p in args.pages.split(",")]
    else:
        pages = list(range(doc.page_count))

    img_paths = []
    for idx in pages:
        if idx < 0 or idx >= doc.page_count:
            continue
        page = doc[idx]
        mat = pymupdf.Matrix(args.dpi / 72, args.dpi / 72)
        clip = page.rect
        pix = page.get_pixmap(matrix=mat, clip=clip)
        ext = args.format
        fname = f"page_{idx+1}.{ext}"
        fpath = out_dir / fname
        pix.save(fpath)
        img_paths.append(str(fpath))

    doc.close()
    print(f"Converted {len(img_paths)} pages to {out_dir}")

def cmd_img2pdf(args):
    import pymupdf

    out_p = ensure_output_dir(args.output)
    writer = pymupdf.open()

    for img_path in args.input:
        img_p = ensure_input(img_path)
        try:
            from PIL import Image
            with Image.open(img_p) as pil_img:
                w, h = pil_img.size
            page = writer.new_page(width=w, height=h)
            page.insert_image(page.rect, filename=str(img_p), keep_proportion=True)
        except Exception:
            ext = img_p.suffix.lower()
            if ext in (".pdf",):
                img_doc = pymupdf.open(img_p)
                for dp in img_doc:
                    page = writer.new_page(width=dp.rect.width, height=dp.rect.height)
                    page.show_pdf_page(page.rect, img_doc, dp.number)
                img_doc.close()
            else:
                raise ValueError(f"Cannot identify image format: {img_p}")

    writer.save(out_p)
    writer.close()
    print(f"Converted {len(args.input)} images to PDF: {out_p}")

def cmd_ocr(args):
    try:
        import pytesseract
    except ImportError:
        error("pytesseract not installed. Run: .venv\\Scripts\\pip install pytesseract pypdf2")

    import pymupdf
    import pytesseract
    from PIL import Image

    doc = pymupdf.open(args.input)
    out_pages = []
    if args.pages:
        out_pages = [int(p) - 1 for p in args.pages.split(",")]
    else:
        out_pages = list(range(doc.page_count))

    texts = []
    for idx in out_pages:
        page = doc[idx]
        mat = pymupdf.Matrix(300 / 72, 300 / 72)
        pix = page.get_pixmap(matrix=mat)
        img_data = pix.tobytes("png")
        import io
        img = Image.open(io.BytesIO(img_data))
        if args.preprocess:
            from PIL import ImageFilter, ImageEnhance
            img = img.convert("L")
            img = img.filter(ImageFilter.MedianFilter)
            img = ImageEnhance.Contrast(img).enhance(1.5)

        langs = args.lang
        txt = pytesseract.image_to_string(img, lang=langs)
        texts.append(f"=== Page {idx+1} ===\n{txt}")

    doc.close()
    content = "\n\n".join(texts)
    with open(args.output, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"OCR text written to: {args.output}")

def cmd_weave(args):
    import pymupdf

    main_doc = pymupdf.open(args.input)
    donor_doc = pymupdf.open(args.donor)

    # mapping: comma-separated list of page numbers from donor to insert in order
    # e.g. "1,2,3" means insert donor page 1, then page 2, then page 3 into main doc
    donor_pages = [int(x) - 1 for x in args.mapping.split(",")]
    if not donor_pages:
        error("Provide --mapping as comma-separated page numbers")

    # Build result by copying main pages and inserting donor pages
    out_p = ensure_output_dir(args.output)
    result = pymupdf.open()

    for i in range(main_doc.page_count):
        result.insert_pdf(main_doc, from_page=i, to_page=i)
        # Check if this position should have a donor page inserted
        # Simple approach: insert donor pages sequentially at each main page
        if i < len(donor_pages):
            donor_page = donor_pages[i]
            if 0 <= donor_page < donor_doc.page_count:
                result.insert_pdf(donor_doc, from_page=donor_page, to_page=donor_page)

    # If there are more donor pages than main pages
    for j in range(main_doc.page_count, len(donor_pages)):
        donor_page = donor_pages[j]
        if 0 <= donor_page < donor_doc.page_count:
            result.insert_pdf(donor_doc, from_page=donor_page, to_page=donor_page)

    result.save(out_p, incremental=True, encryption=0)
    result.close()
    main_doc.close()
    donor_doc.close()
    print(f"Weaved pages into: {out_p}")

def cmd_stamp(args):
    import pymupdf

    if not args.text:
        error("Provide --text for stamp content")

    from datetime import date

    def expand_template(tmpl, page_num, total):
        t = date.today()
        return tmpl.replace("{n}", str(page_num + 1))\
                   .replace("{N}", str(total))\
                   .replace("{d}", t.isoformat())\
                   .replace("{D}", t.strftime("%Y-%m-%d"))

    doc = pymupdf.open(args.input)
    out_p = ensure_output_dir(args.output)
    total = doc.page_count

    for i in range(total):
        page = doc[i]
        w = page.rect.width
        h = page.rect.height
        expanded = expand_template(args.text, i, total)

        if args.pos == "header":
            rc = pymupdf.Rect(50, 10, w - 50, 40)
            page.add_freetext_annot(rc, expanded, fontsize=args.font,
                                   text_color=(0, 0, 0))
        elif args.pos == "footer":
            rc = pymupdf.Rect(50, h - 40, w - 50, h - 10)
            page.add_freetext_annot(rc, expanded, fontsize=args.font,
                                   text_color=(0, 0, 0))
        else:  # center
            rc = pymupdf.Rect(w/2 - 100, h/2 - 20, w/2 + 100, h/2 + 20)
            page.add_freetext_annot(rc, expanded, fontsize=args.font,
                                   text_color=(0, 0, 0))

    doc.save(out_p, encryption=0)
    doc.close()
    print(f"Stamped {total} pages -> {out_p}")

def sanitize_filename(name):
    import re
    return re.sub(r'[<>:"/\\|?*]', '_', name)

# ── Dispatch ─────────────────────────────────────────────────────

def main():
    ensure_venv()
    args = parse_args()

    # lazily import after venv is ready
    import pymupdf

    if args.cmd == "info": cmd_info(args)
    elif args.cmd == "text": cmd_text(args)
    elif args.cmd == "images": cmd_images(args)
    elif args.cmd == "pages": cmd_pages(args)
    elif args.cmd == "merge": cmd_merge(args)
    elif args.cmd == "split": cmd_split(args)
    elif args.cmd == "rotate": cmd_rotate(args)
    elif args.cmd == "watermark": cmd_watermark(args)
    elif args.cmd == "compress": cmd_compress(args)
    elif args.cmd == "encrypt": cmd_encrypt(args)
    elif args.cmd == "decrypt": cmd_decrypt(args)
    elif args.cmd == "metadata": cmd_metadata(args)
    elif args.cmd == "bookmarks": cmd_bookmarks(args)
    elif args.cmd == "pdf2img": cmd_pdf2img(args)
    elif args.cmd == "img2pdf": cmd_img2pdf(args)
    elif args.cmd == "ocr": cmd_ocr(args)
    elif args.cmd == "weave": cmd_weave(args)
    elif args.cmd == "stamp": cmd_stamp(args)
    else:
        error(f"Unknown command: {args.cmd}")

if __name__ == "__main__":
    main()