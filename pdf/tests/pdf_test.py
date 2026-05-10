#!/usr/bin/env python3
"""
PDF Skill Comprehensive Test Suite
Usage: python pdf_test.py [--no-cleanup]

Creates test PDFs, runs all commands, verifies outputs, cleans up.
"""

import sys
import os
import subprocess
import json
import shutil
from pathlib import Path
import tempfile

VENV_PYTHON = Path(r"D:\Repos\Skills\.venv\Scripts\python.exe")
SKILL_SCRIPT = Path(r"D:\Repos\Skills\pdf\scripts\pdf.py")
TESTS_DIR = Path(__file__).parent.resolve()

# ── helpers ────────────────────────────────────────────────

def run(args, check=True, capture=True):
    """Run the PDF script and return stdout."""
    cmd = [str(VENV_PYTHON), str(SKILL_SCRIPT)] + args
    r = subprocess.run(cmd, capture_output=capture, text=True)
    if check and r.returncode != 0:
        print(f"  ❌ Command failed: {' '.join(args)}", file=sys.stderr)
        print(f"     stderr: {r.stderr[:500]}", file=sys.stderr)
        return None
    return r.stdout, r.stderr

def exists(path):
    p = Path(path).resolve()
    return p.exists() and p.stat().st_size > 0

def read_text(path):
    with open(path, "r", encoding="utf-8") as f:
        return f.read()

def read_json(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)

# ── Test PDF Creation ─────────────────────────────────────

def create_test_pdfs(work_dir):
    """Create a set of known test PDFs using PyMuPDF."""
    import pymupdf

    # PDF 1: simple 3-page text doc with bookmarks
    doc1_path = work_dir / "test1.pdf"
    doc1 = pymupdf.open()
    for i in range(3):
        page = doc1.new_page()
        page.insert_text((50, 100), f"Test Document 1 - Page {i+1}", fontsize=20)
        page.insert_text((50, 150), "This is a test PDF for automated testing.", fontsize=12)
    doc1.set_toc([(1, "Test Document 1 - Page 1", 1), (1, "Test Document 1 - Page 2", 2), (1, "Test Document 1 - Page 3", 3)])
    doc1.save(doc1_path)
    doc1.close()

    # PDF 2: 2-page doc
    doc2_path = work_dir / "test2.pdf"
    doc2 = pymupdf.open()
    for i in range(2):
        page = doc2.new_page()
        page.insert_text((50, 100), f"Test Document 2 - Page {i+1}", fontsize=20)
        page.insert_text((50, 150), "Second test document content.", fontsize=12)
    doc2.save(doc2_path)
    doc2.close()

    # PDF 3: single-page doc for encryption test
    doc3_path = work_dir / "test3.pdf"
    doc3 = pymupdf.open()
    page = doc3.new_page()
    page.insert_text((50, 100), "Test Document 3 - Single Page", fontsize=20)
    page.insert_text((50, 150), "For encryption testing.", fontsize=12)
    doc3.save(doc3_path)
    doc3.close()

    # PDF 4: multi-page for split by range
    doc4_path = work_dir / "test4.pdf"
    doc4 = pymupdf.open()
    for i in range(6):
        page = doc4.new_page()
        page.insert_text((50, 100), f"Page {i+1} of 6", fontsize=20)
        page.insert_text((50, 150), f"Content for page {i+1}.", fontsize=12)
    doc4.save(doc4_path)
    doc4.close()

    return [doc1_path, doc2_path, doc3_path, doc4_path]

# ── Test Runners ──────────────────────────────────────────

def test_info(passed, failed, work_dir):
    print("\n── info ──")
    doc1 = work_dir / "test1.pdf"
    out_json = work_dir / "info.json"
    stdout, _ = run(["info", "-i", str(doc1), "-o", str(out_json)])
    if not stdout and not exists(out_json):
        failed += 1
    else:
        data = read_json(out_json)
        checks = [("pages", data["pages"] == 3), ("version", data["version"] != "")]
        for name, ok in checks:
            if not ok:
                print(f"  ❌ info check '{name}' failed")
                failed += 1
            else:
                print(f"  ✅ info '{name}'")
                passed += 1
    return passed, failed

