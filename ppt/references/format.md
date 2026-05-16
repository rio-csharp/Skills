# PPTX Format Internals

PPTX is a ZIP-based XML format (Office Open XML, ECMA-376). It shares the same packaging foundation as DOCX and XLSX but uses PresentationML for content.

## Structure Overview

A .pptx file is a ZIP archive containing:
- `ppt/presentation.xml` — main presentation (slide list, sizes, defaults)
- `ppt/slideMasters/slideMaster1.xml` — master slide template
- `ppt/slideLayouts/slideLayout1.xml` — layout templates linked to masters
- `ppt/slides/slide1.xml` — individual slide content
- `ppt/notesSlides/notesSlide1.xml` — speaker notes
- `[Content_Types].xml` — content type declarations
- `_rels/` — relationship files

## Bundled Helper Capabilities

The `ppt.cs` helper script supports:

- **Read**: list slide count and preview text from each slide
- **Create from lines**: generate full presentations with headings, paragraphs, bold/italic, code blocks, quotes, bullets, numbered lists, tables, images, shapes, charts, and backgrounds
- **Modify**: append new slides to existing presentations
- **Merge**: combine multiple presentations
- **Split/Remove/Reorder**: manipulate slide collections
- **Notes**: add and extract speaker notes
- **Properties**: set document metadata (title, author, etc.)
- **Export**: convert to PDF via LibreOffice or PowerPoint COM
- **Themes**: 5 built-in visual themes controlling colors, fonts, and spacing
- **Layouts**: 7 slide layout templates (title-content, title, two-content, comparison, section-header, title-only, blank)
- **Shapes**: rectangles, ellipses, lines, arrows with fill, stroke, and rounded corners
- **Charts**: bar, column, pie, line, area charts with inline CSV data
- **Animations**: basic entrance effects (fade, flyin, appear)
- **Transitions**: 20+ slide transition effects
- **Multi-slide**: `NEWSLIDE` command creates new slides within a single content stream

## DrawingML Text Model

PPTX text uses DrawingML (same as shapes in Word and Excel):
- `TextBody` contains `Paragraph` elements
- Each `Paragraph` contains `Run` elements
- Each `Run` contains `Text` and optional `RunProperties` (bold, italic, color, font size)
- Alignment is set on `ParagraphProperties`
- Bullet/numbered lists use `ParagraphProperties.Level`

## DrawingML Shapes

Shapes are defined via `ShapeProperties` containing:
- `Transform2D` with `Offset` (x, y) and `Extents` (width, height) in EMU
- `PresetGeometry` with `ShapeTypeValues` (Rectangle, Ellipse, Line, etc.)
- `SolidFill` for fill color
- `Outline` for stroke (color + width)
- `HeadEnd` for arrowheads on lines

## DrawingML Charts

Charts are embedded via `ChartPart` referenced from a `GraphicFrame`:
- `ChartSpace` contains `Chart`
- `Chart` contains `PlotArea` with specific chart types (`BarChart`, `PieChart`, `LineChart`, `AreaChart`)
- Each chart type has its own series type (`BarChartSeries`, `PieChartSeries`, etc.)
- Series contain `CategoryAxisData` (categories) and `Values` (numeric data)
- Axis charts (bar, column, line, area) require `CategoryAxis` and `ValueAxis`

## Slide Layout Architecture

Layouts are templates that define default placeholder shapes on a slide:
- Each layout specifies shape names and positions
- Text commands route to appropriate shapes (headings → TitleShape, body → first content shape)
- `blank` layout has no shapes; user must provide coordinates via `POS` or `SHAPE`

## Animations & Transitions

Animations use `Slide.Timing` containing a `TimeNodeList` with:
- `ParallelTimeNode` → `CommonTimeNode` → `ChildTimeNodeList`
- `SequenceTimeNode` for main sequence
- `SetBehavior` or `Animate` for individual effects
- `TargetElement` with `ShapeTarget` references shape by ID

Transitions use `Slide.Transition` containing specific transition types:
- `FadeTransition`, `PushTransition`, `WipeTransition`, etc.

## OpenXml SDK v3.3.0 Notes

- `SlideMasterPart`, `SlideLayoutPart`, and `SlidePart` have no public constructors in v3.3.0.
- The helper works around this by creating a blank .pptx via `System.IO.Packaging` first, then opening it with `PresentationDocument.Open` where `AddNewPart<SlidePart>()` works correctly.
- `PresentationPart.SlideLayoutParts` does not exist; layouts are accessed via `SlideMasterPart.SlideLayoutParts`.
- Slide manipulation (merge, split) uses `AddPart` to copy slide parts between presentations, which preserves relationships.

## Limitations

- Animation support is basic (fade, flyin, appear entrance effects only). No motion paths or complex timelines.
- No embedded video/audio support.
- Table styling is basic (header row fill only).
- No built-in OCR or image-to-text extraction.
- Shape corner radius is approximate (uses preset rounded rectangle geometry).
- Export to PDF requires external tools (LibreOffice or PowerPoint).
