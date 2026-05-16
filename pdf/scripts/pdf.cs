#!/usr/bin/env dotnet
#:package PdfPig@0.1.14
#:package itext7@9.6.0
#:package itext7.bouncy-castle-adapter@9.6.0
#:package SixLabors.ImageSharp@3.1.12

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UglyToad.PdfPig;
using iText7Document = iText.Kernel.Pdf.PdfDocument;
using iText7PdfReader = iText.Kernel.Pdf.PdfReader;
using iText7PdfWriter = iText.Kernel.Pdf.PdfWriter;
using iText7WriterProperties = iText.Kernel.Pdf.WriterProperties;
using iText7ReaderProperties = iText.Kernel.Pdf.ReaderProperties;
using iText7PageSize = iText.Kernel.Geom.PageSize;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using SixLabors.ImageSharp;

var exitCode = new PdfTool().Run(args);
return exitCode;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(InfoResult))]
[JsonSerializable(typeof(MetadataResult))]
partial class PdfJsonContext : JsonSerializerContext { }

sealed record InfoResult(
    string File,
    int Pages,
    string Version,
    double SizeKb,
    string Title,
    string Author,
    string Subject,
    string Keywords,
    string Creator,
    string Producer,
    string CreationDate,
    string ModDate);

sealed record MetadataResult(
    string Title,
    string Author,
    string Subject,
    string Keywords,
    string Creator,
    string Producer);

class PdfTool
{
    string _currentColor = "";
    string _currentSize = "";
    string _currentFont = "";
    string _currentAlign = "";
    StyleTheme _theme = Themes["default"];