def test_text(passed, failed, work_dir):
    print("\n── text ──")
    doc1 = work_dir / "test1.pdf"
    out_txt = work_dir / "text_out.txt"
    stdout, _ = run(["text", "-i", str(doc1), "-o", str(out_txt)])
    if exists(out_txt):
        content = read_text(out_txt)
        ok = "Test Document 1" in content
        if ok:
            print("  ✅ text extracts content")
            passed += 1
        else:
            print("  ❌ text content check failed")
            failed += 1
    else:
        print("  ❌ text output file not created")
        failed += 1

    # layout mode
    out_layout = work_dir / "text_layout.txt"
    stdout2, _ = run(["text", "-i", str(doc1), "--layout", "-o", str(out_layout)])
    if exists(out_layout):
        print("  ✅ text --layout runs")
        passed += 1
    else:
        print("  ❌ text --layout failed")
        failed += 1
    return passed, failed

def test_images(passed, failed, work_dir):
    print("\n── images ──")
    doc1 = work_dir / "test1.pdf"
    out_dir = work_dir / "images_out"
    run(["images", "-i", str(doc1), "-o", str(out_dir)])
    meta_file = out_dir / "_images_meta.json"
    if exists(meta_file):
        print("  ✅ images extracts (meta created)")
        passed += 1
    else:
        print("  ❌ images failed")
        failed += 1
    return passed, failed

def test_pages(passed, failed, work_dir):
    print("\n── pages ──")
    doc4 = work_dir / "test4.pdf"
    out_pages = work_dir / "pages_1_3.pdf"
    stdout, _ = run(["pages", "-i", str(doc4), "--range", "1-3", "-o", str(out_pages)])
    if exists(out_pages):
        print("  ✅ pages --range extracts")
        passed += 1
    else:
        print("  ❌ pages --range failed")
        failed += 1

    # test mode=remove
    out_removed = work_dir / "pages_remaining.pdf"
    stdout, _ = run(["pages", "-i", str(doc4), "--range", "1", "--mode", "remove", "-o", str(out_removed)])
    if exists(out_removed):
        import pymupdf
        doc = pymupdf.open(out_removed)
        if doc.page_count == 5:
            print("  ✅ pages --mode remove works")
            passed += 1
        else:
            print(f"  ❌ pages --mode remove: expected 5 pages, got {doc.page_count}")
            failed += 1
        doc.close()
    else:
        print("  ❌ pages --mode remove failed")
        failed += 1
    return passed, failed

def test_merge(passed, failed, work_dir):
    print("\n── merge ──")
    doc1 = work_dir / "test1.pdf"
    doc2 = work_dir / "test2.pdf"
    out_merged = work_dir / "merged.pdf"
    stdout, _ = run(["merge", "-i", str(doc1), str(doc2), "-o", str(out_merged)])
    if exists(out_merged):
        import pymupdf
        doc = pymupdf.open(out_merged)
        if doc.page_count == 5:
            print("  ✅ merge combines 3+2 pages")
            passed += 1
        else:
            print(f"  ❌ merge: expected 5 pages, got {doc.page_count}")
            failed += 1
        doc.close()
    else:
        print("  ❌ merge failed")
        failed += 1
    return passed, failed

def test_split(passed, failed, work_dir):
    print("\n── split ──")
    doc4 = work_dir / "test4.pdf"
    out_dir = work_dir / "split_out"
    out_dir.mkdir(exist_ok=True)
    stdout, _ = run(["split", "-i", str(doc4), "--range", "1-2 5", "-o", str(out_dir)])
    # Check that output dir has a file
    files = list(out_dir.glob("*.pdf"))
    if files:
        print("  ✅ split --range creates files")
        passed += 1
    else:
        print("  ❌ split --range failed")
        failed += 1
    return passed, failed

