# DOCX Format Notes

DOCX is a ZIP-based XML format (Office Open XML, ECMA-376). Key points for working with .docx files:

## Structure

A .docx file is a ZIP archive containing:
- `word/document.xml` - main document content
- `word/styles.xml` - styles definitions
- `word/numbering.xml` - list numbering definitions (used for bullets and numbered lists)
- `word/settings.xml` - document settings
- `[Content_Types].xml` - content type declarations
- `_rels/` - relationship files

## Bundled Helper Capabilities

The `docx.cs` helper script supports:

- **Plain text** read/write with paragraph creation
- **Markdown formatting** via `--from-markdown` (headings, bold, italic, strikethrough, inline code, code blocks, blockquotes, lists, tables, horizontal rules, links)
- **Table insertion** with CSV-style data and optional header row
- **Image insertion** (PNG, JPEG, GIF, BMP, TIFF) with configurable size in millimeters
- **Document metadata** read/write (title, author, subject, keywords)
- **Conversion** to TXT, HTML (preserves formatting), and Markdown

## OpenXML Element Overview

- `Body` - container for paragraph and table elements
- `Paragraph` (`<w:p>`) - block-level text container
- `Run` (`<w:r>`) - inline text container
- `Text` (`<w:t>`) - actual text content within a run
- `Table`, `TableRow`, `TableCell` - table elements
- `NumberingProperties` (`<w:numPr>`) - list item marker
- `Drawing` - inline image container

## Markdown-to-DOCX Mapping

| Markdown | OpenXML Implementation |
|----------|----------------------|
| `# Heading` | `ParagraphStyleId = "Heading1"` (blue, bold) |
| `**bold**` | `RunProperties.Bold` |
| `*italic*` | `RunProperties.Italic` |
| `~~strike~~` | `RunProperties.Strike` |
| `` `code` `` | `RunProperties.Shading` + Consolas font |
| ```` ``` ```` | Paragraph with gray background + Consolas |
| `> quote` | Left border + gray text + indentation |
| `- item` | `NumberingId=1`, `NumberingLevelReference=0` |
| `1. item` | `NumberingId=2`, `NumberingLevelReference=0` |
| `\| a \| b \|` | `Table` with borders |
| `---` | Paragraph with bottom border |

## Limitations

- Read/write uses the `DocumentFormat.OpenXml` NuGet package (v3.3.0).
- Conversion to PDF requires an external PDF renderer (use the pdf skill with the docx as input).
- Binary .doc files are not supported (old Office format).
- Markdown links are styled as blue underlined text but are not actual clickable hyperlinks (no relationship target).
- Image insertion uses fixed dimensions (default ~15cm x ~8.5cm if not specified).

## Metadata Access

`WordprocessingDocument.PackageProperties` provides:
- Title, Creator/Author, Subject, Keywords, Description
- Created, Modified, LastModifiedBy, Revision

## Notes

- Use `InnerText` on Body to extract all text content.
- `Body.InnerText` concatenates all text but loses formatting info.
- DOCX encoding is always UTF-8.
- For Chinese and other CJK text, the helper sets `EastAsia` font to match the specified `--font` or defaults to document defaults.
