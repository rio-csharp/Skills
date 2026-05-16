# PPTX Format Internals

PPTX is a ZIP-based XML format (Office Open XML, ECMA-376). It shares the same packaging foundation as DOCX and XLSX but uses PresentationML for content.

## Structure Overview

A .pptx file is a ZIP archive containing:
- `ppt/presentation.xml` — main presentation (slide list, sizes, defaults)
- `ppt/slideMasters/slideMaster1.xml` — master slide template
- `ppt/slideLayouts/slideLayout1.xml` — layout templates linked to masters
- `ppt/slides/slide1.xml` — individual slide content
- `[Content_Types].xml` — content type declarations
- `_rels/` — relationship files

## Bundled Helper Capabilities

The `ppt.cs` helper script supports:

- **Read**: list slide count and preview text from each slide
- **Create from lines**: generate full presentations with headings, paragraphs, bold/italic, code blocks, quotes, bullets, numbered lists, tables, and images
- **Themes**: 5 built-in visual themes controlling colors, fonts, and spacing
- **Multi-slide**: `NEWSLIDE` command creates new slides within a single content stream

## DrawingML Text Model

PPTX text uses DrawingML (same as shapes in Word and Excel):
- `TextBody` contains `Paragraph` elements
- Each `Paragraph` contains `Run` elements
- Each `Run` contains `Text` and optional `RunProperties` (bold, italic, color, font size)
- Alignment is set on `ParagraphProperties`
- Bullet/numbered lists use `ParagraphProperties.Level`

## OpenXml SDK v3.3.0 Notes

- `SlideMasterPart`, `SlideLayoutPart`, and `SlidePart` have no public constructors in v3.3.0.
- The helper works around this by creating a blank .pptx via `System.IO.Packaging` first, then opening it with `PresentationDocument.Open` where `AddNewPart<SlidePart>()` works correctly.
- `PresentationPart.SlideLayoutParts` does not exist; layouts are accessed via `SlideMasterPart.SlideLayoutParts`.

## Limitations

- No slide reordering, deletion, or merge operations in the current helper.
- No animation or transition support.
- No embedded video/audio support.
- Table styling is basic (header row fill only).
- No built-in OCR or image-to-text extraction.