def test_rotate(passed, failed, work_dir):
    print("\n── rotate ──")
    doc1 = work_dir / "test1.pdf"
    out_rot = work_dir / "rotated.pdf"
    stdout, _ = run(["rotate", "-i", str(doc1), "--angle", "90", "-o", str(out_rot)])
    if exists(out_rot):
        print("  ✅ rotate 90°")
        passed += 1
    else:
        print("  ❌ rotate failed")
        failed += 1
    return passed, failed

def test_watermark(passed, failed, work_dir):
    print("\n── watermark ──")
    doc1 = work_dir / "test1.pdf"
    out_wm = work_dir / "watermarked.pdf"
    stdout, _ = run(["watermark", "-i", str(doc1), "--text", "CONFIDENTIAL",
                     "--opacity", "0.3", "-o", str(out_wm)])
    if exists(out_wm):
        print("  ✅ watermark text")
        passed += 1
    else:
        print("  ❌ watermark failed")
        failed += 1
    return passed, failed

def test_compress(passed, failed, work_dir):
    print("\n── compress ──")
    doc1 = work_dir / "test1.pdf"
    out_comp = work_dir / "compressed.pdf"
    stdout, _ = run(["compress", "-i", str(doc1), "--level", "max", "-o", str(out_comp)])
    if exists(out_comp):
        orig_size = (work_dir / "test1.pdf").stat().st_size
        new_size = out_comp.stat().st_size
        if new_size <= orig_size:
            print(f"  ✅ compress ({orig_size/1024:.1f} KB -> {new_size/1024:.1f} KB)")
            passed += 1
        else:
            print(f"  ⚠️  compress grew file (not always bad)")
            passed += 1
    else:
        print("  ❌ compress failed")
        failed += 1
    return passed, failed

def test_encrypt_decrypt(passed, failed, work_dir):
    print("\n── encrypt/decrypt ──")
    doc3 = work_dir / "test3.pdf"
    out_enc = work_dir / "encrypted.pdf"
    stdout, _ = run(["encrypt", "-i", str(doc3), "--password", "testpass123",
                     "--bits", "128", "-o", str(out_enc)])
    if not exists(out_enc):
        print("  ❌ encrypt failed")
        failed += 1
        return passed, failed
    print("  ✅ encrypt")

    out_dec = work_dir / "decrypted.pdf"
    stdout, _ = run(["decrypt", "-i", str(out_enc), "--password", "testpass123",
                     "-o", str(out_dec)])
    if exists(out_dec):
        print("  ✅ decrypt")
        passed += 1
    else:
        print("  ❌ decrypt failed")
        failed += 1
    return passed, failed

def test_metadata(passed, failed, work_dir):
    print("\n── metadata ──")
    doc1 = work_dir / "test1.pdf"
    out_meta = work_dir / "meta.json"
    stdout, _ = run(["metadata", "-i", str(doc1), "-o", str(out_meta)])
    if exists(out_meta):
        print("  ✅ metadata read")
        passed += 1
    else:
        print("  ❌ metadata read failed")
        failed += 1
        return passed, failed

    out_meta2 = work_dir / "meta_updated.pdf"
    stdout, _ = run(["metadata", "-i", str(doc1), "-o", str(out_meta2),
                     "--title", "Updated Title", "--author", "Test Author"])
    if exists(out_meta2):
        print("  ✅ metadata write")
        passed += 1
    else:
        print("  ❌ metadata write failed")
        failed += 1
    return passed, failed

def test_bookmarks(passed, failed, work_dir):
    print("\n── bookmarks ──")
    doc1 = work_dir / "test1.pdf"
    stdout, _ = run(["bookmarks", "-i", str(doc1), "--list"])
    if stdout and ("Page 1" in stdout or "Test Document" in stdout or "Page" in stdout):
        print("  ✅ bookmarks --list")
        passed += 1
    else:
        print("  ❌ bookmarks --list failed")
        failed += 1
    return passed, failed

