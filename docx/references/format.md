# DOCX Format Notes

DOCX is a ZIP-based XML format (Office Open XML, ECMA-376). Key points for working with .docx files:

## Structure

A .docx file is a ZIP archive containing:
- `word/document.xml` - main document content
- `word/styles.xml` - styles definitions
- `word/settings.xml` - document settings
- `[Content_Types].xml` - content type declarations
- `_rels/` - relationship files

## Limitations

- Read/write uses the `DocumentFormat.OpenXml` NuGet package.
- Conversion to PDF requires an external PDF renderer (use the pdf skill with the docx as input).
- Binary .doc files are not supported (old Office format).

## OpenXML Element Overview

- `Body` - container for paragraph and table elements
- `Paragraph` (`<w:p>`) - block-level text container
- `Run` (`<w:r>`) - inline text container
- `Text` (`<w:t>`) - actual text content within a run
- Tables use `Table`, `TableRow`, `TableCell` elements

## Metadata Access

`WordprocessingDocument.PackageProperties` provides:
- Title, Creator/Author, Subject, Keywords, Description
- Created, Modified, LastModifiedBy, Revision

## Notes

- Use `InnerText` on Body to extract all text content.
- `Body.InnerText` concatenates all text but loses formatting info.
- DOCX encoding is always UTF-8.