    static readonly Dictionary<string, StyleTheme> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = new StyleTheme
        {
            Name = "default",
            HeadingColor = "2E74B5",
            QuoteTextColor = "666666",
            QuoteBorderColor = "CCCCCC",
            CodeBackground = "F5F5F5",
            TableHeaderFill = "D9E2F3",
            TableBorderColor = "999999",
            LinkColor = "0563C1",
            HeadingFont = StandardFonts.HELVETICA_BOLD,
            BodyFont = StandardFonts.HELVETICA,
            CodeFont = StandardFonts.COURIER,
            Heading1Size = 24, Heading2Size = 20, Heading3Size = 18,
            Heading4Size = 16, Heading5Size = 14, Heading6Size = 12,
            BodySize = 11, CodeSize = 9,
            HeadingSpacingBefore = 18, HeadingSpacingAfter = 6,
            ParagraphSpacingAfter = 8,
            HeadingBold = true,
            TableBorders = true
        },
        ["report"] = new StyleTheme
        {
            Name = "report",
            HeadingColor = "1F4E79",
            QuoteTextColor = "5B5B5B",
            QuoteBorderColor = "A6A6A6",
            CodeBackground = "F2F2F2",
            TableHeaderFill = "B4C7E7",
            TableBorderColor = "7F7F7F",
            LinkColor = "2E75B6",
            HeadingFont = StandardFonts.HELVETICA_BOLD,
            BodyFont = StandardFonts.HELVETICA,
            CodeFont = StandardFonts.COURIER,
            Heading1Size = 26, Heading2Size = 22, Heading3Size = 18,
            Heading4Size = 16, Heading5Size = 14, Heading6Size = 12,
            BodySize = 10, CodeSize = 9,
            HeadingSpacingBefore = 16, HeadingSpacingAfter = 4,
            ParagraphSpacingAfter = 6,
            HeadingBold = true,
            TableBorders = true
        },
        ["modern"] = new StyleTheme
        {
            Name = "modern",
            HeadingColor = "00B4D8",
            QuoteTextColor = "495057",
            QuoteBorderColor = "CED4DA",
            CodeBackground = "F8F9FA",
            TableHeaderFill = "E9ECEF",
            TableBorderColor = "ADB5BD",
            LinkColor = "0096C7",
            HeadingFont = StandardFonts.HELVETICA_BOLD,
            BodyFont = StandardFonts.HELVETICA,
            CodeFont = StandardFonts.COURIER,
            Heading1Size = 26, Heading2Size = 22, Heading3Size = 18,
            Heading4Size = 16, Heading5Size = 14, Heading6Size = 12,
            BodySize = 10, CodeSize = 9,
            HeadingSpacingBefore = 20, HeadingSpacingAfter = 6,
            ParagraphSpacingAfter = 8,
            HeadingBold = false,
            TableBorders = true
        },
        ["minimal"] = new StyleTheme
        {
            Name = "minimal",
            HeadingColor = "212529",
            QuoteTextColor = "6C757D",
            QuoteBorderColor = "DEE2E6",
            CodeBackground = "F8F9FA",
            TableHeaderFill = "E9ECEF",
            TableBorderColor = "ADB5BD",
            LinkColor = "495057",
            HeadingFont = StandardFonts.HELVETICA_BOLD,
            BodyFont = StandardFonts.HELVETICA,
            CodeFont = StandardFonts.COURIER,
            Heading1Size = 24, Heading2Size = 20, Heading3Size = 18,
            Heading4Size = 16, Heading5Size = 14, Heading6Size = 12,
            BodySize = 10, CodeSize = 9,
            HeadingSpacingBefore = 14, HeadingSpacingAfter = 4,
            ParagraphSpacingAfter = 5,
            HeadingBold = false,
            TableBorders = false
        },
        ["elegant"] = new StyleTheme
        {
            Name = "elegant",
            HeadingColor = "4A4A4A",
            QuoteTextColor = "7A7A7A",
            QuoteBorderColor = "C9B99A",
            CodeBackground = "FDFBF7",
            TableHeaderFill = "F5F0E8",
            TableBorderColor = "C9B99A",
            LinkColor = "8B7355",
            HeadingFont = StandardFonts.TIMES_BOLD,
            BodyFont = StandardFonts.TIMES_ROMAN,
            CodeFont = StandardFonts.COURIER,
            Heading1Size = 26, Heading2Size = 22, Heading3Size = 18,
            Heading4Size = 16, Heading5Size = 14, Heading6Size = 12,
            BodySize = 11, CodeSize = 9,
            HeadingSpacingBefore = 18, HeadingSpacingAfter = 6,
            ParagraphSpacingAfter = 8,
            HeadingBold = true,
            TableBorders = true
        }
    };

    public int Run(string[] args)
    {
        if (args.Length == 0) return Fail("No command. Try: pdf info --input file.pdf");
        var cmd = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        return cmd switch
        {
            "info" => CmdInfo(rest),
            "text" => CmdText(rest),
            "pages" => CmdPages(rest),
            "merge" => CmdMerge(rest),
            "split" => CmdSplit(rest),
            "rotate" => CmdRotate(rest),
            "watermark" => CmdWatermark(rest),
            "compress" => CmdCompress(rest),
            "encrypt" => CmdEncrypt(rest),
            "decrypt" => CmdDecrypt(rest),
            "metadata" => CmdMetadata(rest),
            "bookmarks" => CmdBookmarks(rest),
            "img2pdf" => CmdImg2Pdf(rest),
            "weave" => CmdWeave(rest),
            "stamp" => CmdStamp(rest),
            "create" => CmdCreate(rest),
            "images" or "render" => Fail($"{cmd}: not supported by this C# helper. Use scripts/extract_images.py with uv run --with pymupdf python."),
            "pdf2img" or "ocr" => Fail($"{cmd}: not supported by this C# helper. Use a dedicated rendering/OCR tool when page rendering or OCR is required."),
            _ => Fail($"Unknown command: {cmd}")
        };
    }

    // ── Info ────────────────────────────────────────────────────────────────

    int CmdInfo(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("info: --input required");
        var output = Arg("--output", "-o", args);

        using var doc = new iText7Document(new iText7PdfReader(input));
        var info = doc.GetDocumentInfo();
        var sizeKb = new FileInfo(input).Length / 1024.0;
        var result = new InfoResult(
            Path.GetFullPath(input),
            doc.GetNumberOfPages(),
            doc.GetPdfVersion().ToString(),
            Math.Round(sizeKb, 1),
            info.GetTitle() ?? "",
            info.GetAuthor() ?? "",
            info.GetSubject() ?? "",
            info.GetKeywords() ?? "",
            info.GetCreator() ?? "",
            info.GetProducer() ?? "",
            info.GetMoreInfo("CreationDate") ?? "",
            info.GetMoreInfo("ModDate") ?? "");

        if (!string.IsNullOrEmpty(output))
        {
            File.WriteAllText(output, JsonSerializer.Serialize(result, PdfJsonContext.Default.InfoResult));
            Console.WriteLine("Info written to: " + output);
        }
        else
        {
            Console.WriteLine("  file: " + result.File);
            Console.WriteLine("  pages: " + result.Pages);
            Console.WriteLine("  version: " + result.Version);
            Console.WriteLine("  size_kb: " + result.SizeKb);
            Console.WriteLine("  title: " + result.Title);
            Console.WriteLine("  author: " + result.Author);
            Console.WriteLine("  subject: " + result.Subject);
            Console.WriteLine("  keywords: " + result.Keywords);
            Console.WriteLine("  creator: " + result.Creator);
            Console.WriteLine("  producer: " + result.Producer);
            Console.WriteLine("  creation_date: " + result.CreationDate);
            Console.WriteLine("  mod_date: " + result.ModDate);
        }
        return 0;
    }

    // ── Text ────────────────────────────────────────────────────────────────

    int CmdText(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("text: --input required");
        var output = Arg("--output", "-o", args);
        var page = Arg("--page", "", args);
        var fmt = Arg("--fmt", "", args) ?? "txt";

        var sb = new System.Text.StringBuilder();
        using var doc = PdfDocument.Open(input);
        List<int> pages;
        if (string.IsNullOrEmpty(page))
        {
            pages = Enumerable.Range(1, doc.NumberOfPages).ToList();
        }
        else if (TryParsePage(page, doc.NumberOfPages, out var pageNumber, out var pageError))
        {
            pages = new List<int> { pageNumber };
        }
        else
        {
            return Fail("text: " + pageError);
        }

        foreach (var p in pages)
        {
            if (p < 1 || p > doc.NumberOfPages) continue;
            var pdfPage = doc.GetPage(p);
            var text = pdfPage.Text;
            sb.AppendLine(text);
        }

        var content = sb.ToString().TrimEnd();
        if (!string.IsNullOrEmpty(output))
        {
            File.WriteAllText(output, content);
            Console.WriteLine("Text written to: " + output);
        }
        else
        {
            Console.Write(content);
        }
        return 0;
    }

    // ── Pages ───────────────────────────────────────────────────────────────

    int CmdPages(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("pages: --input required");
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("pages: --output required");
        var range = Arg("--range", "", args) ?? "1-";
        var mode = Arg("--mode", "", args) ?? "copy";

        using var srcDoc = new iText7Document(new iText7PdfReader(input));
        var total = srcDoc.GetNumberOfPages();
        if (!TryParseRange(range, total, out var indices, out var rangeError)) return Fail("pages: " + rangeError);

        using var writer = new iText7PdfWriter(output);
        using var dstDoc = new iText7Document(writer);

        if (mode == "remove")
        {
            for (int i = total; i >= 1; i--)
                if (!indices.Contains(i - 1))
                    srcDoc.CopyPagesTo(i, i, dstDoc);
        }
        else
        {
            foreach (var idx in indices)
                if (idx >= 0 && idx < total)
                    srcDoc.CopyPagesTo(idx + 1, idx + 1, dstDoc);
        }
        Console.WriteLine((mode == "remove" ? "Removed" : "Extracted") + " pages to: " + output);
        return 0;
    }

    // ── Merge ───────────────────────────────────────────────────────────────

    int CmdMerge(string[] args)
    {
        var inputs = ExtractInputs(args);
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("merge: --output required");
        if (inputs.Count == 0) return Fail("merge: provide input files");

        // Build in memory, then write atomically
        var ms = new MemoryStream();
        using (var writer = new iText7PdfWriter(ms))
        using (var dstDoc = new iText7Document(writer))
        {
            foreach (var input in inputs)
            {
                // Read input fully into memory to avoid any file locking conflicts
                var inputBytes = File.ReadAllBytes(input);
                using var ms2 = new MemoryStream(inputBytes);
                using var srcDoc = new iText7Document(new iText7PdfReader(ms2));
                var pageCount = srcDoc.GetNumberOfPages();
                for (int i = 1; i <= pageCount; i++)
                    srcDoc.CopyPagesTo(i, i, dstDoc);
            }
        }
        File.WriteAllBytes(output, ms.ToArray());
        Console.WriteLine("Merged " + inputs.Count + " PDFs into: " + output);
        return 0;
    }

    // ── Split ─────────────────────────────────────────────────────────────

    int CmdSplit(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("split: --input required");
        var output = Arg("--output", "-o", args);
        var range = Arg("--range", "", args);
        var byBookmarks = ContainsArg("--bookmarks", args);

        using var srcDoc = new iText7Document(new iText7PdfReader(input));
        var total = srcDoc.GetNumberOfPages();

        if (byBookmarks)
        {
            return Fail("split --bookmarks: unsupported by this helper; use --range instead");
        }
        else if (!string.IsNullOrEmpty(range))
        {
            var outDir = output ?? Path.GetDirectoryName(input) ?? ".";
            Directory.CreateDirectory(outDir);
            if (!TryParseRange(range, total, out var indices, out var rangeError)) return Fail("split: " + rangeError);
            var indices1Based = indices.Select(i => i + 1).ToList();
            using var writer = new iText7PdfWriter(Path.Combine(outDir, Path.GetFileNameWithoutExtension(input) + "_pages.pdf"));
            using var subDoc = new iText7Document(writer);
            foreach (var idx in indices1Based)
                if (idx >= 1 && idx <= total)
                    srcDoc.CopyPagesTo(idx, idx, subDoc);
            Console.WriteLine("Split pages to: " + outDir);
            return 0;
        }
        else
        {
            return Fail("split: provide --range or --bookmarks");
        }
    }

    // ── Rotate ─────────────────────────────────────────────────────────────

    int CmdRotate(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("rotate: --input required");
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("rotate: --output required");
        var angleStr = Arg("--angle", "", args);
        if (string.IsNullOrEmpty(angleStr)) return Fail("rotate: --angle required (90, 180, 270)");
        var range = Arg("--range", "", args);
        if (!int.TryParse(angleStr, out var angle) || (angle != 90 && angle != 180 && angle != 270))
        {
            return Fail("rotate: --angle must be 90, 180, or 270");
        }

        using var srcDoc = new iText7Document(new iText7PdfReader(input));
        using var writer = new iText7PdfWriter(output);
        using var doc = new iText7Document(writer);

        var total = srcDoc.GetNumberOfPages();
        List<int> indices;
        if (string.IsNullOrEmpty(range))
        {
            indices = Enumerable.Range(0, total).ToList();
        }
        else if (!TryParseRange(range, total, out indices, out var rangeError))
        {
            return Fail("rotate: " + rangeError);
        }

        for (int i = 1; i <= total; i++)
        {
            var page = srcDoc.GetPage(i);
            if (indices.Contains(i - 1))
                page.SetRotation((page.GetRotation() + angle) % 360);
            srcDoc.CopyPagesTo(i, i, doc);
        }
        Console.WriteLine("Rotated " + indices.Count + " pages by " + angle + " deg -> " + output);
        return 0;
    }

    // ── Watermark ─────────────────────────────────────────────────────────

    int CmdWatermark(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("watermark: --input required");
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("watermark: --output required");
        var text = Arg("--text", "", args);
        var pos = Arg("--pos", "", args) ?? "center";

        if (string.IsNullOrEmpty(text)) return Fail("watermark: --text required");

        using var reader = new iText7PdfReader(input);
        using var writer = new iText7PdfWriter(output);
        using var doc = new iText7Document(reader, writer);

        for (int i = 1; i <= doc.GetNumberOfPages(); i++)
        {
            var page = doc.GetPage(i);
            var rect = page.GetPageSize();
            var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);

            float fontSize = (float)Math.Min(rect.GetWidth() / (text.Length * 0.6), rect.GetHeight() * 0.25);
            if (fontSize < 8) fontSize = 8;
            canvas.BeginText();
            var font = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            canvas.SetFontAndSize(font, fontSize);
            canvas.SetColor(new iText.Kernel.Colors.DeviceRgb(128, 128, 128), fill: true);

            float x, y;
            if (pos == "tile")
            {
                x = (float)rect.GetLeft() + 10;
                y = (float)rect.GetBottom() + 10;
            }
            else
            {
                x = (float)(rect.GetLeft() + rect.GetWidth() / 2 - 80);
                y = (float)(rect.GetBottom() + rect.GetHeight() / 2);
            }
            canvas.MoveText(x, y);
            canvas.ShowText(text);
            canvas.EndText();
        }
        Console.WriteLine("Watermarked " + doc.GetNumberOfPages() + " pages -> " + output);
        return 0;
    }

    // ── Compress ───────────────────────────────────────────────────────────

    int CmdCompress(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("compress: --input required");
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("compress: --output required");
        var level = Arg("--level", "", args) ?? "medium";
        var gc = level switch { "low" => 1, "medium" => 3, "max" => 9, _ => 3 };

        var origSize = new FileInfo(input).Length;
        using var writer = new iText7PdfWriter(output, new iText7WriterProperties().SetCompressionLevel(gc));
        using var doc = new iText7Document(new iText7PdfReader(input), writer);
        doc.Close();

        var newSize = new FileInfo(output).Length;
        var ratio = (1 - (double)newSize / origSize) * 100;
        Console.WriteLine($"Compressed {origSize / 1024:F0} KB -> {newSize / 1024:F0} KB ({ratio:F1}% reduction) -> {output}");
        return 0;
    }

    // ── Encrypt ────────────────────────────────────────────────────────────

    int CmdEncrypt(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("encrypt: --input required");
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("encrypt: --output required");
        var password = Arg("--password", "", args);
        if (string.IsNullOrEmpty(password)) return Fail("encrypt: --password required");
        var owner = Arg("--owner", "", args) ?? password;

        var props = new iText7WriterProperties().SetStandardEncryption(
            System.Text.Encoding.UTF8.GetBytes(password),
            System.Text.Encoding.UTF8.GetBytes(owner),
            0xFFFFFF, // all permissions
            1); // 1 = 128-bit RC4

        using var writer = new iText7PdfWriter(output, props);
        using var doc = new iText7Document(new iText7PdfReader(input), writer);
        Console.WriteLine("Encrypted -> " + output);
        return 0;
    }

    // ── Decrypt ─────────────────────────────────────────────────────────────

    int CmdDecrypt(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("decrypt: --input required");
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("decrypt: --output required");
        var password = Arg("--password", "", args);
        if (string.IsNullOrEmpty(password)) return Fail("decrypt: --password required");

        using var reader = new iText7PdfReader(input, new iText7ReaderProperties().SetPassword(System.Text.Encoding.UTF8.GetBytes(password)));
        using var writer = new iText7PdfWriter(output, new iText7WriterProperties());
        using var doc = new iText7Document(reader, writer);
        Console.WriteLine("Decrypted -> " + output);
        return 0;
    }

    // ── Metadata ───────────────────────────────────────────────────────────

    int CmdMetadata(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("metadata: --input required");
        var output = Arg("--output", "-o", args);
        var title = Arg("--title", "", args);
        var author = Arg("--author", "", args);
        var subject = Arg("--subject", "", args);
        var keywords = Arg("--keywords", "", args);

        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(author) && string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(keywords))
        {
            using var reader = new iText7PdfReader(input);
            using var doc = new iText7Document(reader);
            var info = doc.GetDocumentInfo();
            var result = new MetadataResult(
                info.GetTitle() ?? "",
                info.GetAuthor() ?? "",
                info.GetSubject() ?? "",
                info.GetKeywords() ?? "",
                info.GetCreator() ?? "",
                info.GetProducer() ?? "");
            if (!string.IsNullOrEmpty(output))
            {
                File.WriteAllText(output, JsonSerializer.Serialize(result, PdfJsonContext.Default.MetadataResult));
                Console.WriteLine("Metadata written to: " + output);
            }
            else
            {
                Console.WriteLine("  title: " + result.Title);
                Console.WriteLine("  author: " + result.Author);
                Console.WriteLine("  subject: " + result.Subject);
                Console.WriteLine("  keywords: " + result.Keywords);
                Console.WriteLine("  creator: " + result.Creator);
                Console.WriteLine("  producer: " + result.Producer);
            }
        }
        else
        {
            var outPath = output ?? Path.Combine(Path.GetDirectoryName(input) ?? ".", Path.GetFileNameWithoutExtension(input) + "_meta.pdf");
            using var reader = new iText7PdfReader(input);
            using var writer = new iText7PdfWriter(outPath);
            using var doc = new iText7Document(reader, writer);
            var info = doc.GetDocumentInfo();
            if (!string.IsNullOrEmpty(title)) info.SetTitle(title);
            if (!string.IsNullOrEmpty(author)) info.SetAuthor(author);
            if (!string.IsNullOrEmpty(subject)) info.SetSubject(subject);
            if (!string.IsNullOrEmpty(keywords)) info.SetKeywords(keywords);
            Console.WriteLine("Metadata updated -> " + outPath);
        }
        return 0;
    }

    // ── Bookmarks ──────────────────────────────────────────────────────────

    int CmdBookmarks(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("bookmarks: --input required");
        var list = ContainsArg("--list", args);
        var add = ContainsArg("--add", args);
        var addTitle = Arg("--title", "", args);
        var addPage = Arg("--page", "", args);

        using var doc = new iText7Document(new iText7PdfReader(input));

        if (list)
        {
            try
            {
                var outline = doc.GetOutlines(false);
                var topLevel = outline.GetAllChildren();
                if (topLevel.Count == 0)
                    Console.WriteLine("No bookmarks found.");
                else
                    Console.WriteLine("Bookmarks: " + topLevel.Count + " top-level item(s)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("No bookmarks found: " + ex.Message);
            }
            return 0;
        }

        if (add)
        {
            if (string.IsNullOrEmpty(addTitle) || string.IsNullOrEmpty(addPage))
                return Fail("bookmarks --add: --title and --page required");
            return Fail("bookmarks --add: unsupported by this helper; only --list is supported");
        }

        Console.WriteLine("Use --list or --add");
        return 0;
    }

    // ── Img2Pdf ─────────────────────────────────────────────────────────────

    int CmdImg2Pdf(string[] args)
    {
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("img2pdf: --output required");

        var inputs = ExtractInputs(args);
        if (inputs.Count == 0) return Fail("img2pdf: at least one image required");

        using var writer = new iText7PdfWriter(output);
        using var pdf = new iText7Document(writer);
        using var layout = new iText.Layout.Document(pdf);
        layout.SetMargins(0, 0, 0, 0);

        var added = 0;
        foreach (var imgPath in inputs)
        {
            if (!File.Exists(imgPath))
            {
                Console.Error.WriteLine($"Warning: image not found: {imgPath}");
                continue;
            }

            try
            {
                using var imgInfo = SixLabors.ImageSharp.Image.Load(imgPath);
                var pageSize = new iText7PageSize(imgInfo.Width, imgInfo.Height);
                pdf.AddNewPage(pageSize);

                var imageData = iText.IO.Image.ImageDataFactory.Create(imgPath);
                var image = new iText.Layout.Element.Image(imageData)
                    .ScaleToFit(imgInfo.Width, imgInfo.Height)
                    .SetFixedPosition(added + 1, 0, 0);
                layout.Add(image);
                added++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: could not add {imgPath}: {ex.Message}");
            }
        }

        if (added == 0) return Fail("img2pdf: no valid images were added");
        Console.WriteLine($"Converted {added} image(s) to PDF: {output}");
        return 0;
    }

    // ── Weave ───────────────────────────────────────────────────────────────

    int CmdWeave(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("weave: --input required");
        var donor = Arg("--donor", "", args);
        if (string.IsNullOrEmpty(donor)) return Fail("weave: --donor required");
        var mapping = Arg("--mapping", "", args);
        if (string.IsNullOrEmpty(mapping)) return Fail("weave: --mapping required (e.g. 1,2,3)");
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("weave: --output required");

        if (!TryParsePositiveList(mapping, out var donorPages, out var mappingError))
        {
            return Fail("weave: " + mappingError);
        }
        using var main = new iText7Document(new iText7PdfReader(input));
        using var donorDoc = new iText7Document(new iText7PdfReader(donor));
        using var writer = new iText7PdfWriter(output);
        using var result = new iText7Document(writer);

        int di = 0;
        for (int i = 1; i <= main.GetNumberOfPages(); i++)
        {
            main.CopyPagesTo(i, i, result);
            if (di < donorPages.Length && donorPages[di] == i)
            {
                var dp = donorPages[di++];
                if (dp >= 1 && dp <= donorDoc.GetNumberOfPages())
                    donorDoc.CopyPagesTo(dp, dp, result);
            }
        }
        while (di < donorPages.Length)
        {
            var dp = donorPages[di++];
            if (dp >= 1 && dp <= donorDoc.GetNumberOfPages())
                donorDoc.CopyPagesTo(dp, dp, result);
        }
        Console.WriteLine("Weaved pages into: " + output);
        return 0;
    }

    // ── Stamp ───────────────────────────────────────────────────────────────

    int CmdStamp(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Arg("", "", args);
        if (string.IsNullOrEmpty(input)) return Fail("stamp: --input required");
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("stamp: --output required");
        var text = Arg("--text", "", args);
        if (string.IsNullOrEmpty(text)) return Fail("stamp: --text required");
        var pos = Arg("--pos", "", args) ?? "footer";
        var fontArg = Arg("--font", "", args) ?? "10";
        if (!int.TryParse(fontArg, out var fontSize) || fontSize <= 0)
        {
            return Fail("stamp: --font must be a positive integer");
        }

        using var reader = new iText7PdfReader(input);
        using var writer = new iText7PdfWriter(output);
        using var doc = new iText7Document(reader, writer);

        var total = doc.GetNumberOfPages();
        for (int i = 1; i <= total; i++)
        {
            var page = doc.GetPage(i);
            var rect = page.GetPageSize();
            var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
            var expanded = text.Replace("{n}", i.ToString()).Replace("{N}", total.ToString()).Replace("{d}", DateTime.Now.ToString("yyyy-MM-dd"));

            canvas.BeginText();
            canvas.SetFontAndSize(iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA), fontSize);
            float x = (float)rect.GetLeft() + 50;
            float y = pos == "header" ? (float)rect.GetTop() - 30 : pos == "footer" ? (float)rect.GetBottom() + 10 : (float)(rect.GetBottom() + rect.GetHeight() / 2);
            canvas.MoveText(x, y);
            canvas.ShowText(expanded);
            canvas.EndText();
        }
        Console.WriteLine("Stamped " + total + " pages -> " + output);
        return 0;
    }

    // ── Create ──────────────────────────────────────────────────────────────

    int CmdCreate(string[] args)
    {
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("create: --output required");
        var content = Arg("--content", "-c", args);
        var contentFile = Arg("--content-file", "-cf", args);
        var fromLines = ContainsArg("--from-lines", args) || ContainsArg("-l", args);
        var styleName = Arg("--style", "-s", args) ?? "default";

        if (!string.IsNullOrEmpty(contentFile))
        {
            if (!File.Exists(contentFile)) return Fail($"Content file not found: {contentFile}");
            content = File.ReadAllText(contentFile);
        }

        if (Themes.TryGetValue(styleName, out var theme))
            _theme = theme;
        else
            Console.WriteLine($"Warning: unknown style '{styleName}', using default");

        using var writer = new iText7PdfWriter(output);
        using var pdf = new iText7Document(writer);
        using var document = new Document(pdf);
        document.SetMargins(60, 60, 60, 60);

        if (!string.IsNullOrEmpty(content))
        {
            if (fromLines)
                ParseLineFormat(content, document);
            else
            {
                var lines = content.Split('\n', StringSplitOptions.None);
                foreach (var line in lines)
                {
                    document.Add(new Paragraph(line)
                        .SetFont(GetOrCreateFont(_theme.BodyFont))
                        .SetFontSize(_theme.BodySize)
                        .SetMarginBottom(_theme.ParagraphSpacingAfter));
                }
            }
        }

        document.Close();
        Console.WriteLine("Created: " + Path.GetFullPath(output));
        return 0;
    }

    // ── Line Format Parser ──────────────────────────────────────────────────

    static readonly HashSet<string> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "H1","H2","H3","H4","H5","H6","P","B","I","U","S","BI","CODE","QUOTE",
        "BULLET","NUMBER","HR","BR","TABLE","IMG","COLOR","SIZE","FONT","ALIGN"
    };

    string? GetLineCommand(string line)
    {
        var trimmed = line.TrimStart();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        var spaceIdx = trimmed.IndexOf(' ');
        var cmd = spaceIdx < 0 ? trimmed : trimmed[..spaceIdx];
        return KnownCommands.Contains(cmd) ? cmd.ToUpperInvariant() : null;
    }

    void ParseLineFormat(string content, Document document)
    {
        var lines = content.Split('\n');
        for (int idx = 0; idx < lines.Length; idx++)
        {
            var rawLine = lines[idx];
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var spaceIdx = line.IndexOf(' ');
            string cmd, text;
            if (spaceIdx < 0)
            {
                cmd = line.Trim().ToUpperInvariant();
                text = "";
            }
            else
            {
                cmd = line[..spaceIdx].Trim().ToUpperInvariant();
                text = line[(spaceIdx + 1)..];
            }

            switch (cmd)
            {
                case "H1": AddHeading(document, 1, text); break;
                case "H2": AddHeading(document, 2, text); break;
                case "H3": AddHeading(document, 3, text); break;
                case "H4": AddHeading(document, 4, text); break;
                case "H5": AddHeading(document, 5, text); break;
                case "H6": AddHeading(document, 6, text); break;
                case "P": AddFormattedParagraph(document, text); break;
                case "B": AddFormattedParagraph(document, text, bold: true); break;
                case "I": AddFormattedParagraph(document, text, italic: true); break;
                case "U": AddFormattedParagraph(document, text, underline: true); break;
                case "S": AddFormattedParagraph(document, text, underline: true); break;
                case "BI": AddFormattedParagraph(document, text, bold: true, italic: true); break;
                case "CODE":
                    {
                        var codeLines = new List<string> { text };
                        int nextIdx = idx + 1;
                        while (nextIdx < lines.Length)
                        {
                            var nextLine = lines[nextIdx].TrimEnd();
                            if (string.IsNullOrWhiteSpace(nextLine)) break;
                            var trimmedNext = nextLine.TrimStart();
                            if (!trimmedNext.StartsWith("CODE", StringComparison.OrdinalIgnoreCase)) break;
                            var nextSpaceIdx = trimmedNext.IndexOf(' ');
                            var nextText = nextSpaceIdx < 0 ? "" : trimmedNext[(nextSpaceIdx + 1)..];
                            codeLines.Add(nextText);
                            nextIdx++;
                        }
                        AddCodeBlock(document, codeLines);
                        idx = nextIdx - 1;
                    }
                    break;
                case "QUOTE": AddQuote(document, text); break;
                case "BULLET": AddBullet(document, text); break;
                case "NUMBER": AddNumbered(document, text); break;
                case "HR": AddHorizontalRule(document); break;
                case "BR": document.Add(new Paragraph("").SetMarginBottom(_theme.ParagraphSpacingAfter)); break;
                case "TABLE":
                    {
                        var tableLines = new List<string> { text };
                        int nextIdx = idx + 1;
                        while (nextIdx < lines.Length)
                        {
                            var nextLine = lines[nextIdx].TrimEnd();
                            if (string.IsNullOrWhiteSpace(nextLine)) break;
                            if (GetLineCommand(nextLine) != null) break;
                            tableLines.Add(nextLine);
                            nextIdx++;
                        }
                        AddTableFromLine(document, string.Join(";", tableLines));
                        idx = nextIdx - 1;
                    }
                    break;
                case "IMG": AddImageFromLine(document, text); break;
                case "COLOR": _currentColor = text.Trim(); break;
                case "SIZE": _currentSize = text.Trim(); break;
                case "FONT": _currentFont = text.Trim(); break;
                case "ALIGN": _currentAlign = text.Trim().ToLowerInvariant(); break;
                default:
                    AddFormattedParagraph(document, line);
                    break;
            }
        }
    }

    void AddHeading(Document doc, int level, string text)
    {
        var size = level switch { 1 => _theme.Heading1Size, 2 => _theme.Heading2Size, 3 => _theme.Heading3Size, 4 => _theme.Heading4Size, 5 => _theme.Heading5Size, _ => _theme.Heading6Size };
        var para = new Paragraph(text)
            .SetFont(GetOrCreateFont(_theme.HeadingFont))
            .SetFontSize(size)
            .SetFontColor(HexToRgb(_theme.HeadingColor))
            .SetMarginTop(_theme.HeadingSpacingBefore)
            .SetMarginBottom(_theme.HeadingSpacingAfter);
        ApplyParaAlign(para);
        doc.Add(para);
    }

    void AddFormattedParagraph(Document doc, string text, bool bold = false, bool italic = false, bool underline = false, bool strike = false)
    {
        var textEl = new Text(text);
        if (bold) textEl.SimulateBold();
        if (italic) textEl.SimulateItalic();
        if (underline) textEl.SetUnderline();
        if (!string.IsNullOrEmpty(_currentColor)) textEl.SetFontColor(HexToRgb(_currentColor));
        if (!string.IsNullOrEmpty(_currentSize) && float.TryParse(_currentSize, out var sz)) textEl.SetFontSize(sz);
        if (!string.IsNullOrEmpty(_currentFont))
        {
            try { textEl.SetFont(GetOrCreateFont(_currentFont)); } catch { }
        }

        var para = new Paragraph(textEl)
            .SetFont(GetOrCreateFont(_theme.BodyFont))
            .SetFontSize(_theme.BodySize)
            .SetMarginBottom(_theme.ParagraphSpacingAfter);
        ApplyParaAlign(para);
        doc.Add(para);
    }

    void AddCodeBlock(Document doc, List<string> lines)
    {
        var table = new Table(1).SetWidth(UnitValue.CreatePercentValue(100));
        var cell = new Cell()
            .SetBackgroundColor(HexToRgb(_theme.CodeBackground))
            .SetPadding(10)
            .SetBorder(new SolidBorder(HexToRgb("DDDDDD"), 1));

        foreach (var line in lines)
        {
            cell.Add(new Paragraph(line)
                .SetFont(GetOrCreateFont(_theme.CodeFont))
                .SetFontSize(_theme.CodeSize)
                .SetFontColor(new DeviceRgb(60, 60, 60))
                .SetMultipliedLeading(1.3f));
        }
        table.AddCell(cell);
        doc.Add(table);
        doc.Add(new Paragraph("").SetMarginBottom(_theme.ParagraphSpacingAfter));
    }

    void AddQuote(Document doc, string text)
    {
        var table = new Table(1).SetWidth(UnitValue.CreatePercentValue(100));
        var cell = new Cell()
            .SetBorderLeft(new SolidBorder(HexToRgb(_theme.QuoteBorderColor), 4))
            .SetBackgroundColor(HexToRgb("F8F8F8"))
            .SetPadding(10)
            .Add(new Paragraph(text)
                .SetFont(GetOrCreateFont(_theme.BodyFont))
                .SetFontSize(_theme.BodySize)
                .SetFontColor(HexToRgb(_theme.QuoteTextColor))
                .SetMultipliedLeading(1.4f));
        table.AddCell(cell);
        doc.Add(table);
        doc.Add(new Paragraph("").SetMarginBottom(_theme.ParagraphSpacingAfter));
    }

    void AddBullet(Document doc, string text)
    {
        var list = new List(ListNumberingType.ZAPF_DINGBATS_1)
            .SetListSymbol("\u2022")
            .SetFont(GetOrCreateFont(_theme.BodyFont))
            .SetFontSize(_theme.BodySize)
            .SetMarginLeft(20)
            .SetMarginBottom(_theme.ParagraphSpacingAfter);
        var item = new ListItem();
        var para = new Paragraph(text).SetFont(GetOrCreateFont(_theme.BodyFont)).SetFontSize(_theme.BodySize);
        if (!string.IsNullOrEmpty(_currentColor)) para.SetFontColor(HexToRgb(_currentColor));
        if (!string.IsNullOrEmpty(_currentSize) && float.TryParse(_currentSize, out var sz)) para.SetFontSize(sz);
        if (!string.IsNullOrEmpty(_currentFont)) para.SetFont(GetOrCreateFont(_currentFont));
        ApplyParaAlign(para);
        item.Add(para);
        list.Add(item);
        doc.Add(list);
    }

    void AddNumbered(Document doc, string text)
    {
        var list = new List(ListNumberingType.DECIMAL)
            .SetFont(GetOrCreateFont(_theme.BodyFont))
            .SetFontSize(_theme.BodySize)
            .SetMarginLeft(20)
            .SetMarginBottom(_theme.ParagraphSpacingAfter);
        var item = new ListItem();
        var para = new Paragraph(text).SetFont(GetOrCreateFont(_theme.BodyFont)).SetFontSize(_theme.BodySize);
        if (!string.IsNullOrEmpty(_currentColor)) para.SetFontColor(HexToRgb(_currentColor));
        if (!string.IsNullOrEmpty(_currentSize) && float.TryParse(_currentSize, out var sz)) para.SetFontSize(sz);
        if (!string.IsNullOrEmpty(_currentFont)) para.SetFont(GetOrCreateFont(_currentFont));
        ApplyParaAlign(para);
        item.Add(para);
        list.Add(item);
        doc.Add(list);
    }

    void AddHorizontalRule(Document doc)
    {
        doc.Add(new Paragraph("")
            .SetBorderBottom(new SolidBorder(HexToRgb("CCCCCC"), 1))
            .SetWidth(UnitValue.CreatePercentValue(100))
            .SetMarginTop(8)
            .SetMarginBottom(8));
    }

    void AddTableFromLine(Document doc, string text)
    {
        var rows = text.Split(';');
        if (rows.Length == 0) return;
        var cols = rows[0].Split(',').Length;
        var table = new Table(cols).SetWidth(UnitValue.CreatePercentValue(100));
        if (_theme.TableBorders)
            table.SetBorder(new SolidBorder(HexToRgb(_theme.TableBorderColor), 1));

        for (int r = 0; r < rows.Length; r++)
        {
            var cells = rows[r].Split(',');
            foreach (var c in cells)
            {
                var cell = new Cell()
                    .SetPadding(6)
                    .Add(new Paragraph(c.Trim()).SetFont(GetOrCreateFont(_theme.BodyFont)).SetFontSize(_theme.BodySize - 1));
                if (r == 0 && _theme.TableBorders)
                    cell.SetBackgroundColor(HexToRgb(_theme.TableHeaderFill));
                table.AddCell(cell);
            }
        }
        doc.Add(table);
        doc.Add(new Paragraph("").SetMarginBottom(_theme.ParagraphSpacingAfter));
    }

    void AddImageFromLine(Document doc, string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        var imgPath = parts[0];
        if (!File.Exists(imgPath))
        {
            Console.WriteLine($"Warning: image not found: {imgPath}");
            return;
        }

        float widthPt = 0, heightPt = 0;
        if (parts.Length > 1)
        {
            if (parts[1].EndsWith('%'))
            {
                if (float.TryParse(parts[1][..^1], out var pct))
                    widthPt = pct / 100f * 450;
            }
            else if (float.TryParse(parts[1], out var mm))
            {
                widthPt = mm * 2.8346f;
            }
        }
        if (parts.Length > 2 && float.TryParse(parts[2], out var hmm))
        {
            heightPt = hmm * 2.8346f;
        }

        try
        {
            var imgData = iText.IO.Image.ImageDataFactory.Create(imgPath);
            var img = new iText.Layout.Element.Image(imgData);
            if (widthPt > 0 && heightPt > 0)
                img.SetWidth(widthPt).SetHeight(heightPt);
            else if (widthPt > 0)
                img.SetWidth(widthPt);
            else
                img.SetAutoScale(true);
            doc.Add(img);
            doc.Add(new Paragraph("").SetMarginBottom(_theme.ParagraphSpacingAfter));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: could not add image {imgPath}: {ex.Message}");
        }
    }

    DeviceRgb HexToRgb(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            return new DeviceRgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }
        return new DeviceRgb(0, 0, 0);
    }

    PdfFont GetOrCreateFont(string fontName)
    {
        try
        {
            return PdfFontFactory.CreateFont(fontName);
        }
        catch
        {
            return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        }
    }

    void ApplyParaAlign(Paragraph para)
    {
        if (string.IsNullOrEmpty(_currentAlign)) return;
        para.SetTextAlignment(_currentAlign switch
        {
            "center" => TextAlignment.CENTER,
            "right" => TextAlignment.RIGHT,
            "justify" => TextAlignment.JUSTIFIED,
            _ => TextAlignment.LEFT
        });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    string? Arg(string longForm, string shortForm, string[] args)
    {
        if (string.IsNullOrEmpty(longForm) && string.IsNullOrEmpty(shortForm))
        {
            return FirstPositional(args);
        }

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == longForm || (!string.IsNullOrEmpty(shortForm) && args[i] == shortForm))
                return i + 1 < args.Length ? args[i + 1] : null;
        }
        return null;
    }

    string? FirstPositional(string[] args)
    {
        var optionsWithValues = new HashSet<string>
        {
            "--input", "-i", "--output", "-o", "--range", "--mode", "--angle",
            "--text", "--password", "--owner", "--level", "--title", "--author",
            "--subject", "--keywords", "--donor", "--mapping", "--pos", "--font",
            "--page", "--fmt"
        };

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (optionsWithValues.Contains(arg))
            {
                i++;
                continue;
            }
            if (!arg.StartsWith("-")) return arg;
        }

        return null;
    }

    bool ContainsArg(string flag, string[] args) => args.Contains(flag);

    List<string> ExtractInputs(string[] args)
    {
        var inputs = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--input" || a == "-i")
            {
                if (i + 1 < args.Length) inputs.Add(args[++i]);
                continue;
            }
            if (a == "--output" || a == "-o" || a == "--range" || a == "--mode" || a == "--angle" || a == "--text" || a == "--password" || a == "--owner" || a == "--level" || a == "--title" || a == "--author" || a == "--subject" || a == "--keywords" || a == "--donor" || a == "--mapping" || a == "--pos" || a == "--font")
            {
                if (i + 1 < args.Length) i++;
                continue;
            }
            if (a.StartsWith("-")) continue;
            inputs.Add(a);
        }
        return inputs;
    }

    bool TryParseRange(string range, int total, out List<int> result, out string error)
    {
        result = new List<int>();
        error = "";
        foreach (var part in range.Replace(",", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Contains('-'))
            {
                var sp = part.Split('-');
                if (sp.Length != 2)
                {
                    error = $"invalid page range '{part}'";
                    return false;
                }
                if (!int.TryParse(sp[0] == "" ? "1" : sp[0], out var start) ||
                    !int.TryParse(sp[1] == "" ? total.ToString() : sp[1], out var end))
                {
                    error = $"invalid page range '{part}'";
                    return false;
                }
                if (start < 1 || end < start || end > total)
                {
                    error = $"page range '{part}' is outside 1-{total}";
                    return false;
                }
                for (int i = start; i <= end; i++) result.Add(i - 1);
            }
            else
            {
                if (!int.TryParse(part, out var page) || page < 1 || page > total)
                {
                    error = $"page '{part}' is outside 1-{total}";
                    return false;
                }
                result.Add(page - 1);
            }
        }
        if (result.Count == 0)
        {
            error = "page range is empty";
            return false;
        }
        return true;
    }

    bool TryParsePositiveList(string value, out int[] numbers, out string error)
    {
        var parsed = new List<int>();
        error = "";
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var number) || number < 1)
            {
                numbers = [];
                error = $"--mapping must be a comma-separated list of positive page numbers; invalid value '{part}'";
                return false;
            }
            parsed.Add(number);
        }
        if (parsed.Count == 0)
        {
            numbers = [];
            error = "--mapping must include at least one page number";
            return false;
        }
        numbers = parsed.ToArray();
        return true;
    }

    bool TryParsePage(string value, int total, out int page, out string error)
    {
        if (!int.TryParse(value, out page) || page < 1 || page > total)
        {
            error = $"--page must be an integer between 1 and {total}";
            return false;
        }
        error = "";
        return true;
    }

    int Fail(string msg)
    {
        Console.Error.WriteLine("ERROR: " + msg);
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: pdf <command> [options]");
        Console.Error.WriteLine("Commands: info, text, pages, merge, split, rotate, watermark, compress, encrypt, decrypt, metadata, bookmarks, img2pdf, weave, stamp, create");
        Console.Error.WriteLine("Python helper commands: images, render");
        return 1;
    }
}