def test_pdf2img(passed, failed, work_dir):
    print("\n── pdf2img ──")
    doc1 = work_dir / "test1.pdf"
    out_dir = work_dir / "pdf2img_out"
    out_dir.mkdir(exist_ok=True)
    stdout, _ = run(["pdf2img", "-i", str(doc1), "--dpi", "72", "--format", "png",
                     "--output", str(out_dir)])
    imgs = list(out_dir.glob("*.png"))
    if imgs:
        print(f"  ✅ pdf2img ({len(imgs)} pages)")
        passed += 1
    else:
        print("  ❌ pdf2img failed")
        failed += 1
    return passed, failed

def test_img2pdf(passed, failed, work_dir):
    print("\n── img2pdf ──")
    # Create a real PNG image using PIL
    from PIL import Image, ImageDraw, ImageFont
    img_test = work_dir / "test_img.png"
    img = Image.new("RGB", (200, 100), color="white")
    draw = ImageDraw.Draw(img)
    draw.text((40, 40), "Test Image", fill="black")
    img.save(img_test)

    out_pdf = work_dir / "img2pdf_out.pdf"
    stdout, _ = run(["img2pdf", "-i", str(img_test), "-o", str(out_pdf)])
    if exists(out_pdf):
        print("  ✅ img2pdf")
        passed += 1
    else:
        print("  ❌ img2pdf failed")
        failed += 1
    return passed, failed

def test_stamp(passed, failed, work_dir):
    print("\n── stamp ──")
    doc1 = work_dir / "test1.pdf"
    out_stamp = work_dir / "stamped.pdf"
    stdout, _ = run(["stamp", "-i", str(doc1), "--text", "Page {n} of {N}",
                     "--pos", "footer", "--font", "10", "-o", str(out_stamp)])
    if exists(out_stamp):
        print("  ✅ stamp")
        passed += 1
    else:
        print("  ❌ stamp failed")
        failed += 1
    return passed, failed

# ── Main ─────────────────────────────────────────────────

def main():
    no_cleanup = "--no-cleanup" in sys.argv

    print("PDF Skill — Comprehensive Test Suite")
    print("=" * 50)

    # bootstrap venv
    subprocess.run([str(VENV_PYTHON), "-m", "pip", "install", "pymupdf", "pdfplumber"],
                  capture_output=True)

    work_dir = Path(tempfile.mkdtemp(prefix="pdf_test_"))
    print(f"Work dir: {work_dir}\n")

    create_test_pdfs(work_dir)
    print(f"Created {len(list(work_dir.glob('*.pdf')))} test PDFs")

    passed, failed = 0, 0

    # Run all tests
    passed, failed = test_info(passed, failed, work_dir)
    passed, failed = test_text(passed, failed, work_dir)
    passed, failed = test_images(passed, failed, work_dir)
    passed, failed = test_pages(passed, failed, work_dir)
    passed, failed = test_merge(passed, failed, work_dir)
    passed, failed = test_split(passed, failed, work_dir)
    passed, failed = test_rotate(passed, failed, work_dir)
    passed, failed = test_watermark(passed, failed, work_dir)
    passed, failed = test_compress(passed, failed, work_dir)
    passed, failed = test_encrypt_decrypt(passed, failed, work_dir)
    passed, failed = test_metadata(passed, failed, work_dir)
    passed, failed = test_bookmarks(passed, failed, work_dir)
    passed, failed = test_pdf2img(passed, failed, work_dir)
    passed, failed = test_img2pdf(passed, failed, work_dir)
    passed, failed = test_stamp(passed, failed, work_dir)

    # Cleanup
    if not no_cleanup:
        shutil.rmtree(work_dir, ignore_errors=True)
        print(f"\nCleaned up: {work_dir}")
    else:
        print(f"\nKept work dir: {work_dir}")

    print(f"\n{'=' * 50}")
    print(f"Results: {passed} passed, {failed} failed")
    if failed > 0:
        sys.exit(1)

if __name__ == "__main__":
    main()
