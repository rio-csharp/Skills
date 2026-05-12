---
name: docx
description: Create, read, modify, and convert Word (.docx) documents. Use when the user asks to create a Word document, read/extract text from .docx files, add content to existing documents, convert docx to HTML/txt, or inspect document metadata. Do not use for .doc (old binary format) files or PDF conversion (use the pdf skill instead).
---

# Docx

Work with Word (.docx) files using the bundled C# helper.

## Start Here

1. Identify the task: read, create, modify, or convert.
2. Run the appropriate command below.
3. Validate output exists and is valid.

## Commands

### Read / Extract

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- read "<path-to-file.docx>"
```

Output: document metadata (title, author, subject, keywords, created, modified) and full text content.

### Create Document

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- create --output "<path-to-output.docx>" --content "<text content>"
```

Creates a new .docx file. Use `\n` in content to create multiple paragraphs.

### Modify Document

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- modify "<path-to-file.docx>" --content "<text to append>"
```

Appends a new paragraph to an existing .docx file. Use `--output` to write to a different file instead of modifying in place.

### Convert Document

```bash
dotnet run --file <skill-path>/scripts/docx.cs -- convert "<path-to-file.docx>" --format <html|txt>
```

Converts a .docx file to HTML or plain text. PDF conversion is not supported (use the pdf skill instead).

## Workflow

1. **Read**: Extract text/metadata first to understand document structure.
2. **Create/Modify**: Use create or modify commands to build or update the document.
3. **Convert**: If the user needs HTML or text output, use the convert command.
4. **Validate**: Check output file was created and has content.

## Resources

- `scripts/docx.cs`: bundled helper for all docx operations.
- `references/format.md`: DOCX format internals and limitations.

## Safety

- Read-only operations (read, convert) do not modify source files.
- Create/modify operations create new files or modify copies.
- No network operations or credential requirements.

## Validation

- Check output file exists after create/modify/convert.
- For read operations, verify extracted text is non-empty.
- Confirm HTML conversion produces valid output files.
