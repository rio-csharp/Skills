#!/usr/bin/env python3
"""
Extract embedded images from PDF pages, or render pages as PNG.
Usage: uv run --with pymupdf python extract_images.py <command> [options]
Requires: uv (run 'uv run --with pymupdf python extract_images.py --help' to verify)
"""

import sys
import argparse
import os
from pathlib import Path

try:
    import fitz  # PyMuPDF
except ImportError:
    print("ERROR: PyMuPDF not installed. Run: uv run --with pymupdf python extract_images.py ...",
          file=sys.stderr)
    sys.exit(1)


def extract_images(pdf_path: str, output_dir: str, prefix: str) -> int:
    if not os.path.exists(pdf_path):
        print(f"ERROR: file not found: {pdf_path}", file=sys.stderr)
        return 1

    try:
        doc = fitz.open(pdf_path)
    except Exception as e:
        print(f"ERROR: could not open '{pdf_path}': {e}", file=sys.stderr)
        return 1

    out_path = Path(output_dir)
    out_path.mkdir(parents=True, exist_ok=True)

    total = 0
    for page_num, page in enumerate(doc):
        images = page.get_images(full=True)
        for img_index, img in enumerate(images):
            xref = img[0]
            base_image = doc.extract_image(xref)
            ext = base_image["ext"] or "png"
            img_data = base_image["image"]

            filename = f"{prefix}page{page_num + 1}_img{img_index + 1}.{ext}"
            filepath = out_path / filename

            try:
                with open(filepath, "wb") as f:
                    f.write(img_data)
                total += 1
            except Exception as e:
                print(f"WARNING: could not write {filepath}: {e}", file=sys.stderr)

    page_count = len(doc)
    doc.close()
    print(f"Extracted {total} image(s) from {page_count} page(s) -> {output_dir}")
    return 0


def render_pages(pdf_path: str, output_dir: str, prefix: str, dpi: int) -> int:
    if not os.path.exists(pdf_path):
        print(f"ERROR: file not found: {pdf_path}", file=sys.stderr)
        return 1

    try:
        doc = fitz.open(pdf_path)
    except Exception as e:
        print(f"ERROR: could not open '{pdf_path}': {e}", file=sys.stderr)
        return 1

    out_path = Path(output_dir)
    out_path.mkdir(parents=True, exist_ok=True)

    page_count = len(doc)
    for page_num, page in enumerate(doc):
        mat = fitz.Matrix(dpi / 72, dpi / 72)
        pix = page.get_pixmap(matrix=mat)
        filename = f"{prefix}page{page_num + 1}.png"
        filepath = out_path / filename
        try:
            pix.save(str(filepath))
        except Exception as e:
            print(f"WARNING: could not render page {page_num + 1}: {e}", file=sys.stderr)
    doc.close()
    print(f"Rendered {page_count} page(s) at {dpi} DPI -> {output_dir}")
    return 0


def main():
    parser = argparse.ArgumentParser(description="Extract images or render pages from a PDF.")
    sub = parser.add_subparsers(dest="command", required=True)

    p_images = sub.add_parser("images", help="Extract embedded images from PDF pages")
    p_images.add_argument("-i", "--input", required=True, help="Input PDF file")
    p_images.add_argument("-o", "--output", required=True, help="Output directory")
    p_images.add_argument("--prefix", default="", help="Filename prefix for extracted images")

    p_render = sub.add_parser("render", help="Render PDF pages as PNG images")
    p_render.add_argument("-i", "--input", required=True, help="Input PDF file")
    p_render.add_argument("-o", "--output", required=True, help="Output directory")
    p_render.add_argument("--prefix", default="", help="Filename prefix for rendered pages")
    p_render.add_argument("--dpi", type=int, default=150, help="Render resolution (default: 150)")

    args = parser.parse_args()

    if args.command == "images":
        return extract_images(args.input, args.output, args.prefix)
    elif args.command == "render":
        return render_pages(args.input, args.output, args.prefix, args.dpi)


if __name__ == "__main__":
    sys.exit(main())