class StyleTheme
{
    public string Name { get; set; } = "default";
    public string HeadingColor { get; set; } = "2E74B5";
    public string QuoteTextColor { get; set; } = "666666";
    public string QuoteBorderColor { get; set; } = "CCCCCC";
    public string CodeBackground { get; set; } = "F5F5F5";
    public string TableHeaderFill { get; set; } = "D9E2F3";
    public string TableBorderColor { get; set; } = "999999";
    public string LinkColor { get; set; } = "0563C1";
    public string HeadingFont { get; set; } = StandardFonts.HELVETICA_BOLD;
    public string BodyFont { get; set; } = StandardFonts.HELVETICA;
    public string CodeFont { get; set; } = StandardFonts.COURIER;
    public int Heading1Size { get; set; } = 24;
    public int Heading2Size { get; set; } = 20;
    public int Heading3Size { get; set; } = 18;
    public int Heading4Size { get; set; } = 16;
    public int Heading5Size { get; set; } = 14;
    public int Heading6Size { get; set; } = 12;
    public int BodySize { get; set; } = 11;
    public int CodeSize { get; set; } = 9;
    public int HeadingSpacingBefore { get; set; } = 18;
    public int HeadingSpacingAfter { get; set; } = 6;
    public int ParagraphSpacingAfter { get; set; } = 8;
    public bool HeadingBold { get; set; } = true;
    public bool TableBorders { get; set; } = true;
}
