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
            "images" or "pdf2img" or "ocr" => Fail($"{cmd}: not supported by this C# helper. Use a dedicated rendering/OCR tool when true image extraction, page rendering, or OCR is required."),
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

    // ── Helpers ─────────────────────────────────────────────────────────────

    string? Arg(string longForm, string shortForm, string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == longForm || (!string.IsNullOrEmpty(shortForm) && args[i] == shortForm))
                return i + 1 < args.Length ? args[i + 1] : null;
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
        Console.Error.WriteLine("Commands: info, text, pages, merge, split, rotate, watermark, compress, encrypt, decrypt, metadata, bookmarks, img2pdf, weave, stamp");
        return 1;
    }
}
