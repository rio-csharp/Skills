#!/usr/bin/env dotnet
#:package DocumentFormat.OpenXml@3.3.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

var exitCode = new DocxTool().Run(args);
return exitCode;

class DocxTool
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
            TableBorderColor = "auto",
            LinkColor = "0563C1",
            HeadingFont = "Arial",
            BodyFont = "Arial",
            CodeFont = "Consolas",
            Heading1Size = 32, Heading2Size = 26, Heading3Size = 24,
            Heading4Size = 24, Heading5Size = 24, Heading6Size = 24,
            BodySize = 24, CodeSize = 20,
            HeadingSpacingBefore = "240", HeadingSpacingAfter = "60",
            ParagraphSpacingAfter = "120",
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
            HeadingFont = "Calibri",
            BodyFont = "Calibri",
            CodeFont = "Courier New",
            Heading1Size = 36, Heading2Size = 28, Heading3Size = 24,
            Heading4Size = 24, Heading5Size = 22, Heading6Size = 22,
            BodySize = 22, CodeSize = 20,
            HeadingSpacingBefore = "200", HeadingSpacingAfter = "40",
            ParagraphSpacingAfter = "100",
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
            HeadingFont = "Segoe UI",
            BodyFont = "Segoe UI",
            CodeFont = "Fira Code",
            Heading1Size = 36, Heading2Size = 30, Heading3Size = 26,
            Heading4Size = 26, Heading5Size = 24, Heading6Size = 24,
            BodySize = 22, CodeSize = 20,
            HeadingSpacingBefore = "300", HeadingSpacingAfter = "80",
            ParagraphSpacingAfter = "120",
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
            HeadingFont = "Helvetica Neue",
            BodyFont = "Helvetica Neue",
            CodeFont = "SF Mono",
            Heading1Size = 32, Heading2Size = 28, Heading3Size = 24,
            Heading4Size = 24, Heading5Size = 24, Heading6Size = 24,
            BodySize = 22, CodeSize = 20,
            HeadingSpacingBefore = "200", HeadingSpacingAfter = "40",
            ParagraphSpacingAfter = "80",
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
            HeadingFont = "Georgia",
            BodyFont = "Georgia",
            CodeFont = "Courier New",
            Heading1Size = 36, Heading2Size = 28, Heading3Size = 26,
            Heading4Size = 26, Heading5Size = 24, Heading6Size = 24,
            BodySize = 24, CodeSize = 20,
            HeadingSpacingBefore = "280", HeadingSpacingAfter = "80",
            ParagraphSpacingAfter = "120",
            HeadingBold = true,
            TableBorders = true
        }
    };

    public int Run(string[] args)
    {
        if (args.Length == 0) return Fail("No command. Try: docx read --input file.docx");
        var cmd = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        return cmd switch
        {
            "read" => CmdRead(rest),
            "create" => CmdCreate(rest),
            "modify" => CmdModify(rest),
            "convert" => CmdConvert(rest),
            "insert-table" => CmdInsertTable(rest),
            "insert-image" => CmdInsertImage(rest),
            "set-properties" => CmdSetProperties(rest),
            _ => Fail($"Unknown command: {cmd}")
        };
    }

    int CmdRead(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("read: --input required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        using var doc = WordprocessingDocument.Open(input, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Fail("Invalid docx: no body found");

        var props = doc.PackageProperties;
        Console.WriteLine("file: " + Path.GetFullPath(input));
        Console.WriteLine("title: " + (props.Title ?? ""));
        Console.WriteLine("author: " + (props.Creator ?? ""));
        Console.WriteLine("subject: " + (props.Subject ?? ""));
        Console.WriteLine("keywords: " + (props.Keywords ?? ""));
        Console.WriteLine("created: " + (props.Created?.ToString() ?? ""));
        Console.WriteLine("modified: " + (props.Modified?.ToString() ?? ""));

        Console.WriteLine();
        Console.WriteLine("=== Content ===");
        foreach (var para in body.Descendants<Paragraph>())
        {
            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            var prefix = styleId != null ? $"[{styleId}] " : "";
            Console.WriteLine(prefix + para.InnerText);
        }
        return 0;
    }

    int CmdCreate(string[] args)
    {
        var output = Arg("--output", "-o", args);
        if (string.IsNullOrEmpty(output)) return Fail("create: --output required");
        var content = Arg("--content", "-c", args);
        var contentFile = Arg("--content-file", "-cf", args);
        var fromLines = Flag("--from-lines", "-l", args);
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

        var doc = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        EnsureStyles(mainPart);
        EnsureNumbering(mainPart);
        EnsureSectionProperties(body);

        if (!string.IsNullOrEmpty(content))
        {
            if (fromLines)
                ParseLineFormat(content, body, mainPart);
            else
            {
                var lines = content.Split('\n', StringSplitOptions.None);
                foreach (var line in lines)
                {
                    var para = body.AppendChild(new Paragraph());
                    var run = para.AppendChild(new Run());
                    run.AppendChild(new Text(line));
                }
            }
        }
        doc.Save();
        Console.WriteLine("Created: " + Path.GetFullPath(output));
        return 0;
    }

    int CmdModify(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("modify: --input required");
        var output = Arg("--output", "-o", args);
        var content = Arg("--content", "-c", args);
        var fromLines = Flag("--from-lines", "-l", args);
        var appendText = Arg("--append", "", args) ?? content ?? "";
        var styleName = Arg("--style", "-s", args);

        if (!File.Exists(input)) return Fail($"File not found: {input}");

        var targetPath = output ?? input;
        if (output != null && output != input)
        {
            File.Copy(input, output, true);
            input = output;
        }

        using var doc = WordprocessingDocument.Open(input, true);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Fail("Invalid docx: no body found");

        if (!string.IsNullOrEmpty(styleName) && Themes.TryGetValue(styleName, out var theme))
            _theme = theme;

        EnsureStyles(doc.MainDocumentPart!);
        EnsureNumbering(doc.MainDocumentPart!);

        if (fromLines)
            ParseLineFormat(appendText, body, doc.MainDocumentPart!);
        else
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(appendText));
        }

        doc.Save();
        Console.WriteLine("Modified: " + Path.GetFullPath(targetPath));
        return 0;
    }

    int CmdConvert(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("convert: --input required");
        var output = Arg("--output", "-o", args);
        var format = Arg("--format", "-f", args)?.ToLowerInvariant() ?? "txt";

        if (!File.Exists(input)) return Fail($"File not found: {input}");

        using var doc = WordprocessingDocument.Open(input, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Fail("Invalid docx: no body found");

        if (format == "txt")
        {
            var outPath = output ?? Path.ChangeExtension(input, ".txt");
            var text = string.Join(Environment.NewLine + Environment.NewLine, body.Elements<Paragraph>().Select(p => p.InnerText));
            File.WriteAllText(outPath, text);
            Console.WriteLine("Converted to TXT: " + Path.GetFullPath(outPath));
        }
        else if (format == "html")
        {
            var outPath = output ?? Path.ChangeExtension(input, ".html");
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\"><title>");
            sb.Append(System.Net.WebUtility.HtmlEncode(doc.PackageProperties.Title ?? "Document"));
            sb.AppendLine("</title><style>body{font-family:Arial,sans-serif;max-width:800px;margin:40px auto;line-height:1.6;}h1,h2,h3,h4{color:#333;}table{border-collapse:collapse;width:100%;}td,th{border:1px solid #ddd;padding:8px;}th{background:#f2f2f2;}blockquote{border-left:4px solid #ccc;margin:0;padding-left:16px;color:#666;}code{background:#f4f4f4;padding:2px 4px;border-radius:3px;}pre{background:#f4f4f4;padding:12px;border-radius:4px;overflow-x:auto;}</style></head><body>");
            sb.AppendLine(DocxToHtml(body));
            sb.AppendLine("</body></html>");
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine("Converted to HTML: " + Path.GetFullPath(outPath));
        }
        else if (format == "md")
        {
            var outPath = output ?? Path.ChangeExtension(input, ".md");
            var md = DocxToMarkdown(body);
            File.WriteAllText(outPath, md);
            Console.WriteLine("Converted to Markdown: " + Path.GetFullPath(outPath));
        }
        else if (format == "pdf")
        {
            return Fail("convert --format pdf: requires additional PDF rendering library not bundled in this helper. Use the pdf skill instead for PDF conversion from docx source.");
        }
        else
        {
            return Fail($"convert --format: unknown format '{format}'. Use txt, html, md, or pdf (requires external tool).");
        }
        return 0;
    }

    int CmdInsertTable(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("insert-table: --input required");
        var output = Arg("--output", "-o", args);
        var rowsStr = Arg("--rows", "-r", args);
        var colsStr = Arg("--cols", "-c", args);
        var data = Arg("--data", "-d", args);
        var header = Flag("--header", "-h", args);

        if (!File.Exists(input)) return Fail($"File not found: {input}");
        if (!int.TryParse(rowsStr, out var rows) || rows < 1) return Fail("insert-table: --rows must be a positive integer");
        if (!int.TryParse(colsStr, out var cols) || cols < 1) return Fail("insert-table: --cols must be a positive integer");

        var targetPath = output ?? input;
        if (output != null && output != input)
        {
            File.Copy(input, output, true);
            input = output;
        }

        using var doc = WordprocessingDocument.Open(input, true);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Fail("Invalid docx: no body found");

        string[][]? cellData = null;
        if (!string.IsNullOrEmpty(data))
        {
            cellData = data.Split("\\n")
                .Select(line => line.Split(',').Select(c => c.Trim()).ToArray())
                .ToArray();
        }

        var table = BuildTable(rows, cols, cellData, header);
        body.Append(table);
        doc.Save();
        Console.WriteLine("Table inserted: " + Path.GetFullPath(targetPath));
        return 0;
    }

    int CmdInsertImage(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("insert-image: --input required");
        var output = Arg("--output", "-o", args);
        var imagePath = Arg("--image", "", args);
        var widthStr = Arg("--width", "", args);
        var heightStr = Arg("--height", "", args);

        if (!File.Exists(input)) return Fail($"File not found: {input}");
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return Fail("insert-image: --image must point to an existing file");

        long widthEmu = 5760000;
        long heightEmu = 3240000;

        if (!string.IsNullOrEmpty(widthStr) && long.TryParse(widthStr, out var w)) widthEmu = w * 914400 / 10;
        if (!string.IsNullOrEmpty(heightStr) && long.TryParse(heightStr, out var h)) heightEmu = h * 914400 / 10;

        var targetPath = output ?? input;
        if (output != null && output != input)
        {
            File.Copy(input, output, true);
            input = output;
        }

        using var doc = WordprocessingDocument.Open(input, true);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Fail("Invalid docx: no body found");

        InsertImageIntoBody(doc.MainDocumentPart!, body, imagePath, widthEmu, heightEmu);
        doc.Save();
        Console.WriteLine("Image inserted: " + Path.GetFullPath(targetPath));
        return 0;
    }

    int CmdSetProperties(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("set-properties: --input required");
        var title = Arg("--title", "-t", args);
        var author = Arg("--author", "-a", args);
        var subject = Arg("--subject", "-s", args);
        var keywords = Arg("--keywords", "-k", args);

        if (!File.Exists(input)) return Fail($"File not found: {input}");

        using var doc = WordprocessingDocument.Open(input, true);
        var props = doc.PackageProperties;
        if (title != null) props.Title = title;
        if (author != null) props.Creator = author;
        if (subject != null) props.Subject = subject;
        if (keywords != null) props.Keywords = keywords;

        doc.Save();
        Console.WriteLine("Properties updated: " + Path.GetFullPath(input));
        return 0;
    }

    // ------------------------------------------------------------------
    // LINE FORMAT PARSER
    // ------------------------------------------------------------------
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

    void ParseLineFormat(string content, Body body, MainDocumentPart mainPart)
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
                case "H1": AddHeading(body, 1, text); break;
                case "H2": AddHeading(body, 2, text); break;
                case "H3": AddHeading(body, 3, text); break;
                case "H4": AddHeading(body, 4, text); break;
                case "H5": AddHeading(body, 5, text); break;
                case "H6": AddHeading(body, 6, text); break;
                case "P": AddFormattedParagraph(body, text); break;
                case "B": AddFormattedParagraph(body, text, bold: true); break;
                case "I": AddFormattedParagraph(body, text, italic: true); break;
                case "U": AddFormattedParagraph(body, text, underline: true); break;
                case "S": AddFormattedParagraph(body, text, strike: true); break;
                case "BI": AddFormattedParagraph(body, text, bold: true, italic: true); break;
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
                        AddCodeBlock(body, codeLines);
                        idx = nextIdx - 1;
                    }
                    break;
                case "QUOTE": AddQuote(body, text); break;
                case "BULLET": AddBullet(body, text, mainPart); break;
                case "NUMBER": AddNumbered(body, text, mainPart); break;
                case "HR": AddHorizontalRule(body); break;
                case "BR": body.AppendChild(new Paragraph()); break;
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
                        AddTableFromLine(body, string.Join(";", tableLines));
                        idx = nextIdx - 1;
                    }
                    break;
                case "IMG": AddImageFromLine(body, mainPart, text); break;
                case "COLOR": _currentColor = text.Trim(); break;
                case "SIZE": _currentSize = text.Trim(); break;
                case "FONT": _currentFont = text.Trim(); break;
                case "ALIGN": _currentAlign = text.Trim().ToLowerInvariant(); break;
                default:
                    AddFormattedParagraph(body, line);
                    break;
            }
        }
    }

    void AddHeading(Body body, int level, string text)
    {
        var para = new Paragraph();
        var pPr = new ParagraphProperties(new ParagraphStyleId { Val = $"Heading{level}" });
        ApplyParaAlign(pPr);
        para.Append(pPr);
        var run = para.AppendChild(new Run());
        ApplyRunFormat(run);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        body.Append(para);
    }

    void AddFormattedParagraph(Body body, string text, bool bold = false, bool italic = false, bool underline = false, bool strike = false)
    {
        var para = new Paragraph();
        var pPr = new ParagraphProperties();
        ApplyParaAlign(pPr);
        para.Append(pPr);
        var run = para.AppendChild(new Run());
        var rPr = new RunProperties();
        if (bold) rPr.Append(new Bold());
        if (italic) rPr.Append(new Italic());
        if (underline) rPr.Append(new Underline { Val = UnderlineValues.Single });
        if (strike) rPr.Append(new Strike());
        ApplyRunFormat(run, rPr);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        body.Append(para);
    }

    static readonly HashSet<string> CSharpKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue",
        "decimal","default","delegate","do","double","else","enum","event","explicit","extern","false","finally",
        "fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal","is","lock","long",
        "namespace","new","null","object","operator","out","override","params","private","protected","public",
        "readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc","static","string","struct","switch",
        "this","throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using","virtual","void",
        "volatile","while","async","await","var","dynamic","add","remove","get","set","yield","partial","record",
        "init","required","when","nameof","with"
    };

    void AddCodeBlock(Body body, List<string> lines)
    {
        var para = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = _theme.CodeBackground });
        pPr.Append(new SpacingBetweenLines { After = "0", Before = "0", Line = "280", LineRule = LineSpacingRuleValues.Auto });
        ApplyParaAlign(pPr);
        para.Append(pPr);

        for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
        {
            var tokens = HighlightCodeLine(lines[lineIdx]);
            foreach (var (tokenText, color) in tokens)
            {
                var run = para.AppendChild(new Run());
                var rPr = new RunProperties();
                rPr.Append(new RunFonts { Ascii = _theme.CodeFont, HighAnsi = _theme.CodeFont, EastAsia = _theme.CodeFont });
                rPr.Append(new FontSize { Val = _theme.CodeSize.ToString() });
                if (color != null) rPr.Append(new Color { Val = color });
                run.AppendChild(rPr);
                run.AppendChild(new Text(tokenText) { Space = SpaceProcessingModeValues.Preserve });
            }
            if (lineIdx < lines.Count - 1)
            {
                var brRun = para.AppendChild(new Run());
                brRun.AppendChild(new Break());
            }
        }
        body.Append(para);

        var spacer = new Paragraph();
        var spacerPpr = new ParagraphProperties();
        spacerPpr.Append(new SpacingBetweenLines { After = _theme.ParagraphSpacingAfter, Before = "0" });
        spacer.Append(spacerPpr);
        body.Append(spacer);
    }

    List<(string text, string? color)> HighlightCodeLine(string line)
    {
        var result = new List<(string text, string? color)>();
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//") || trimmed.StartsWith("#"))
        {
            result.Add((line, "6A9955"));
            return result;
        }

        int i = 0;
        while (i < line.Length)
        {
            if (i < line.Length - 1 && line[i] == '/' && line[i + 1] == '/')
            {
                result.Add((line[i..], "6A9955"));
                break;
            }
            if (line[i] == '#')
            {
                if (i == 0 || !(char.IsLetterOrDigit(line[i - 1]) || line[i - 1] == '_'))
                {
                    result.Add((line[i..], "6A9955"));
                    break;
                }
                int hashStart = i;
                i++;
                result.Add((line[hashStart..i], null));
                continue;
            }

            if (line[i] == '"' || line[i] == '\'')
            {
                char quote = line[i];
                int start = i;
                i++;
                while (i < line.Length && line[i] != quote)
                {
                    if (line[i] == '\\' && i + 1 < line.Length) i += 2;
                    else i++;
                }
                if (i < line.Length) i++;
                result.Add((line[start..i], "CE9178"));
                continue;
            }

            if (char.IsLetter(line[i]) || line[i] == '_')
            {
                int start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
                var word = line[start..i];
                if (CSharpKeywords.Contains(word))
                    result.Add((word, "569CD6"));
                else
                    result.Add((word, null));
                continue;
            }

            int textStart = i;
            while (i < line.Length
                   && line[i] != '"' && line[i] != '\'' && line[i] != '#'
                   && !(i < line.Length - 1 && line[i] == '/' && line[i + 1] == '/')
                   && !char.IsLetter(line[i]) && line[i] != '_')
            {
                i++;
            }
            if (textStart < i)
                result.Add((line[textStart..i], null));
        }

        return result;
    }

    void AddQuote(Body body, string text)
    {
        var para = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.Append(new ParagraphBorders(
            new TopBorder { Val = BorderValues.Nil },
            new LeftBorder { Val = BorderValues.Single, Size = 24, Color = _theme.QuoteBorderColor, Space = 10 }
        ));
        pPr.Append(new SpacingBetweenLines { After = _theme.ParagraphSpacingAfter, Before = _theme.ParagraphSpacingAfter });
        pPr.Append(new Indentation { Left = "720" });
        ApplyParaAlign(pPr);
        para.Append(pPr);
        var run = para.AppendChild(new Run());
        var rPr = new RunProperties(new Color { Val = _theme.QuoteTextColor });
        ApplyRunFormat(run, rPr);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        body.Append(para);
    }

    void AddBullet(Body body, string text, MainDocumentPart mainPart)
    {
        var para = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.Append(new NumberingProperties(new NumberingLevelReference { Val = 0 }, new NumberingId { Val = 1 }));
        ApplyParaAlign(pPr);
        para.Append(pPr);
        var run = para.AppendChild(new Run());
        ApplyRunFormat(run);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        body.Append(para);
    }

    void AddNumbered(Body body, string text, MainDocumentPart mainPart)
    {
        var para = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.Append(new NumberingProperties(new NumberingLevelReference { Val = 0 }, new NumberingId { Val = 2 }));
        ApplyParaAlign(pPr);
        para.Append(pPr);
        var run = para.AppendChild(new Run());
        ApplyRunFormat(run);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        body.Append(para);
    }

    void AddHorizontalRule(Body body)
    {
        body.AppendChild(new Paragraph(new ParagraphProperties(new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "auto" }))));
    }

    void AddTableFromLine(Body body, string text)
    {
        var rows = text.Split(';').Select(r => r.Split(',').Select(c => c.Trim()).ToArray()).ToArray();
        if (rows.Length == 0) return;
        var maxCols = rows.Max(r => r.Length);
        var table = BuildTable(rows.Length, maxCols, rows, header: false);
        body.Append(table);
    }

    void AddImageFromLine(Body body, MainDocumentPart mainPart, string text)
    {
        var parts = text.Split(',').Select(p => p.Trim()).ToArray();
        var imagePath = parts[0];
        if (!File.Exists(imagePath)) return;

        long widthEmu = 0, heightEmu = 0;
        bool autoSize = true;

        if (parts.Length > 1)
        {
            var widthSpec = parts[1];
            if (widthSpec.EndsWith("%"))
            {
                if (double.TryParse(widthSpec.TrimEnd('%'), out var pct))
                {
                    var availWidth = GetPageContentWidth(mainPart);
                    widthEmu = (long)(availWidth * pct / 100.0);
                    autoSize = false;
                }
            }
            else if (long.TryParse(widthSpec, out var mmWidth))
            {
                widthEmu = mmWidth * 914400 / 10;
                autoSize = false;
            }
        }

        if (parts.Length > 2 && long.TryParse(parts[2], out var mmHeight))
        {
            heightEmu = mmHeight * 914400 / 10;
        }
        else if (widthEmu > 0)
        {
            if (GetImageDimensions(imagePath, out var imgW, out var imgH))
                heightEmu = widthEmu * imgH / imgW;
            else
                heightEmu = widthEmu * 3 / 4;
        }

        if (autoSize)
        {
            if (GetImageDimensions(imagePath, out var imgW, out var imgH))
            {
                var availWidth = GetPageContentWidth(mainPart);
                long pixelWidthEmu = (long)imgW * 914400 / 96;
                long pixelHeightEmu = (long)imgH * 914400 / 96;
                if (pixelWidthEmu > availWidth)
                {
                    var scale = (double)availWidth / pixelWidthEmu;
                    widthEmu = availWidth;
                    heightEmu = (long)(pixelHeightEmu * scale);
                }
                else
                {
                    widthEmu = pixelWidthEmu;
                    heightEmu = pixelHeightEmu;
                }
            }
            else
            {
                widthEmu = 3600000;
                heightEmu = 2700000;
            }
        }

        InsertImageIntoBody(mainPart, body, imagePath, widthEmu, heightEmu);
    }

    void EnsureSectionProperties(Body body)
    {
        if (body.Elements<SectionProperties>().LastOrDefault() == null)
        {
            body.Append(new SectionProperties(
                new PageSize { Width = 11906, Height = 16838 },
                new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 }
            ));
        }
    }

    long GetPageContentWidth(MainDocumentPart mainPart)
    {
        var body = mainPart.Document?.Body;
        if (body == null) return 3600000;

        var sectPr = body.Elements<SectionProperties>().LastOrDefault();
        if (sectPr == null) sectPr = body.AppendChild(new SectionProperties());

        var pageSize = sectPr.GetFirstChild<PageSize>();
        var pageMargin = sectPr.GetFirstChild<PageMargin>();

        long pageWidth = pageSize?.Width?.Value != null ? (long)pageSize.Width.Value * 635 : 11905200;
        long leftMargin = pageMargin?.Left?.Value != null ? (long)pageMargin.Left.Value * 635 : 1440000;
        long rightMargin = pageMargin?.Right?.Value != null ? (long)pageMargin.Right.Value * 635 : 1440000;

        return Math.Max(pageWidth - leftMargin - rightMargin, 3600000);
    }

    bool GetImageDimensions(string path, out int width, out int height)
    {
        width = 0; height = 0;
        try
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            if (ext == ".png")
            {
                fs.Position = 16;
                var buf = new byte[8];
                _ = fs.Read(buf, 0, 8);
                width = (buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3];
                height = (buf[4] << 24) | (buf[5] << 16) | (buf[6] << 8) | buf[7];
                return true;
            }
            else if (ext == ".gif")
            {
                fs.Position = 6;
                var buf = new byte[4];
                _ = fs.Read(buf, 0, 4);
                width = buf[0] | (buf[1] << 8);
                height = buf[2] | (buf[3] << 8);
                return true;
            }
            else if (ext == ".jpg" || ext == ".jpeg")
            {
                var buf = new byte[4096];
                int read = fs.Read(buf, 0, buf.Length);
                for (int i = 0; i < read - 9; i++)
                {
                    if (buf[i] == 0xFF && (buf[i + 1] == 0xC0 || buf[i + 1] == 0xC2))
                    {
                        height = (buf[i + 5] << 8) | buf[i + 6];
                        width = (buf[i + 7] << 8) | buf[i + 8];
                        return true;
                    }
                }
            }
            else if (ext == ".bmp")
            {
                fs.Position = 18;
                var buf = new byte[8];
                _ = fs.Read(buf, 0, 8);
                width = buf[0] | (buf[1] << 8) | (buf[2] << 16) | (buf[3] << 24);
                height = buf[4] | (buf[5] << 8) | (buf[6] << 16) | (buf[7] << 24);
                return true;
            }
        }
        catch { }
        return false;
    }

    void ApplyParaAlign(ParagraphProperties pPr)
    {
        if (_currentAlign == "left") pPr.Append(new Justification { Val = JustificationValues.Left });
        else if (_currentAlign == "center") pPr.Append(new Justification { Val = JustificationValues.Center });
        else if (_currentAlign == "right") pPr.Append(new Justification { Val = JustificationValues.Right });
        else if (_currentAlign == "justify") pPr.Append(new Justification { Val = JustificationValues.Both });
    }

    void ApplyRunFormat(Run run, RunProperties? existing = null)
    {
        var rPr = existing ?? new RunProperties();
        var fontName = !string.IsNullOrEmpty(_currentFont) ? _currentFont : _theme.BodyFont;
        var existingFonts = rPr.GetFirstChild<RunFonts>();
        if (existingFonts != null)
        {
            existingFonts.Ascii = fontName;
            existingFonts.HighAnsi = fontName;
            existingFonts.EastAsia = fontName;
        }
        else
        {
            rPr.PrependChild(new RunFonts { Ascii = fontName, HighAnsi = fontName, EastAsia = fontName });
        }
        if (!string.IsNullOrEmpty(_currentColor)) rPr.Append(new Color { Val = _currentColor });
        if (!string.IsNullOrEmpty(_currentSize) && int.TryParse(_currentSize, out var sz)) rPr.Append(new FontSize { Val = sz.ToString() });
        if (rPr.HasChildren) run.PrependChild(rPr);
    }

    // ------------------------------------------------------------------
    // TABLE BUILDER
    // ------------------------------------------------------------------
    Table BuildTable(int rows, int cols, string[][]? cellData, bool header)
    {
        var table = new Table();
        var tblProp = new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        if (_theme.TableBorders)
        {
            tblProp.Append(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = _theme.TableBorderColor },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = _theme.TableBorderColor },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = _theme.TableBorderColor },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = _theme.TableBorderColor }
            ));
        }
        table.AppendChild(tblProp);
        var tblGrid = new TableGrid();
        for (int c = 0; c < cols; c++) tblGrid.Append(new GridColumn());
        table.AppendChild(tblGrid);

        for (int r = 0; r < rows; r++)
        {
            var tr = new TableRow();
            for (int c = 0; c < cols; c++)
            {
                var tc = new TableCell();
                var tcProp = new TableCellProperties();
                if (header && r == 0)
                {
                    tcProp.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = _theme.TableHeaderFill });
                }
                tc.Append(tcProp);
                var para = new Paragraph();
                var run = new Run();
                var cellText = cellData != null && r < cellData.Length && c < cellData[r].Length ? cellData[r][c] : "";
                run.Append(new Text(cellText));
                if (header && r == 0) run.RunProperties = new RunProperties(new Bold());
                para.Append(run);
                tc.Append(para);
                tr.Append(tc);
            }
            table.Append(tr);
        }
        return table;
    }

    // ------------------------------------------------------------------
    // IMAGE INSERTION
    // ------------------------------------------------------------------
    void InsertImageIntoBody(MainDocumentPart mainPart, Body body, string imagePath, long widthEmu, long heightEmu)
    {
        var ext = Path.GetExtension(imagePath).ToLowerInvariant();
        var imagePart = ext switch
        {
            ".png" => mainPart.AddImagePart(ImagePartType.Png),
            ".gif" => mainPart.AddImagePart(ImagePartType.Gif),
            ".bmp" => mainPart.AddImagePart(ImagePartType.Bmp),
            ".tiff" or ".tif" => mainPart.AddImagePart(ImagePartType.Tiff),
            _ => mainPart.AddImagePart(ImagePartType.Jpeg),
        };

        using (var imgStream = new FileStream(imagePath, FileMode.Open))
        {
            imagePart.FeedData(imgStream);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);

        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                new DW.DocProperties { Id = 1, Name = "Picture" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks()),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0, Name = imagePath },
                                new PIC.NonVisualPictureDrawingProperties()
                            ),
                            new PIC.BlipFill(new A.Blip { Embed = relationshipId }, new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(new A.Offset { X = 0, Y = 0 }, new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                            )
                        )
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            ) { DistanceFromTop = 0, DistanceFromBottom = 0, DistanceFromLeft = 0, DistanceFromRight = 0 }
        );

        var para = new Paragraph();
        var pPr = new ParagraphProperties();
        if (string.IsNullOrEmpty(_currentAlign))
            pPr.Append(new Justification { Val = JustificationValues.Center });
        else
            ApplyParaAlign(pPr);
        para.Append(pPr);
        para.Append(new Run(drawing));
        body.Append(para);
    }

    // ------------------------------------------------------------------
    // DOCX -> HTML
    // ------------------------------------------------------------------
    string DocxToHtml(Body body)
    {
        var sb = new StringBuilder();
        string? currentListType = null;

        foreach (var para in body.Elements<Paragraph>())
        {
            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            var align = para.ParagraphProperties?.Justification?.Val?.Value;
            var indent = para.ParagraphProperties?.Indentation?.Left?.Value;
            var numProps = para.ParagraphProperties?.NumberingProperties;
            var shading = para.ParagraphProperties?.Shading?.Fill?.Value;

            string? listType = null;
            if (numProps != null)
            {
                var numId = numProps.NumberingId?.Val?.Value;
                listType = numId == 1 ? "ul" : "ol";
            }

            if (listType != currentListType)
            {
                if (currentListType != null) sb.AppendLine($"</{currentListType}>");
                if (listType != null) sb.AppendLine($"<{listType}>");
                currentListType = listType;
            }

            string tag = listType != null ? "li" : "p";
            if (styleId != null && styleId.StartsWith("Heading")) tag = $"h{styleId[^1]}";

            var styleParts = new List<string>();
            if (align != null) styleParts.Add($"text-align:{align}");
            if (indent != null && listType == null) styleParts.Add($"margin-left:{indent}pt");
            if (shading != null && shading != "auto") styleParts.Add($"background:#{shading}");

            sb.Append($"<{tag}");
            if (styleParts.Count > 0) sb.Append($" style=\"{string.Join(";", styleParts)}\"");
            sb.Append(">");

            foreach (var run in para.Elements<Run>())
            {
                var rPr = run.RunProperties;
                var isBold = rPr?.Bold != null;
                var isItalic = rPr?.Italic != null;
                var isStrike = rPr?.Strike != null;
                var color = rPr?.Color?.Val?.Value;
                var fontSize = rPr?.FontSize?.Val?.Value;
                var runShading = rPr?.Shading?.Fill?.Value;
                var runFont = rPr?.RunFonts?.Ascii?.Value;

                var openTags = new List<string>();
                var styles = new List<string>();
                if (isBold) openTags.Add("strong");
                if (isItalic) openTags.Add("em");
                if (isStrike) openTags.Add("del");
                if (color != null) styles.Add($"color:#{color}");
                if (fontSize != null && int.TryParse(fontSize, out var fs)) styles.Add($"font-size:{fs / 2}pt");
                if (runShading != null && runShading != "auto") styles.Add($"background:#{runShading}");
                if (runFont != null && runFont.Contains("Consolas")) styles.Add("font-family:Consolas,monospace");

                foreach (var t in openTags) sb.Append($"<{t}>");
                if (styles.Count > 0) sb.Append($"<span style=\"{string.Join(";", styles)}\">");

                var text = string.Join("", run.Elements<Text>().Select(t => System.Net.WebUtility.HtmlEncode(t.Text)));
                sb.Append(text);

                if (styles.Count > 0) sb.Append("</span>");
                foreach (var t in openTags.AsEnumerable().Reverse()) sb.Append($"</{t}>");
            }

            sb.AppendLine($"</{tag}>");
        }

        if (currentListType != null) sb.AppendLine($"</{currentListType}>");

        foreach (var table in body.Elements<Table>())
        {
            sb.AppendLine("<table>");
            foreach (var row in table.Elements<TableRow>())
            {
                sb.AppendLine("<tr>");
                foreach (var cell in row.Elements<TableCell>())
                {
                    sb.Append("<td>");
                    sb.Append(string.Join(" ", cell.Elements<Paragraph>().Select(p => System.Net.WebUtility.HtmlEncode(p.InnerText))));
                    sb.AppendLine("</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }

        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // DOCX -> Markdown
    // ------------------------------------------------------------------
    string DocxToMarkdown(Body body)
    {
        var sb = new StringBuilder();
        foreach (var para in body.Elements<Paragraph>())
        {
            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            var prefix = "";
            if (styleId != null && styleId.StartsWith("Heading") && int.TryParse(styleId[^1..], out var hl))
                prefix = new string('#', hl) + " ";

            var text = para.InnerText;
            foreach (var run in para.Elements<Run>())
            {
                if (run.RunProperties?.Bold != null)
                    text = text.Replace(run.InnerText, $"**{run.InnerText}**");
                if (run.RunProperties?.Italic != null)
                    text = text.Replace(run.InnerText, $"*{run.InnerText}*");
            }

            sb.AppendLine(prefix + text);
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    void EnsureStyles(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.StyleDefinitionsPart;
        if (stylesPart == null)
        {
            stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles();
        }

        var existing = stylesPart.Styles?.Elements<Style>().Select(s => s.StyleId?.Value).ToHashSet() ?? new HashSet<string?>();

        for (int i = 1; i <= 6; i++)
        {
            var id = $"Heading{i}";
            if (!existing.Contains(id))
            {
                var size = i switch { 1 => _theme.Heading1Size, 2 => _theme.Heading2Size, 3 => _theme.Heading3Size, 4 => _theme.Heading4Size, 5 => _theme.Heading5Size, _ => _theme.Heading6Size };
                var rPr = new RunProperties();
                rPr.Append(new RunFonts { Ascii = _theme.HeadingFont, HighAnsi = _theme.HeadingFont, EastAsia = _theme.HeadingFont });
                if (_theme.HeadingBold) rPr.Append(new Bold());
                rPr.Append(new Color { Val = _theme.HeadingColor });
                rPr.Append(new FontSize { Val = size.ToString() });

                var style = new Style(
                    new StyleName { Val = $"heading {i}" },
                    new BasedOn { Val = "Normal" },
                    new NextParagraphStyle { Val = "Normal" },
                    new StyleParagraphProperties(
                        new KeepNext(),
                        new KeepLines(),
                        new SpacingBetweenLines { Before = _theme.HeadingSpacingBefore, After = _theme.HeadingSpacingAfter },
                        new OutlineLevel { Val = i - 1 }
                    ),
                    rPr
                ) { Type = StyleValues.Paragraph, StyleId = id };
                stylesPart.Styles!.Append(style);
            }
        }
    }

    void EnsureNumbering(MainDocumentPart mainPart)
    {
        var numberingPart = mainPart.NumberingDefinitionsPart;
        if (numberingPart == null)
        {
            numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
            numberingPart.Numbering = new Numbering();
        }

        var existingAbstract = numberingPart.Numbering?.Elements<AbstractNum>().Select(a => a.AbstractNumberId?.Value).ToHashSet() ?? new HashSet<int?>();
        var existingInstance = numberingPart.Numbering?.Elements<NumberingInstance>().Select(n => n.NumberID?.Value).ToHashSet() ?? new HashSet<int?>();

        if (!existingAbstract.Contains(1))
        {
            var abstractNum1 = new AbstractNum(
                new Level(
                    new NumberingFormat { Val = NumberFormatValues.Bullet },
                    new LevelText { Val = "\u2022" },
                    new ParagraphProperties(new Indentation { Left = "720", Hanging = "360" })
                ) { LevelIndex = 0 },
                new Level(
                    new NumberingFormat { Val = NumberFormatValues.Bullet },
                    new LevelText { Val = "\u25E6" },
                    new ParagraphProperties(new Indentation { Left = "1440", Hanging = "360" })
                ) { LevelIndex = 1 }
            ) { AbstractNumberId = 1 };
            numberingPart.Numbering!.Append(abstractNum1);
        }

        if (!existingAbstract.Contains(2))
        {
            var abstractNum2 = new AbstractNum(
                new Level(
                    new NumberingFormat { Val = NumberFormatValues.Decimal },
                    new LevelText { Val = "%1." },
                    new ParagraphProperties(new Indentation { Left = "720", Hanging = "360" })
                ) { LevelIndex = 0 },
                new Level(
                    new NumberingFormat { Val = NumberFormatValues.LowerLetter },
                    new LevelText { Val = "%2." },
                    new ParagraphProperties(new Indentation { Left = "1440", Hanging = "360" })
                ) { LevelIndex = 1 }
            ) { AbstractNumberId = 2 };
            numberingPart.Numbering!.Append(abstractNum2);
        }

        if (!existingInstance.Contains(1))
        {
            var instance1 = new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 1 };
            numberingPart.Numbering!.Append(instance1);
        }

        if (!existingInstance.Contains(2))
        {
            var instance2 = new NumberingInstance(new AbstractNumId { Val = 2 }) { NumberID = 2 };
            numberingPart.Numbering!.Append(instance2);
        }
    }

    string? Arg(string longForm, string shortForm, string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == longForm || (!string.IsNullOrEmpty(shortForm) && args[i] == shortForm))
                return i + 1 < args.Length ? args[i + 1] : null;
        }
        return null;
    }

    bool Flag(string longForm, string shortForm, string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == longForm || (!string.IsNullOrEmpty(shortForm) && args[i] == shortForm))
                return true;
        }
        return false;
    }

    string? Positional(string[] args)
    {
        var optionsWithValues = new HashSet<string> { "--input", "-i", "--output", "-o", "--content", "-c", "--append", "--format", "-f", "--rows", "-r", "--cols", "-c", "--data", "-d", "--image", "--width", "--height", "--font", "--font-size", "--title", "-t", "--author", "-a", "--subject", "-s", "--keywords", "-k", "--style", "-s" };
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

    int Fail(string msg)
    {
        Console.Error.WriteLine("ERROR: " + msg);
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: docx <command> [options]");
        Console.Error.WriteLine("Commands: read, create, modify, convert, insert-table, insert-image, set-properties");
        Console.Error.WriteLine("Styles: default, report, modern, minimal, elegant");
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
    public string TableBorderColor { get; set; } = "auto";
    public string LinkColor { get; set; } = "0563C1";
    public string HeadingFont { get; set; } = "Arial";
    public string BodyFont { get; set; } = "Arial";
    public string CodeFont { get; set; } = "Consolas";
    public int Heading1Size { get; set; } = 32;
    public int Heading2Size { get; set; } = 26;
    public int Heading3Size { get; set; } = 24;
    public int Heading4Size { get; set; } = 24;
    public int Heading5Size { get; set; } = 24;
    public int Heading6Size { get; set; } = 24;
    public int BodySize { get; set; } = 24;
    public int CodeSize { get; set; } = 20;
    public string HeadingSpacingBefore { get; set; } = "240";
    public string HeadingSpacingAfter { get; set; } = "60";
    public string ParagraphSpacingAfter { get; set; } = "120";
    public bool HeadingBold { get; set; } = true;
    public bool TableBorders { get; set; } = true;
}
