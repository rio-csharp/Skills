#!/usr/bin/env dotnet
#:package DocumentFormat.OpenXml@3.3.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using P = DocumentFormat.OpenXml.Presentation;

var exitCode = new PptTool().Run(args);
return exitCode;

class PptTool
{
    // ── State ───────────────────────────────────────────────────────────────
    string _currentColor = "";
    string _currentSize = "";
    string _currentFont = "";
    string _currentAlign = "";
    string _currentLayout = "title-content";
    string _currentFill = "";
    string _currentStroke = "";
    string _currentStrokeWidth = "";
    string _currentRound = "";
    string _tableBorderColor = "";
    string _tableBorderWidth = "";
    bool _tableZebra = false;
    string _tableHeaderColor = "";
    long _posX = -1, _posY = -1, _posW = -1, _posH = -1;
    StyleTheme _theme = Themes["default"];
    uint _shapeId = 1;

    const long SlideWidth = 12192000;
    const long SlideHeight = 6858000;

    // ── Themes ──────────────────────────────────────────────────────────────
    static readonly Dictionary<string, StyleTheme> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = new StyleTheme
        {
            Name = "default", HeadingColor = "2E74B5", QuoteTextColor = "666666",
            QuoteBorderColor = "CCCCCC", CodeBackground = "F5F5F5", TableHeaderFill = "D9E2F3",
            TableBorderColor = "999999", Heading1Size = 44, Heading2Size = 32, BodySize = 18,
            CodeSize = 14, BulletSize = 18, HeadingSpacingBefore = 0, HeadingSpacingAfter = 12,
            ParagraphSpacingAfter = 8, HeadingBold = true, TableBorders = true
        },
        ["report"] = new StyleTheme
        {
            Name = "report", HeadingColor = "1F4E79", QuoteTextColor = "5B5B5B",
            QuoteBorderColor = "A6A6A6", CodeBackground = "F2F2F2", TableHeaderFill = "B4C7E7",
            TableBorderColor = "7F7F7F", Heading1Size = 48, Heading2Size = 36, BodySize = 16,
            CodeSize = 14, BulletSize = 16, HeadingSpacingBefore = 0, HeadingSpacingAfter = 10,
            ParagraphSpacingAfter = 6, HeadingBold = true, TableBorders = true
        },
        ["modern"] = new StyleTheme
        {
            Name = "modern", HeadingColor = "00B4D8", QuoteTextColor = "495057",
            QuoteBorderColor = "CED4DA", CodeBackground = "F8F9FA", TableHeaderFill = "E9ECEF",
            TableBorderColor = "ADB5BD", Heading1Size = 48, Heading2Size = 36, BodySize = 16,
            CodeSize = 14, BulletSize = 16, HeadingSpacingBefore = 0, HeadingSpacingAfter = 12,
            ParagraphSpacingAfter = 8, HeadingBold = false, TableBorders = true
        },
        ["minimal"] = new StyleTheme
        {
            Name = "minimal", HeadingColor = "212529", QuoteTextColor = "6C757D",
            QuoteBorderColor = "DEE2E6", CodeBackground = "F8F9FA", TableHeaderFill = "E9ECEF",
            TableBorderColor = "ADB5BD", Heading1Size = 44, Heading2Size = 32, BodySize = 16,
            CodeSize = 14, BulletSize = 16, HeadingSpacingBefore = 0, HeadingSpacingAfter = 8,
            ParagraphSpacingAfter = 5, HeadingBold = false, TableBorders = false
        },
        ["elegant"] = new StyleTheme
        {
            Name = "elegant", HeadingColor = "4A4A4A", QuoteTextColor = "7A7A7A",
            QuoteBorderColor = "C9B99A", CodeBackground = "FDFBF7", TableHeaderFill = "F5F0E8",
            TableBorderColor = "C9B99A", Heading1Size = 48, Heading2Size = 36, BodySize = 18,
            CodeSize = 14, BulletSize = 18, HeadingSpacingBefore = 0, HeadingSpacingAfter = 12,
            ParagraphSpacingAfter = 8, HeadingBold = true, TableBorders = true
        }
    };

    // ── Layouts ─────────────────────────────────────────────────────────────
    static readonly Dictionary<string, LayoutDef> Layouts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title-content"] = new("title-content", new()
        {
            new("TitleShape", 500000, 300000, 11192000, 900000),
            new("ContentShape", 500000, 1500000, 11192000, 5200000)
        }),
        ["title"] = new("title", new()
        {
            new("TitleShape", 500000, 300000, 11192000, 6000000)
        }),
        ["two-content"] = new("two-content", new()
        {
            new("TitleShape", 500000, 300000, 11192000, 900000),
            new("LeftShape", 500000, 1500000, 5400000, 5200000),
            new("RightShape", 5900000, 1500000, 5400000, 5200000)
        }),
        ["blank"] = new("blank", new()),
        ["section-header"] = new("section-header", new()
        {
            new("TitleShape", 500000, 2000000, 11192000, 1200000),
            new("SubtitleShape", 500000, 3400000, 11192000, 600000)
        }),
        ["title-only"] = new("title-only", new()
        {
            new("TitleShape", 500000, 300000, 11192000, 6400000)
        }),
        ["comparison"] = new("comparison", new()
        {
            new("TitleShape", 500000, 300000, 11192000, 900000),
            new("LeftHeaderShape", 500000, 1300000, 5400000, 500000),
            new("LeftShape", 500000, 1900000, 5400000, 4800000),
            new("RightHeaderShape", 5900000, 1300000, 5400000, 500000),
            new("RightShape", 5900000, 1900000, 5400000, 4800000)
        })
    };

    // ── Entry Point ─────────────────────────────────────────────────────────

    public int Run(string[] args)
    {
        if (args.Length == 0) return Fail("No command. Try: ppt read --input file.pptx");
        var cmd = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        return cmd switch
        {
            "read" => CmdRead(rest),
            "create" => CmdCreate(rest),
            "modify" => CmdModify(rest),
            "merge" => CmdMerge(rest),
            "split" => CmdSplit(rest),
            "remove" => CmdRemove(rest),
            "reorder" => CmdReorder(rest),
            "notes" => CmdNotes(rest),
            "extract-notes" => CmdExtractNotes(rest),
            "set-properties" => CmdSetProperties(rest),
            "export" => CmdExport(rest),
            _ => Fail($"Unknown command: {cmd}")
        };
    }

    // ── Read ────────────────────────────────────────────────────────────────

    int CmdRead(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        if (string.IsNullOrEmpty(input)) return Fail("read: --input required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        using var doc = PresentationDocument.Open(input, false);
        var pres = doc.PresentationPart?.Presentation;
        if (pres == null) return Fail("Invalid pptx: no presentation found");

        var slideIds = pres.SlideIdList?.Elements<SlideId>() ?? Enumerable.Empty<SlideId>();
        var slides = slideIds.Select(sid => doc.PresentationPart?.GetPartById(sid.RelationshipId?.Value ?? "") as SlidePart).Where(sp => sp != null).ToList();

        Console.WriteLine("slides: " + slides.Count);
        Console.WriteLine("file: " + Path.GetFullPath(input));

        for (int i = 0; i < slides.Count; i++)
        {
            var slide = slides[i]!;
            var texts = slide.Slide?.Descendants<A.Text>()?.Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t)) ?? Enumerable.Empty<string>();
            var preview = string.Join(" ", texts.Take(5));
            if (preview.Length > 120) preview = preview[..120] + "...";
            Console.WriteLine($"  [{i + 1}] {preview}");
        }
        return 0;
    }

    // ── Create ─────────────────────────────────────────────────────────────

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

        CreateBlankPptx(output);

        using var doc = PresentationDocument.Open(output, true);
        var presPart = doc.PresentationPart!;
        var masterPart = presPart.SlideMasterParts.First();
        var layoutPart = masterPart.SlideLayoutParts.First();

        if (!string.IsNullOrEmpty(content))
        {
            if (fromLines)
                ParseLineFormat(content, presPart, layoutPart);
            else
            {
                var slide = CreateSlide(presPart, layoutPart);
                AddTextToSlide(slide, content, _theme.BodySize);
            }
        }

        presPart.Presentation.Save();
        Console.WriteLine("Created: " + Path.GetFullPath(output));
        return 0;
    }

    // ── Modify ──────────────────────────────────────────────────────────────

    int CmdModify(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        var output = Arg("--output", "-o", args);
        var content = Arg("--content", "-c", args);
        var contentFile = Arg("--content-file", "-cf", args);
        var fromLines = Flag("--from-lines", "-l", args);
        var styleName = Arg("--style", "-s", args) ?? "default";

        if (string.IsNullOrEmpty(input)) return Fail("modify: --input required");
        if (string.IsNullOrEmpty(output)) return Fail("modify: --output required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        if (!string.IsNullOrEmpty(contentFile))
        {
            if (!File.Exists(contentFile)) return Fail($"Content file not found: {contentFile}");
            content = File.ReadAllText(contentFile);
        }

        if (Themes.TryGetValue(styleName, out var theme))
            _theme = theme;

        File.Copy(input, output, true);
        using var doc = PresentationDocument.Open(output, true);
        var presPart = doc.PresentationPart!;
        var masterPart = presPart.SlideMasterParts.First();
        var layoutPart = masterPart.SlideLayoutParts.First();

        if (!string.IsNullOrEmpty(content) && fromLines)
            ParseLineFormat(content, presPart, layoutPart);

        presPart.Presentation.Save();
        Console.WriteLine("Modified: " + Path.GetFullPath(output));
        return 0;
    }

    // ── Merge ───────────────────────────────────────────────────────────────

    int CmdMerge(string[] args)
    {
        var inputs = ArgsMulti("--input", "-i", args);
        var output = Arg("--output", "-o", args);

        if (inputs.Count == 0) return Fail("merge: at least one --input required");
        if (string.IsNullOrEmpty(output)) return Fail("merge: --output required");
        foreach (var input in inputs)
            if (!File.Exists(input)) return Fail($"File not found: {input}");

        if (File.Exists(output)) File.Delete(output);
        File.Copy(inputs[0], output, true);

        using var outDoc = PresentationDocument.Open(output, true);
        var outPresPart = outDoc.PresentationPart!;

        for (int i = 1; i < inputs.Count; i++)
        {
            using var srcDoc = PresentationDocument.Open(inputs[i], false);
            var srcPresPart = srcDoc.PresentationPart!;
            var srcSlideIds = srcPresPart.Presentation.SlideIdList!.Elements<SlideId>().ToList();

            foreach (var srcSlideId in srcSlideIds)
            {
                var srcSlidePart = srcPresPart.GetPartById(srcSlideId.RelationshipId!) as SlidePart;
                if (srcSlidePart == null) continue;

                var newSlidePart = outPresPart.AddPart(srcSlidePart);
                var maxId = outPresPart.Presentation.SlideIdList!.Elements<SlideId>()
                    .Select(s => s.Id?.Value ?? 0u).DefaultIfEmpty(0u).Max();
                var newSlideId = new SlideId { Id = maxId + 256, RelationshipId = outPresPart.GetIdOfPart(newSlidePart) };
                outPresPart.Presentation.SlideIdList.Append(newSlideId);
            }
        }

        outPresPart.Presentation.Save();
        Console.WriteLine($"Merged {inputs.Count} presentations into: " + Path.GetFullPath(output));
        return 0;
    }

    // ── Split ───────────────────────────────────────────────────────────────

    int CmdSplit(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        var output = Arg("--output", "-o", args);
        var rangeStr = Arg("--range", "-r", args);

        if (string.IsNullOrEmpty(input)) return Fail("split: --input required");
        if (string.IsNullOrEmpty(output)) return Fail("split: --output required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        using var doc = PresentationDocument.Open(input, false);
        var slideIds = doc.PresentationPart!.Presentation.SlideIdList!.Elements<SlideId>().ToList();
        var selected = string.IsNullOrEmpty(rangeStr)
            ? Enumerable.Range(1, slideIds.Count).ToArray()
            : ParseRange(rangeStr, slideIds.Count);

        Directory.CreateDirectory(output);

        foreach (var idx in selected)
        {
            var slideId = slideIds[idx - 1];
            var srcSlidePart = doc.PresentationPart.GetPartById(slideId.RelationshipId!) as SlidePart;
            if (srcSlidePart == null) continue;

            var outPath = Path.Combine(output, $"slide_{idx}.pptx");
            CreateBlankPptx(outPath);

            using var outDoc = PresentationDocument.Open(outPath, true);
            var outPresPart = outDoc.PresentationPart!;
            var newSlidePart = outPresPart.AddPart(srcSlidePart);
            var newSlideId = new SlideId { Id = 256, RelationshipId = outPresPart.GetIdOfPart(newSlidePart) };
            outPresPart.Presentation.SlideIdList!.Append(newSlideId);
            outPresPart.Presentation.Save();
        }

        Console.WriteLine($"Split {selected.Length} slides to {output}");
        return 0;
    }

    // ── Remove ──────────────────────────────────────────────────────────────

    int CmdRemove(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        var output = Arg("--output", "-o", args);
        var rangeStr = Arg("--range", "-r", args);

        if (string.IsNullOrEmpty(input)) return Fail("remove: --input required");
        if (string.IsNullOrEmpty(output)) return Fail("remove: --output required");
        if (string.IsNullOrEmpty(rangeStr)) return Fail("remove: --range required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        File.Copy(input, output, true);
        using var doc = PresentationDocument.Open(output, true);
        var presPart = doc.PresentationPart!;
        var slideIds = presPart.Presentation.SlideIdList!.Elements<SlideId>().ToList();
        var toRemove = ParseRange(rangeStr, slideIds.Count);
        var toRemoveSet = new HashSet<int>(toRemove);

        foreach (var slideId in slideIds)
        {
            var idx = slideIds.IndexOf(slideId) + 1;
            if (toRemoveSet.Contains(idx))
                presPart.Presentation.SlideIdList.RemoveChild(slideId);
        }

        presPart.Presentation.Save();
        Console.WriteLine("Removed slides: " + string.Join(", ", toRemove));
        return 0;
    }

    // ── Reorder ─────────────────────────────────────────────────────────────

    int CmdReorder(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        var output = Arg("--output", "-o", args);
        var orderStr = Arg("--order", null, args);

        if (string.IsNullOrEmpty(input)) return Fail("reorder: --input required");
        if (string.IsNullOrEmpty(output)) return Fail("reorder: --output required");
        if (string.IsNullOrEmpty(orderStr)) return Fail("reorder: --order required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        var order = orderStr.Split(',').Select(s => int.Parse(s.Trim())).ToArray();

        File.Copy(input, output, true);
        using var doc = PresentationDocument.Open(output, true);
        var presPart = doc.PresentationPart!;
        var slideIds = presPart.Presentation.SlideIdList!.Elements<SlideId>().ToList();

        if (order.Length != slideIds.Count)
            return Fail($"Order must specify all {slideIds.Count} slides");

        var expected = Enumerable.Range(1, slideIds.Count).ToArray();
        if (!order.OrderBy(x => x).SequenceEqual(expected))
            return Fail("Order must be a permutation of 1.." + slideIds.Count);

        var reordered = order.Select(i => slideIds[i - 1]).ToList();
        presPart.Presentation.SlideIdList.RemoveAllChildren<SlideId>();
        foreach (var sid in reordered)
            presPart.Presentation.SlideIdList.Append(sid);

        presPart.Presentation.Save();
        Console.WriteLine("Reordered slides: " + string.Join(", ", order));
        return 0;
    }

    // ── Notes ───────────────────────────────────────────────────────────────

    int CmdNotes(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        var output = Arg("--output", "-o", args);
        var slideNumStr = Arg("--slide", null, args);
        var content = Arg("--content", "-c", args);

        if (string.IsNullOrEmpty(input)) return Fail("notes: --input required");
        if (string.IsNullOrEmpty(output)) return Fail("notes: --output required");
        if (string.IsNullOrEmpty(slideNumStr)) return Fail("notes: --slide required");
        if (!int.TryParse(slideNumStr, out var slideNum) || slideNum < 1)
            return Fail("notes: --slide must be a positive integer");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        File.Copy(input, output, true);
        using var doc = PresentationDocument.Open(output, true);
        var presPart = doc.PresentationPart!;
        var slideIds = presPart.Presentation.SlideIdList!.Elements<SlideId>().ToList();

        if (slideNum > slideIds.Count)
            return Fail($"Slide {slideNum} does not exist (total: {slideIds.Count})");

        var slideId = slideIds[slideNum - 1];
        var slidePart = presPart.GetPartById(slideId.RelationshipId!) as SlidePart;
        if (slidePart == null) return Fail("Could not access slide");

        AddNotes(slidePart, content ?? "");
        presPart.Presentation.Save();
        Console.WriteLine($"Added notes to slide {slideNum}");
        return 0;
    }

    int CmdExtractNotes(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        var output = Arg("--output", "-o", args);

        if (string.IsNullOrEmpty(input)) return Fail("extract-notes: --input required");
        if (string.IsNullOrEmpty(output)) return Fail("extract-notes: --output required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        using var doc = PresentationDocument.Open(input, false);
        var slideIds = doc.PresentationPart!.Presentation.SlideIdList!.Elements<SlideId>().ToList();

        using var writer = new StreamWriter(output);
        for (int i = 0; i < slideIds.Count; i++)
        {
            var slidePart = doc.PresentationPart.GetPartById(slideIds[i].RelationshipId!) as SlidePart;
            if (slidePart?.NotesSlidePart == null) continue;

            var texts = slidePart.NotesSlidePart.NotesSlide.Descendants<A.Text>()
                .Select(t => t.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t));
            var noteText = string.Join(" ", texts);

            if (!string.IsNullOrWhiteSpace(noteText))
            {
                writer.WriteLine($"--- Slide {i + 1} ---");
                writer.WriteLine(noteText);
                writer.WriteLine();
            }
        }

        Console.WriteLine("Extracted notes to: " + Path.GetFullPath(output));
        return 0;
    }

    // ── Set Properties ──────────────────────────────────────────────────────

    int CmdSetProperties(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        var output = Arg("--output", "-o", args);
        var title = Arg("--title", null, args);
        var author = Arg("--author", null, args);
        var subject = Arg("--subject", null, args);
        var keywords = Arg("--keywords", null, args);

        if (string.IsNullOrEmpty(input)) return Fail("set-properties: --input required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        var outPath = output ?? input;
        if (outPath != input) File.Copy(input, outPath, true);

        using var doc = PresentationDocument.Open(outPath, true);
        var props = doc.PackageProperties;
        if (title != null) props.Title = title;
        if (author != null) props.Creator = author;
        if (subject != null) props.Subject = subject;
        if (keywords != null) props.Keywords = keywords;

        Console.WriteLine("Updated properties: " + Path.GetFullPath(outPath));
        return 0;
    }

    // ── Export ──────────────────────────────────────────────────────────────

    int CmdExport(string[] args)
    {
        var input = Arg("--input", "-i", args) ?? Positional(args);
        var output = Arg("--output", "-o", args);

        if (string.IsNullOrEmpty(input)) return Fail("export: --input required");
        if (string.IsNullOrEmpty(output)) return Fail("export: --output required");
        if (!File.Exists(input)) return Fail($"File not found: {input}");

        // Try LibreOffice
        var soffice = FindInPath("soffice") ?? FindInPath("soffice.bin");
        if (soffice != null)
        {
            var outDir = Path.GetDirectoryName(Path.GetFullPath(output))!;
            var psi = new System.Diagnostics.ProcessStartInfo(soffice)
            {
                Arguments = $"--headless --convert-to pdf --outdir \"{outDir}\" \"{Path.GetFullPath(input)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit();

            var expectedTemp = Path.Combine(outDir, Path.GetFileNameWithoutExtension(input) + ".pdf");
            if (File.Exists(expectedTemp) && expectedTemp != Path.GetFullPath(output))
                File.Move(expectedTemp, Path.GetFullPath(output), true);

            if (File.Exists(output))
            {
                Console.WriteLine("Exported to: " + Path.GetFullPath(output));
                return 0;
            }
        }

        // Try PowerPoint COM on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var type = Type.GetTypeFromProgID("PowerPoint.Application");
                if (type != null)
                {
                    dynamic app = Activator.CreateInstance(type)!;
                    app.Visible = false;
                    dynamic pres = app.Presentations.Open(Path.GetFullPath(input), WithWindow: false);
                    pres.SaveAs(Path.GetFullPath(output), 32);
                    pres.Close();
                    app.Quit();
                    Console.WriteLine("Exported to: " + Path.GetFullPath(output));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PowerPoint COM export failed: {ex.Message}");
            }
        }

        return Fail("Export failed. Install LibreOffice or ensure PowerPoint is available.");
    }

    string? FindInPath(string name)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT")?.Split(Path.PathSeparator) ?? new[] { ".exe", ".cmd", ".bat" })
            : new[] { "" };
        foreach (var dir in paths)
        {
            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(dir, name + ext);
                if (File.Exists(fullPath)) return fullPath;
            }
        }
        return null;
    }

    // ── Blank PPTX Workaround ───────────────────────────────────────────────

    void CreateBlankPptx(string path)
    {
        using var pkg = System.IO.Packaging.Package.Open(path, FileMode.Create);
        var ct = pkg.CreatePart(new Uri("/[Content_Types].xml", UriKind.Relative), "application/vnd.openxmlformats-package.core-properties+xml");
        using (var s = new StreamWriter(ct.GetStream()))
            s.Write("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/><Default Extension='xml' ContentType='application/xml'/><Override PartName='/ppt/presentation.xml' ContentType='application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml'/><Override PartName='/ppt/slideMasters/slideMaster1.xml' ContentType='application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml'/><Override PartName='/ppt/slideLayouts/slideLayout1.xml' ContentType='application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml'/></Types>");

        var rels = pkg.CreatePart(new Uri("/_rels/.rels", UriKind.Relative), "application/vnd.openxmlformats-package.relationships+xml");
        using (var s = new StreamWriter(rels.GetStream()))
            s.Write("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='ppt/presentation.xml'/></Relationships>");

        var pres = pkg.CreatePart(new Uri("/ppt/presentation.xml", UriKind.Relative), "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml");
        using (var s = new StreamWriter(pres.GetStream()))
            s.Write("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><p:presentation xmlns:p='http://schemas.openxmlformats.org/presentationml/2006/main' xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><p:sldIdLst/><p:sldSz cx='12192000' cy='6858000' type='screen16x9'/><p:notesSz cx='6858000' cy='9144000'/><p:defaultTextStyle/></p:presentation>");

        var presRels = pkg.CreatePart(new Uri("/ppt/_rels/presentation.xml.rels", UriKind.Relative), "application/vnd.openxmlformats-package.relationships+xml");
        using (var s = new StreamWriter(presRels.GetStream()))
            s.Write("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster' Target='slideMasters/slideMaster1.xml'/></Relationships>");

        var master = pkg.CreatePart(new Uri("/ppt/slideMasters/slideMaster1.xml", UriKind.Relative), "application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml");
        using (var s = new StreamWriter(master.GetStream()))
            s.Write("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><p:sldMaster xmlns:p='http://schemas.openxmlformats.org/presentationml/2006/main' xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id='1' name=''/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x='0' y='0'/><a:ext cx='0' cy='0'/><a:chOff x='0' y='0'/><a:chExt cx='0' cy='0'/></a:xfrm></p:grpSpPr></p:spTree></p:cSld><p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles></p:sldMaster>");

        var masterRels = pkg.CreatePart(new Uri("/ppt/slideMasters/_rels/slideMaster1.xml.rels", UriKind.Relative), "application/vnd.openxmlformats-package.relationships+xml");
        using (var s = new StreamWriter(masterRels.GetStream()))
            s.Write("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout' Target='../slideLayouts/slideLayout1.xml'/></Relationships>");

        var layout = pkg.CreatePart(new Uri("/ppt/slideLayouts/slideLayout1.xml", UriKind.Relative), "application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml");
        using (var s = new StreamWriter(layout.GetStream()))
            s.Write("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><p:sldLayout xmlns:p='http://schemas.openxmlformats.org/presentationml/2006/main' xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id='1' name=''/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x='0' y='0'/><a:ext cx='0' cy='0'/><a:chOff x='0' y='0'/><a:chExt cx='0' cy='0'/></a:xfrm></p:grpSpPr></p:spTree></p:cSld></p:sldLayout>");

        var layoutRels = pkg.CreatePart(new Uri("/ppt/slideLayouts/_rels/slideLayout1.xml.rels", UriKind.Relative), "application/vnd.openxmlformats-package.relationships+xml");
        using (var s = new StreamWriter(layoutRels.GetStream()))
            s.Write("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster' Target='../slideMasters/slideMaster1.xml'/></Relationships>");
    }

    // ── Line Format Parser ──────────────────────────────────────────────────

    static readonly HashSet<string> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "H1","H2","H3","H4","H5","H6","P","B","I","U","S","BI","CODE","QUOTE",
        "BULLET","NUMBER","HR","BR","TABLE","IMG","COLOR","SIZE","FONT","ALIGN","NEWSLIDE",
        "LAYOUT", "POS", "BGCOLOR", "BGIMAGE", "SHAPE", "FILL", "STROKE", "ROUND",
        "CHART", "ANIMATE", "TRANSITION", "LINK",
        "TABLE-BORDER", "TABLE-ZEBRA", "TABLE-NO-ZEBRA", "TABLE-HEADER"
    };

    string? GetLineCommand(string line)
    {
        var trimmed = line.TrimStart();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        var spaceIdx = trimmed.IndexOf(' ');
        var cmd = spaceIdx < 0 ? trimmed : trimmed[..spaceIdx];
        return KnownCommands.Contains(cmd) ? cmd.ToUpperInvariant() : null;
    }

    void ParseLineFormat(string content, PresentationPart presPart, SlideLayoutPart layoutPart)
    {
        var lines = content.Split('\n');
        SlidePart? currentSlide = null;

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

            if (cmd == "NEWSLIDE")
            {
                currentSlide = CreateSlide(presPart, layoutPart);
                continue;
            }

            if (currentSlide == null)
            {
                currentSlide = CreateSlide(presPart, layoutPart);
            }

            switch (cmd)
            {
                case "H1": AddHeading(currentSlide, 1, text); break;
                case "H2": AddHeading(currentSlide, 2, text); break;
                case "H3": AddHeading(currentSlide, 3, text); break;
                case "H4": AddHeading(currentSlide, 4, text); break;
                case "H5": AddHeading(currentSlide, 5, text); break;
                case "H6": AddHeading(currentSlide, 6, text); break;
                case "P": AddParagraphToSlide(currentSlide, text, bold: false, italic: false); break;
                case "B": AddParagraphToSlide(currentSlide, text, bold: true); break;
                case "I": AddParagraphToSlide(currentSlide, text, italic: true); break;
                case "U": AddParagraphToSlide(currentSlide, text, underline: true); break;
                case "S": AddParagraphToSlide(currentSlide, text, strike: true); break;
                case "BI": AddParagraphToSlide(currentSlide, text, bold: true, italic: true); break;
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
                        AddCodeBlock(currentSlide, codeLines);
                        idx = nextIdx - 1;
                    }
                    break;
                case "QUOTE": AddQuote(currentSlide, text); break;
                case "BULLET": AddBullet(currentSlide, text); break;
                case "NUMBER": AddNumbered(currentSlide, text); break;
                case "HR": AddHorizontalRule(currentSlide); break;
                case "BR": AddEmptyParagraph(currentSlide); break;
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
                        AddTable(currentSlide, string.Join(";", tableLines));
                        idx = nextIdx - 1;
                    }
                    break;
                case "IMG": AddImage(currentSlide, presPart, text); break;
                case "COLOR": _currentColor = text.Trim(); break;
                case "SIZE": _currentSize = text.Trim(); break;
                case "FONT": _currentFont = text.Trim(); break;
                case "ALIGN": _currentAlign = text.Trim().ToLowerInvariant(); break;
                case "LAYOUT": _currentLayout = text.Trim().ToLowerInvariant(); break;
                case "POS": ParsePos(text); break;
                case "BGCOLOR": SetBackgroundColor(currentSlide, text.Trim()); break;
                case "BGIMAGE": SetBackgroundImage(currentSlide, presPart, text.Trim()); break;
                case "SHAPE": ParseShape(currentSlide, text); break;
                case "FILL": _currentFill = text.Trim(); break;
                case "STROKE": ParseStroke(text); break;
                case "ROUND": _currentRound = text.Trim(); break;
                case "CHART": AddChart(currentSlide, presPart, text); break;
                case "ANIMATE": AddAnimation(currentSlide, text.Trim().ToLowerInvariant()); break;
                case "TRANSITION": SetTransition(currentSlide, text.Trim().ToLowerInvariant()); break;
                case "LINK": AddHyperlink(currentSlide, presPart, text); break;
                case "TABLE-BORDER": ParseTableBorder(text); break;
                case "TABLE-ZEBRA": _tableZebra = true; break;
                case "TABLE-NO-ZEBRA": _tableZebra = false; break;
                case "TABLE-HEADER": _tableHeaderColor = text.Trim(); break;
                default:
                    AddParagraphToSlide(currentSlide, line);
                    break;
            }
        }
    }

    // ── Slide & Layout ──────────────────────────────────────────────────────

    SlidePart CreateSlide(PresentationPart presPart, SlideLayoutPart layoutPart)
    {
        var slidePart = presPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));
        slidePart.AddPart(layoutPart);

        var csd = slidePart.Slide.CommonSlideData!;
        csd.Background = new Background(new BackgroundStyleReference { Index = 1001 });

        uint maxId = presPart.Presentation.SlideIdList!.Elements<SlideId>().Select(s => s.Id?.Value ?? 0u).DefaultIfEmpty(0u).Max();
        var slideId = new SlideId { Id = maxId + 256, RelationshipId = presPart.GetIdOfPart(slidePart) };
        presPart.Presentation.SlideIdList.Append(slideId);

        _shapeId = 1;

        if (Layouts.TryGetValue(_currentLayout, out var layout))
        {
            foreach (var shapeDef in layout.Shapes)
            {
                CreateTextShape(slidePart, shapeDef.Name, shapeDef.X, shapeDef.Y, shapeDef.Width, shapeDef.Height);
            }
        }
        else
        {
            CreateTextShape(slidePart, "TitleShape", 500000, 300000, 11192000, 900000);
            CreateTextShape(slidePart, "ContentShape", 500000, 1500000, 11192000, 5200000);
        }

        return slidePart;
    }

    Shape CreateTextShape(SlidePart slidePart, string name, long x, long y, long w, long h)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var shape = new Shape();
        shape.NonVisualShapeProperties = new NonVisualShapeProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = name },
            new A.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties()
        );
        shape.ShapeProperties = new ShapeProperties(
            new A.Transform2D(
                new A.Offset { X = x, Y = y },
                new A.Extents { Cx = w, Cy = h }
            )
        );
        shape.TextBody = new TextBody(
            new A.BodyProperties { Wrap = A.TextWrappingValues.Square },
            new A.ListStyle(),
            new A.Paragraph()
        );
        shapeTree.Append(shape);
        return shape;
    }

    Shape GetTextShape(SlidePart slidePart, bool isHeading)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var shapes = shapeTree.Elements<Shape>().ToList();

        if (isHeading)
        {
            var title = shapes.FirstOrDefault(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value == "TitleShape");
            if (title != null) return title;
        }

        var content = shapes.FirstOrDefault(s =>
            s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value != "TitleShape"
            && s.TextBody != null);
        if (content != null) return content;

        return GetOrCreateContentShape(slidePart);
    }

    Shape GetOrCreateContentShape(SlidePart slidePart)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var existing = shapeTree.Elements<Shape>().FirstOrDefault(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value == "ContentShape");
        if (existing != null) return existing;

        var (x, y, w, h) = GetDefaultOrPos(500000, 1500000, 11192000, 5200000);
        return CreateTextShape(slidePart, "ContentShape", x, y, w, h);
    }

    Shape GetOrCreateTitleShape(SlidePart slidePart)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var existing = shapeTree.Elements<Shape>().FirstOrDefault(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value == "TitleShape");
        if (existing != null) return existing;

        var (x, y, w, h) = GetDefaultOrPos(500000, 300000, 11192000, 900000);
        return CreateTextShape(slidePart, "TitleShape", x, y, w, h);
    }

    (long x, long y, long w, long h) GetDefaultOrPos(long dx, long dy, long dw, long dh)
    {
        var x = _posX >= 0 ? _posX : dx;
        var y = _posY >= 0 ? _posY : dy;
        var w = _posW >= 0 ? _posW : dw;
        var h = _posH >= 0 ? _posH : dh;
        _posX = _posY = _posW = _posH = -1;
        return (x, y, w, h);
    }

    // ── Positioning ─────────────────────────────────────────────────────────

    void ParsePos(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length >= 1) _posX = ParseLength(parts[0], SlideWidth);
        if (parts.Length >= 2) _posY = ParseLength(parts[1], SlideHeight);
        if (parts.Length >= 3) _posW = ParseLength(parts[2], SlideWidth);
        if (parts.Length >= 4) _posH = ParseLength(parts[3], SlideHeight);
    }

    long ParseLength(string value, long total)
    {
        value = value.Trim();
        if (value.EndsWith('%'))
        {
            if (float.TryParse(value[..^1], out var pct))
                return (long)(pct / 100f * total);
        }
        else if (value.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
        {
            if (float.TryParse(value[..^2], out var mm))
                return (long)(mm * 36000);
        }
        else if (long.TryParse(value, out var emu))
        {
            return emu;
        }
        return 0;
    }

    // ── Background ──────────────────────────────────────────────────────────

    void SetBackgroundColor(SlidePart slidePart, string color)
    {
        var csd = slidePart.Slide!.CommonSlideData!;
        csd.Background = new Background(
            new BackgroundProperties(
                new A.SolidFill(new A.RgbColorModelHex { Val = color.TrimStart('#') })
            )
        );
    }

    void SetBackgroundImage(SlidePart slidePart, PresentationPart presPart, string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Warning: background image not found: {imagePath}");
            return;
        }

        var ext = Path.GetExtension(imagePath).ToLowerInvariant() switch
        {
            ".png" => ImagePartType.Png,
            ".jpg" or ".jpeg" => ImagePartType.Jpeg,
            ".gif" => ImagePartType.Gif,
            ".bmp" => ImagePartType.Bmp,
            _ => ImagePartType.Png
        };
        var imagePart = slidePart.AddImagePart(ext);
        using (var fs = new FileStream(imagePath, FileMode.Open))
            imagePart.FeedData(fs);

        var csd = slidePart.Slide!.CommonSlideData!;
        csd.Background = new Background(
            new BackgroundProperties(
                new A.BlipFill(
                    new A.Blip { Embed = slidePart.GetIdOfPart(imagePart) },
                    new A.Stretch(new A.FillRectangle())
                )
            )
        );
    }

    // ── Drawing Shapes ──────────────────────────────────────────────────────

    void ParseShape(SlidePart slidePart, string text)
    {
        var parts = text.Split(' ', StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return;

        var shapeType = parts[0].ToLowerInvariant();

        if (shapeType == "rect" && parts.Length >= 2)
        {
            var coords = parts[1].Split(',', StringSplitOptions.TrimEntries);
            if (coords.Length >= 4)
            {
                var x = ParseLength(coords[0], SlideWidth);
                var y = ParseLength(coords[1], SlideHeight);
                var w = ParseLength(coords[2], SlideWidth);
                var h = ParseLength(coords[3], SlideHeight);
                AddRectangle(slidePart, x, y, w, h);
            }
        }
        else if (shapeType == "ellipse" && parts.Length >= 2)
        {
            var coords = parts[1].Split(',', StringSplitOptions.TrimEntries);
            if (coords.Length >= 4)
            {
                var x = ParseLength(coords[0], SlideWidth);
                var y = ParseLength(coords[1], SlideHeight);
                var w = ParseLength(coords[2], SlideWidth);
                var h = ParseLength(coords[3], SlideHeight);
                AddEllipse(slidePart, x, y, w, h);
            }
        }
        else if (shapeType == "line" && parts.Length >= 2)
        {
            var coords = parts[1].Split(',', StringSplitOptions.TrimEntries);
            if (coords.Length >= 4)
            {
                var x1 = ParseLength(coords[0], SlideWidth);
                var y1 = ParseLength(coords[1], SlideHeight);
                var x2 = ParseLength(coords[2], SlideWidth);
                var y2 = ParseLength(coords[3], SlideHeight);
                AddLine(slidePart, x1, y1, x2, y2);
            }
        }
        else if (shapeType == "arrow" && parts.Length >= 2)
        {
            var coords = parts[1].Split(',', StringSplitOptions.TrimEntries);
            if (coords.Length >= 4)
            {
                var x1 = ParseLength(coords[0], SlideWidth);
                var y1 = ParseLength(coords[1], SlideHeight);
                var x2 = ParseLength(coords[2], SlideWidth);
                var y2 = ParseLength(coords[3], SlideHeight);
                AddArrow(slidePart, x1, y1, x2, y2);
            }
        }

        _posX = _posY = _posW = _posH = -1;
        _currentFill = "";
        _currentStroke = "";
        _currentStrokeWidth = "";
        _currentRound = "";
    }

    void ParseStroke(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length >= 1) _currentStroke = parts[0].Trim();
        if (parts.Length >= 2) _currentStrokeWidth = parts[1].Trim();
    }

    void ParseTableBorder(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length >= 1) _tableBorderColor = parts[0].Trim();
        if (parts.Length >= 2) _tableBorderWidth = parts[1].Trim();
    }

    void AddRectangle(SlidePart slidePart, long x, long y, long w, long h)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var shape = new Shape();
        shape.NonVisualShapeProperties = new NonVisualShapeProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = "Rectangle" + _shapeId },
            new A.NonVisualShapeDrawingProperties(new A.ShapeLocks()),
            new ApplicationNonVisualDrawingProperties()
        );

        var spPr = new ShapeProperties();
        spPr.Transform2D = new A.Transform2D(
            new A.Offset { X = x, Y = y },
            new A.Extents { Cx = w, Cy = h }
        );

        bool isRounded = !string.IsNullOrEmpty(_currentRound);
        spPr.Append(new A.PresetGeometry(new A.AdjustValueList())
        {
            Preset = isRounded ? A.ShapeTypeValues.Round2SameRectangle : A.ShapeTypeValues.Rectangle
        });

        if (!string.IsNullOrEmpty(_currentFill))
        {
            spPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentFill.TrimStart('#') }));
        }

        if (!string.IsNullOrEmpty(_currentStroke))
        {
            var outline = new A.Outline();
            outline.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentStroke.TrimStart('#') }));
            if (float.TryParse(_currentStrokeWidth, out var sw))
                outline.Width = (int)(sw * 12700);
            spPr.Append(outline);
        }

        shape.ShapeProperties = spPr;
        shapeTree.Append(shape);
    }

    void AddEllipse(SlidePart slidePart, long x, long y, long w, long h)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var shape = new Shape();
        shape.NonVisualShapeProperties = new NonVisualShapeProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = "Ellipse" + _shapeId },
            new A.NonVisualShapeDrawingProperties(new A.ShapeLocks()),
            new ApplicationNonVisualDrawingProperties()
        );

        var spPr = new ShapeProperties();
        spPr.Transform2D = new A.Transform2D(
            new A.Offset { X = x, Y = y },
            new A.Extents { Cx = w, Cy = h }
        );
        spPr.Append(new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Ellipse });

        if (!string.IsNullOrEmpty(_currentFill))
        {
            spPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentFill.TrimStart('#') }));
        }

        if (!string.IsNullOrEmpty(_currentStroke))
        {
            var outline = new A.Outline();
            outline.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentStroke.TrimStart('#') }));
            if (float.TryParse(_currentStrokeWidth, out var sw))
                outline.Width = (int)(sw * 12700);
            spPr.Append(outline);
        }

        shape.ShapeProperties = spPr;
        shapeTree.Append(shape);
    }

    void AddLine(SlidePart slidePart, long x1, long y1, long x2, long y2)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var shape = new Shape();
        shape.NonVisualShapeProperties = new NonVisualShapeProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = "Line" + _shapeId },
            new A.NonVisualShapeDrawingProperties(new A.ShapeLocks()),
            new ApplicationNonVisualDrawingProperties()
        );

        var spPr = new ShapeProperties();
        spPr.Transform2D = new A.Transform2D(
            new A.Offset { X = Math.Min(x1, x2), Y = Math.Min(y1, y2) },
            new A.Extents { Cx = Math.Abs(x2 - x1), Cy = Math.Abs(y2 - y1) }
        );
        spPr.Append(new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Line });

        var outline = new A.Outline();
        if (!string.IsNullOrEmpty(_currentStroke))
            outline.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentStroke.TrimStart('#') }));
        else
            outline.Append(new A.SolidFill(new A.RgbColorModelHex { Val = "000000" }));
        if (float.TryParse(_currentStrokeWidth, out var sw))
            outline.Width = (int)(sw * 12700);
        else
            outline.Width = 12700;
        spPr.Append(outline);

        shape.ShapeProperties = spPr;
        shapeTree.Append(shape);
    }

    void AddArrow(SlidePart slidePart, long x1, long y1, long x2, long y2)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var shape = new Shape();
        shape.NonVisualShapeProperties = new NonVisualShapeProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = "Arrow" + _shapeId },
            new A.NonVisualShapeDrawingProperties(new A.ShapeLocks()),
            new ApplicationNonVisualDrawingProperties()
        );

        var spPr = new ShapeProperties();
        spPr.Transform2D = new A.Transform2D(
            new A.Offset { X = Math.Min(x1, x2), Y = Math.Min(y1, y2) },
            new A.Extents { Cx = Math.Abs(x2 - x1), Cy = Math.Abs(y2 - y1) }
        );
        spPr.Append(new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Line });

        var outline = new A.Outline();
        if (!string.IsNullOrEmpty(_currentStroke))
            outline.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentStroke.TrimStart('#') }));
        else
            outline.Append(new A.SolidFill(new A.RgbColorModelHex { Val = "000000" }));
        if (float.TryParse(_currentStrokeWidth, out var sw))
            outline.Width = (int)(sw * 12700);
        else
            outline.Width = 12700;
        outline.Append(new A.HeadEnd
        {
            Type = A.LineEndValues.Arrow,
            Width = A.LineEndWidthValues.Medium,
            Length = A.LineEndLengthValues.Medium
        });
        spPr.Append(outline);

        shape.ShapeProperties = spPr;
        shapeTree.Append(shape);
    }

    // ── Text Helpers ────────────────────────────────────────────────────────

    void AddTextToSlide(SlidePart slidePart, string text, int fontSizeHundredths)
    {
        var shape = GetOrCreateContentShape(slidePart);
        var para = new A.Paragraph();
        var run = new A.Run(new A.Text(text));
        var rPr = new A.RunProperties { Language = "en-US", FontSize = fontSizeHundredths * 100 };
        run.PrependChild(rPr);
        para.Append(run);
        ApplyParaAlign(para);
        shape.TextBody!.Append(para);
    }

    void AddHeading(SlidePart slidePart, int level, string text)
    {
        var size = level switch { 1 => _theme.Heading1Size, 2 => _theme.Heading2Size, 3 => _theme.Heading3Size, 4 => _theme.Heading4Size, 5 => _theme.Heading5Size, _ => _theme.Heading6Size };
        var shape = GetTextShape(slidePart, isHeading: level <= 2);
        var para = new A.Paragraph();
        var run = new A.Run(new A.Text(text));
        var rPr = new A.RunProperties { Language = "en-US", FontSize = size * 100, Bold = _theme.HeadingBold };
        if (!string.IsNullOrEmpty(_currentColor))
            rPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentColor.TrimStart('#') }));
        else
            rPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _theme.HeadingColor }));
        run.PrependChild(rPr);
        para.Append(run);
        ApplyParaAlign(para);
        shape.TextBody!.Append(para);
    }

    void AddParagraphToSlide(SlidePart slidePart, string text, bool bold = false, bool italic = false, bool underline = false, bool strike = false)
    {
        var shape = GetOrCreateContentShape(slidePart);
        var para = new A.Paragraph();
        var run = new A.Run(new A.Text(text));
        var rPr = new A.RunProperties { Language = "en-US", FontSize = _theme.BodySize * 100 };
        if (bold) rPr.Bold = true;
        if (italic) rPr.Italic = true;
        if (underline) rPr.Underline = A.TextUnderlineValues.Single;
        if (strike) rPr.Strike = A.TextStrikeValues.SingleStrike;
        if (!string.IsNullOrEmpty(_currentColor)) rPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentColor.TrimStart('#') }));
        if (!string.IsNullOrEmpty(_currentSize) && int.TryParse(_currentSize, out var sz)) rPr.FontSize = sz * 100;
        if (!string.IsNullOrEmpty(_currentFont)) rPr.Append(new A.LatinFont { Typeface = _currentFont });
        run.PrependChild(rPr);
        para.Append(run);
        ApplyParaAlign(para);
        shape.TextBody!.Append(para);
    }

    void AddCodeBlock(SlidePart slidePart, List<string> lines)
    {
        var shape = GetOrCreateContentShape(slidePart);
        var para = new A.Paragraph();
        para.ParagraphProperties = new A.ParagraphProperties();

        for (int i = 0; i < lines.Count; i++)
        {
            var run = new A.Run(new A.Text(lines[i]));
            var rPr = new A.RunProperties
            {
                Language = "en-US",
                FontSize = _theme.CodeSize * 100,
            };
            rPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = "333333" }));
            run.PrependChild(rPr);
            para.Append(run);
            if (i < lines.Count - 1)
            {
                para.Append(new A.Run(new A.Break()));
            }
        }
        shape.TextBody!.Append(para);
    }

    void AddQuote(SlidePart slidePart, string text)
    {
        var shape = GetOrCreateContentShape(slidePart);
        var para = new A.Paragraph();
        var run = new A.Run(new A.Text(text));
        var rPr = new A.RunProperties
        {
            Language = "en-US",
            FontSize = _theme.BodySize * 100,
            Italic = true
        };
        rPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _theme.QuoteTextColor }));
        run.PrependChild(rPr);
        para.Append(run);
        ApplyParaAlign(para);
        shape.TextBody!.Append(para);
    }

    void AddBullet(SlidePart slidePart, string text)
    {
        var shape = GetOrCreateContentShape(slidePart);
        var para = new A.Paragraph();
        para.ParagraphProperties = new A.ParagraphProperties();
        para.ParagraphProperties.Level = 0;
        var run = new A.Run(new A.Text(text));
        var rPr = new A.RunProperties { Language = "en-US", FontSize = _theme.BulletSize * 100 };
        if (!string.IsNullOrEmpty(_currentColor)) rPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentColor.TrimStart('#') }));
        run.PrependChild(rPr);
        para.Append(run);
        ApplyParaAlign(para);
        shape.TextBody!.Append(para);
    }

    void AddNumbered(SlidePart slidePart, string text)
    {
        var shape = GetOrCreateContentShape(slidePart);
        var para = new A.Paragraph();
        para.ParagraphProperties = new A.ParagraphProperties();
        para.ParagraphProperties.Level = 0;
        var run = new A.Run(new A.Text(text));
        var rPr = new A.RunProperties { Language = "en-US", FontSize = _theme.BulletSize * 100 };
        if (!string.IsNullOrEmpty(_currentColor)) rPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _currentColor.TrimStart('#') }));
        run.PrependChild(rPr);
        para.Append(run);
        ApplyParaAlign(para);
        shape.TextBody!.Append(para);
    }

    void AddHorizontalRule(SlidePart slidePart)
    {
        var shape = GetOrCreateContentShape(slidePart);
        var para = new A.Paragraph();
        var run = new A.Run(new A.Text("______________________________"));
        var rPr = new A.RunProperties { Language = "en-US", FontSize = 800 };
        run.PrependChild(rPr);
        para.Append(run);
        shape.TextBody!.Append(para);
    }

    void AddEmptyParagraph(SlidePart slidePart)
    {
        var shape = GetOrCreateContentShape(slidePart);
        shape.TextBody!.Append(new A.Paragraph());
    }

    void AddTable(SlidePart slidePart, string text)
    {
        var rows = text.Split(';');
        if (rows.Length == 0) return;
        var cols = rows[0].Split(',').Length;

        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;

        var graphicFrame = new GraphicFrame();
        graphicFrame.NonVisualGraphicFrameProperties = new NonVisualGraphicFrameProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = "TableFrame" },
            new A.NonVisualGraphicFrameDrawingProperties(),
            new ApplicationNonVisualDrawingProperties()
        );
        graphicFrame.Transform = new Transform(
            new A.Offset { X = 500000, Y = 3000000 },
            new A.Extents { Cx = 11192000, Cy = 2000000 }
        );

        var table = new A.Table(
            new A.TableProperties { FirstRow = true },
            new A.TableGrid(Enumerable.Range(0, cols).Select(_ => new A.GridColumn { Width = 2000000 }).ToArray())
        );

        for (int r = 0; r < rows.Length; r++)
        {
            var cells = rows[r].Split(',');
            var row = new A.TableRow { Height = 400000 };
            for (int c = 0; c < cols; c++)
            {
                var cellText = c < cells.Length ? cells[c].Trim() : "";
                var cell = new A.TableCell(
                    new A.TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(new A.Run(new A.Text(cellText)))
                    ),
                    new A.TableCellProperties()
                );
                if (r == 0)
                {
                    var headerColor = !string.IsNullOrEmpty(_tableHeaderColor) ? _tableHeaderColor.TrimStart('#') : _theme.TableHeaderFill;
                    cell.TableCellProperties!.Append(new A.SolidFill(new A.RgbColorModelHex { Val = headerColor }));
                }
                else if (_tableZebra && r % 2 == 0)
                {
                    cell.TableCellProperties!.Append(new A.SolidFill(new A.RgbColorModelHex { Val = "F2F2F2" }));
                }

                // Apply cell borders if specified
                if (!string.IsNullOrEmpty(_tableBorderColor))
                {
                    var borderColor = _tableBorderColor.TrimStart('#');
                    var borderWidth = int.TryParse(_tableBorderWidth, out var bw) ? bw * 12700 : 12700;
                    var cellBorders = new A.TableCellBorders();
                    cellBorders.LeftBorder = new A.LeftBorder(new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = borderColor })) { Width = borderWidth });
                    cellBorders.RightBorder = new A.RightBorder(new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = borderColor })) { Width = borderWidth });
                    cellBorders.TopBorder = new A.TopBorder(new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = borderColor })) { Width = borderWidth });
                    cellBorders.BottomBorder = new A.BottomBorder(new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = borderColor })) { Width = borderWidth });
                    cellBorders.InsideHorizontalBorder = new A.InsideHorizontalBorder(new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = borderColor })) { Width = borderWidth });
                    cellBorders.InsideVerticalBorder = new A.InsideVerticalBorder(new A.Outline(new A.SolidFill(new A.RgbColorModelHex { Val = borderColor })) { Width = borderWidth });
                    cell.TableCellProperties!.Append(cellBorders);
                }

                row.Append(cell);
            }
            table.Append(row);
        }

        var gd = new A.GraphicData();
        gd.Uri = "http://schemas.openxml.org/drawingml/2006/table";
        gd.Append(table);
        graphicFrame.Graphic = new A.Graphic(gd);
        shapeTree.Append(graphicFrame);

        // Reset table state
        _tableBorderColor = "";
        _tableBorderWidth = "";
        _tableZebra = false;
        _tableHeaderColor = "";
    }

    void AddImage(SlidePart slidePart, PresentationPart presPart, string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        var imgPath = parts[0];
        if (!File.Exists(imgPath))
        {
            Console.WriteLine($"Warning: image not found: {imgPath}");
            return;
        }

        long cx = 8000000, cy = 4500000;
        if (parts.Length > 1)
        {
            if (parts[1].EndsWith('%'))
            {
                if (float.TryParse(parts[1][..^1], out var pct))
                    cx = (long)(pct / 100f * SlideWidth);
            }
            else if (parts[1].EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(parts[1][..^2], out var mm))
                    cx = (long)(mm * 36000);
            }
            else if (float.TryParse(parts[1], out var mmRaw))
            {
                cx = (long)(mmRaw * 36000);
            }
        }
        if (parts.Length > 2)
        {
            if (parts[2].EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(parts[2][..^2], out var mm))
                    cy = (long)(mm * 36000);
            }
            else if (float.TryParse(parts[2], out var hmm))
            {
                cy = (long)(hmm * 36000);
            }
        }

        try
        {
            var ext = Path.GetExtension(imgPath).ToLowerInvariant() switch
            {
                ".png" => ImagePartType.Png,
                ".jpg" or ".jpeg" => ImagePartType.Jpeg,
                ".gif" => ImagePartType.Gif,
                ".bmp" => ImagePartType.Bmp,
                _ => ImagePartType.Png
            };
            var imagePart = slidePart.AddImagePart(ext);
            using (var fs = new FileStream(imgPath, FileMode.Open))
                imagePart.FeedData(fs);

            var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
            var picture = new Picture();
            picture.NonVisualPictureProperties = new NonVisualPictureProperties(
                new A.NonVisualDrawingProperties { Id = _shapeId++, Name = Path.GetFileName(imgPath) },
                new A.NonVisualPictureDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()
            );
            picture.BlipFill = new BlipFill(new A.Blip { Embed = slidePart.GetIdOfPart(imagePart) }, new A.Stretch(new A.FillRectangle()));

            long imgX = _posX >= 0 ? _posX : 500000;
            long imgY = _posY >= 0 ? _posY : 3000000;
            _posX = _posY = _posW = _posH = -1;

            picture.ShapeProperties = new ShapeProperties(new A.Transform2D(
                new A.Offset { X = imgX, Y = imgY },
                new A.Extents { Cx = cx, Cy = cy }
            ));
            shapeTree.Append(picture);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: could not add image {imgPath}: {ex.Message}");
        }
    }

    void AddHyperlink(SlidePart slidePart, PresentationPart presPart, string text)
    {
        var spaceIdx = text.IndexOf(' ');
        if (spaceIdx < 0) return;
        var url = text[..spaceIdx].Trim();
        var linkText = text[(spaceIdx + 1)..].Trim();
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(linkText)) return;

        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            var rel = slidePart.AddHyperlinkRelationship(uri, true);

            var shape = GetOrCreateContentShape(slidePart);
            var para = new A.Paragraph();
            var run = new A.Run(new A.Text(linkText));
            var rPr = new A.RunProperties
            {
                Language = "en-US",
                FontSize = _theme.BodySize * 100,
                Underline = A.TextUnderlineValues.Single
            };
            rPr.Append(new A.SolidFill(new A.RgbColorModelHex { Val = "0563C1" }));
            rPr.Append(new A.HyperlinkOnClick { Id = rel.Id });
            run.PrependChild(rPr);
            para.Append(run);
            ApplyParaAlign(para);
            shape.TextBody!.Append(para);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: could not add hyperlink: {ex.Message}");
        }
    }

    void ApplyParaAlign(A.Paragraph para)
    {
        if (string.IsNullOrEmpty(_currentAlign)) return;
        para.ParagraphProperties ??= new A.ParagraphProperties();
        para.ParagraphProperties.Alignment = _currentAlign switch
        {
            "center" => A.TextAlignmentTypeValues.Center,
            "right" => A.TextAlignmentTypeValues.Right,
            "justify" => A.TextAlignmentTypeValues.Justified,
            _ => A.TextAlignmentTypeValues.Left
        };
    }

    // ── Notes ───────────────────────────────────────────────────────────────

    void AddNotes(SlidePart slidePart, string text)
    {
        if (slidePart.NotesSlidePart == null)
        {
            var notesPart = slidePart.AddNewPart<NotesSlidePart>();
            notesPart.NotesSlide = new NotesSlide(
                new CommonSlideData(
                    new ShapeTree(
                        new P.Shape(
                            new NonVisualShapeProperties(
                                new A.NonVisualDrawingProperties { Id = 2, Name = "Notes Placeholder 1" },
                                new A.NonVisualShapeDrawingProperties(),
                                new ApplicationNonVisualDrawingProperties(new PlaceholderShape { Type = PlaceholderValues.Body })
                            ),
                            new ShapeProperties(),
                            new TextBody(
                                new A.BodyProperties(),
                                new A.ListStyle(),
                                new A.Paragraph(new A.Run(new A.Text(text)))
                            )
                        )
                    )
                )
            );
        }
        else
        {
            var shapeTree = slidePart.NotesSlidePart.NotesSlide.CommonSlideData.ShapeTree;
            var placeholder = shapeTree.Elements<P.Shape>().FirstOrDefault();
            if (placeholder != null)
            {
                placeholder.TextBody = new TextBody(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(new A.Run(new A.Text(text)))
                );
            }
        }
    }

    // ── Range Parser ────────────────────────────────────────────────────────

    int[] ParseRange(string range, int max)
    {
        var result = new List<int>();
        var parts = range.Split(',', StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var bounds = part.Split('-', StringSplitOptions.TrimEntries);
                if (bounds.Length == 2
                    && int.TryParse(bounds[0], out var start)
                    && int.TryParse(bounds[1], out var end))
                {
                    for (int i = Math.Max(1, start); i <= Math.Min(max, end); i++)
                        result.Add(i);
                }
            }
            else if (int.TryParse(part, out var n))
            {
                if (n >= 1 && n <= max)
                    result.Add(n);
            }
        }

        return result.Distinct().OrderBy(x => x).ToArray();
    }

    // ── Charts ──────────────────────────────────────────────────────────────

    void AddChart(SlidePart slidePart, PresentationPart presPart, string text)
    {
        var parts = text.Split(' ', StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return;

        var chartType = parts[0].ToLowerInvariant();
        var dataStr = string.Join(" ", parts.Skip(1));
        var dataParts = dataStr.Split(';');
        if (dataParts.Length < 3) return;

        var title = dataParts[0].Trim();
        var categories = dataParts[1].Split(',', StringSplitOptions.TrimEntries);

        // Parse series: each part after categories is either "SeriesName:Val1,Val2" or "Val1,Val2" (backward compat)
        var seriesList = new List<(string Name, double[] Values)>();
        for (int i = 2; i < dataParts.Length; i++)
        {
            var part = dataParts[i].Trim();
            var seriesName = $"Series {i - 1}";
            var valueStr = part;
            if (part.Contains(':'))
            {
                var nameValue = part.Split(':', 2);
                seriesName = nameValue[0].Trim();
                valueStr = nameValue[1].Trim();
            }
            var values = valueStr.Split(',', StringSplitOptions.TrimEntries)
                .Select(v => double.TryParse(v, out var d) ? d : 0).ToArray();
            seriesList.Add((seriesName, values));
        }

        var chartPart = slidePart.AddNewPart<ChartPart>();
        C.Chart chart;

        switch (chartType)
        {
            case "pie": chart = BuildPieChart(title, categories, seriesList); break;
            case "line": chart = BuildLineChart(title, categories, seriesList); break;
            case "area": chart = BuildAreaChart(title, categories, seriesList); break;
            case "bar": chart = BuildBarChart(title, categories, seriesList, horizontal: true); break;
            case "column":
            default: chart = BuildBarChart(title, categories, seriesList, horizontal: false); break;
        }

        chartPart.ChartSpace = new C.ChartSpace(chart);

        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var graphicFrame = new GraphicFrame();
        graphicFrame.NonVisualGraphicFrameProperties = new NonVisualGraphicFrameProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = "ChartFrame" },
            new A.NonVisualGraphicFrameDrawingProperties(),
            new ApplicationNonVisualDrawingProperties()
        );

        long chartX = _posX >= 0 ? _posX : 500000;
        long chartY = _posY >= 0 ? _posY : 2000000;
        long chartW = _posW >= 0 ? _posW : 11192000;
        long chartH = _posH >= 0 ? _posH : 4500000;
        _posX = _posY = _posW = _posH = -1;

        graphicFrame.Transform = new Transform(
            new A.Offset { X = chartX, Y = chartY },
            new A.Extents { Cx = chartW, Cy = chartH }
        );

        var gd = new A.GraphicData();
        gd.Uri = "http://schemas.openxml.org/drawingml/2006/chart";
        gd.Append(new C.ChartReference { Id = slidePart.GetIdOfPart(chartPart) });
        graphicFrame.Graphic = new A.Graphic(gd);
        shapeTree.Append(graphicFrame);
    }

    C.Chart BuildBarChart(string title, string[] categories, List<(string Name, double[] Values)> seriesList, bool horizontal)
    {
        var barChart = new C.BarChart(
            new C.BarDirection { Val = horizontal ? C.BarDirectionValues.Bar : C.BarDirectionValues.Column },
            new C.BarGrouping { Val = C.BarGroupingValues.Clustered }
        );
        for (uint i = 0; i < seriesList.Count; i++)
            barChart.Append(CreateBarSeries(seriesList[(int)i].Name, categories, seriesList[(int)i].Values, i));
        barChart.Append(new C.AxisId { Val = 1 });
        barChart.Append(new C.AxisId { Val = 2 });
        return new C.Chart(
            new C.PlotArea(barChart, CreateCategoryAxis(1, 2), CreateValueAxis(2, 1)),
            new C.Legend(new C.LegendPosition { Val = C.LegendPositionValues.Bottom }),
            new C.EditingLanguage { Val = "en-US" }
        );
    }

    C.Chart BuildPieChart(string title, string[] categories, List<(string Name, double[] Values)> seriesList)
    {
        var pieChart = new C.PieChart();
        for (uint i = 0; i < seriesList.Count; i++)
            pieChart.Append(CreatePieSeries(seriesList[(int)i].Name, categories, seriesList[(int)i].Values, i));
        return new C.Chart(
            new C.PlotArea(pieChart),
            new C.Legend(new C.LegendPosition { Val = C.LegendPositionValues.Bottom }),
            new C.EditingLanguage { Val = "en-US" }
        );
    }

    C.Chart BuildLineChart(string title, string[] categories, List<(string Name, double[] Values)> seriesList)
    {
        var lineChart = new C.LineChart(
            new C.Grouping { Val = C.GroupingValues.Standard }
        );
        for (uint i = 0; i < seriesList.Count; i++)
            lineChart.Append(CreateLineSeries(seriesList[(int)i].Name, categories, seriesList[(int)i].Values, i));
        lineChart.Append(new C.AxisId { Val = 1 });
        lineChart.Append(new C.AxisId { Val = 2 });
        return new C.Chart(
            new C.PlotArea(lineChart, CreateCategoryAxis(1, 2), CreateValueAxis(2, 1)),
            new C.Legend(new C.LegendPosition { Val = C.LegendPositionValues.Bottom }),
            new C.EditingLanguage { Val = "en-US" }
        );
    }

    C.Chart BuildAreaChart(string title, string[] categories, List<(string Name, double[] Values)> seriesList)
    {
        var areaChart = new C.AreaChart(
            new C.Grouping { Val = C.GroupingValues.Standard }
        );
        for (uint i = 0; i < seriesList.Count; i++)
            areaChart.Append(CreateAreaSeries(seriesList[(int)i].Name, categories, seriesList[(int)i].Values, i));
        areaChart.Append(new C.AxisId { Val = 1 });
        areaChart.Append(new C.AxisId { Val = 2 });
        return new C.Chart(
            new C.PlotArea(areaChart, CreateCategoryAxis(1, 2), CreateValueAxis(2, 1)),
            new C.Legend(new C.LegendPosition { Val = C.LegendPositionValues.Bottom }),
            new C.EditingLanguage { Val = "en-US" }
        );
    }

    C.BarChartSeries CreateBarSeries(string title, string[] categories, double[] values, uint index)
    {
        var series = new C.BarChartSeries(
            new C.Index { Val = index },
            new C.Order { Val = index },
            new C.SeriesText(new C.NumericValue { Text = title })
        );
        var stringLit = new C.StringLiteral(new C.PointCount { Val = (uint)categories.Length });
        for (int i = 0; i < categories.Length; i++)
            stringLit.Append(new C.StringPoint(new C.NumericValue { Text = categories[i] }) { Index = (uint)i });
        series.Append(new C.CategoryAxisData(stringLit));
        var numLit = new C.NumberLiteral(new C.FormatCode("General"), new C.PointCount { Val = (uint)values.Length });
        for (int i = 0; i < values.Length; i++)
            numLit.Append(new C.NumericPoint(new C.NumericValue { Text = values[i].ToString() }) { Index = (uint)i });
        series.Append(new C.Values(numLit));
        return series;
    }

    C.PieChartSeries CreatePieSeries(string title, string[] categories, double[] values, uint index)
    {
        var series = new C.PieChartSeries(
            new C.Index { Val = index },
            new C.Order { Val = index },
            new C.SeriesText(new C.NumericValue { Text = title })
        );
        var stringLit = new C.StringLiteral(new C.PointCount { Val = (uint)categories.Length });
        for (int i = 0; i < categories.Length; i++)
            stringLit.Append(new C.StringPoint(new C.NumericValue { Text = categories[i] }) { Index = (uint)i });
        series.Append(new C.CategoryAxisData(stringLit));
        var numLit = new C.NumberLiteral(new C.FormatCode("General"), new C.PointCount { Val = (uint)values.Length });
        for (int i = 0; i < values.Length; i++)
            numLit.Append(new C.NumericPoint(new C.NumericValue { Text = values[i].ToString() }) { Index = (uint)i });
        series.Append(new C.Values(numLit));
        return series;
    }

    C.LineChartSeries CreateLineSeries(string title, string[] categories, double[] values, uint index)
    {
        var series = new C.LineChartSeries(
            new C.Index { Val = index },
            new C.Order { Val = index },
            new C.SeriesText(new C.NumericValue { Text = title })
        );
        var stringLit = new C.StringLiteral(new C.PointCount { Val = (uint)categories.Length });
        for (int i = 0; i < categories.Length; i++)
            stringLit.Append(new C.StringPoint(new C.NumericValue { Text = categories[i] }) { Index = (uint)i });
        series.Append(new C.CategoryAxisData(stringLit));
        var numLit = new C.NumberLiteral(new C.FormatCode("General"), new C.PointCount { Val = (uint)values.Length });
        for (int i = 0; i < values.Length; i++)
            numLit.Append(new C.NumericPoint(new C.NumericValue { Text = values[i].ToString() }) { Index = (uint)i });
        series.Append(new C.Values(numLit));
        return series;
    }

    C.AreaChartSeries CreateAreaSeries(string title, string[] categories, double[] values, uint index)
    {
        var series = new C.AreaChartSeries(
            new C.Index { Val = index },
            new C.Order { Val = index },
            new C.SeriesText(new C.NumericValue { Text = title })
        );
        var stringLit = new C.StringLiteral(new C.PointCount { Val = (uint)categories.Length });
        for (int i = 0; i < categories.Length; i++)
            stringLit.Append(new C.StringPoint(new C.NumericValue { Text = categories[i] }) { Index = (uint)i });
        series.Append(new C.CategoryAxisData(stringLit));
        var numLit = new C.NumberLiteral(new C.FormatCode("General"), new C.PointCount { Val = (uint)values.Length });
        for (int i = 0; i < values.Length; i++)
            numLit.Append(new C.NumericPoint(new C.NumericValue { Text = values[i].ToString() }) { Index = (uint)i });
        series.Append(new C.Values(numLit));
        return series;
    }

    C.CategoryAxis CreateCategoryAxis(uint id, uint crossId)
    {
        return new C.CategoryAxis(
            new C.AxisId { Val = id },
            new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
            new C.Delete { Val = false },
            new C.AxisPosition { Val = C.AxisPositionValues.Bottom },
            new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
            new C.CrossingAxis { Val = crossId },
            new C.Crosses { Val = C.CrossesValues.AutoZero }
        );
    }

    C.ValueAxis CreateValueAxis(uint id, uint crossId)
    {
        return new C.ValueAxis(
            new C.AxisId { Val = id },
            new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
            new C.Delete { Val = false },
            new C.AxisPosition { Val = C.AxisPositionValues.Left },
            new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
            new C.CrossingAxis { Val = crossId },
            new C.Crosses { Val = C.CrossesValues.AutoZero }
        );
    }

    // ── Animations ──────────────────────────────────────────────────────────

    void AddAnimation(SlidePart slidePart, string animType)
    {
        var slide = slidePart.Slide!;
        var shapeTree = slide.CommonSlideData!.ShapeTree!;
        var lastShape = shapeTree.Elements<Shape>().LastOrDefault();
        if (lastShape == null) return;

        var shapeId = lastShape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value ?? 1;

        if (slide.Timing == null)
            slide.Timing = new Timing();

        var timeNodeList = slide.Timing.TimeNodeList ?? new TimeNodeList();
        slide.Timing.TimeNodeList = timeNodeList;

        var rootPar = timeNodeList.Elements<ParallelTimeNode>().FirstOrDefault();
        if (rootPar == null)
        {
            rootPar = new ParallelTimeNode(
                new CommonTimeNode(
                    new ChildTimeNodeList()
                ) { Id = 1, Duration = "indefinite", Restart = TimeNodeRestartValues.Never, NodeType = TimeNodeValues.TmingRoot }
            );
            timeNodeList.Append(rootPar);
        }

        var mainSeq = rootPar.CommonTimeNode.ChildTimeNodeList!.Elements<SequenceTimeNode>().FirstOrDefault();
        if (mainSeq == null)
        {
            mainSeq = new SequenceTimeNode(
                new CommonTimeNode(
                    new ChildTimeNodeList()
                ) { Id = 2, Duration = "indefinite", NodeType = TimeNodeValues.MainSequence }
            ) { Concurrent = true, NextAction = NextActionValues.Seek };
            rootPar.CommonTimeNode.ChildTimeNodeList.Append(mainSeq);
        }

        var effectId = (uint)(100 + shapeId);
        var effectPar = new ParallelTimeNode(
            new CommonTimeNode(
                new ChildTimeNodeList()
            ) { Id = effectId, Fill = TimeNodeFillValues.Hold, NodeType = TimeNodeValues.ClickEffect }
        );

        var behavior = CreateAnimationBehavior(animType, shapeId);
        if (behavior != null)
            effectPar.CommonTimeNode.ChildTimeNodeList!.Append(behavior);

        mainSeq.CommonTimeNode.ChildTimeNodeList!.Append(effectPar);
    }

    OpenXmlElement? CreateAnimationBehavior(string animType, uint shapeId)
    {
        var target = new TargetElement(new ShapeTarget { ShapeId = shapeId.ToString() });

        // Detect auto-play prefix
        bool autoPlay = animType.StartsWith("auto-");
        if (autoPlay) animType = animType[5..];

        // Detect exit prefix
        bool isExit = animType.StartsWith("exit-");
        if (isExit) animType = animType[5..];

        StartConditionList CreateTrigger() => autoPlay
            ? new StartConditionList(new Condition { Delay = "0" })
            : new StartConditionList(new Condition { Event = TriggerEventValues.OnClick });

        // Exit animations use opposite visibility
        if (isExit)
        {
            return new SetBehavior(
                new CommonBehavior(
                    new CommonTimeNode(CreateTrigger())
                    { Id = (uint)(200 + shapeId), Duration = "500", Fill = TimeNodeFillValues.Hold },
                    target,
                    new AttributeNameList(new AttributeName("style.visibility"))
                ),
                new ToVariantValue(new StringVariantValue { Val = "hidden" })
            );
        }

        switch (animType)
        {
            case "appear":
                return new SetBehavior(
                    new CommonBehavior(
                        new CommonTimeNode(CreateTrigger())
                        { Id = (uint)(200 + shapeId), Duration = "1", Fill = TimeNodeFillValues.Hold },
                        target,
                        new AttributeNameList(new AttributeName("style.visibility"))
                    ),
                    new ToVariantValue(new StringVariantValue { Val = "visible" })
                );
            case "flyin":
                return new Animate(
                    new CommonBehavior(
                        new CommonTimeNode(CreateTrigger())
                        { Id = (uint)(200 + shapeId), Duration = "500", Fill = TimeNodeFillValues.Hold },
                        target,
                        new AttributeNameList(new AttributeName("ppt_y"))
                    ),
                    new ToVariantValue(new StringVariantValue { Val = "0.5" })
                );
            case "zoom":
                return new Animate(
                    new CommonBehavior(
                        new CommonTimeNode(CreateTrigger())
                        { Id = (uint)(200 + shapeId), Duration = "500", Fill = TimeNodeFillValues.Hold },
                        target,
                        new AttributeNameList(new AttributeName("ppt_w"))
                    ),
                    new ToVariantValue(new StringVariantValue { Val = "1.0" })
                );
            case "pulse":
            case "grow":
                return new Animate(
                    new CommonBehavior(
                        new CommonTimeNode(CreateTrigger())
                        { Id = (uint)(200 + shapeId), Duration = "300", Fill = TimeNodeFillValues.Hold },
                        target,
                        new AttributeNameList(new AttributeName("ppt_w"))
                    ),
                    new ToVariantValue(new StringVariantValue { Val = "1.2" })
                );
            case "shrink":
                return new Animate(
                    new CommonBehavior(
                        new CommonTimeNode(CreateTrigger())
                        { Id = (uint)(200 + shapeId), Duration = "300", Fill = TimeNodeFillValues.Hold },
                        target,
                        new AttributeNameList(new AttributeName("ppt_w"))
                    ),
                    new ToVariantValue(new StringVariantValue { Val = "0.8" })
                );
            case "fade":
            default:
                return new SetBehavior(
                    new CommonBehavior(
                        new CommonTimeNode(CreateTrigger())
                        { Id = (uint)(200 + shapeId), Duration = "1", Fill = TimeNodeFillValues.Hold },
                        target,
                        new AttributeNameList(new AttributeName("style.visibility"))
                    ),
                    new ToVariantValue(new StringVariantValue { Val = "visible" })
                );
        }
    }

    // ── Transitions ─────────────────────────────────────────────────────────

    void SetTransition(SlidePart slidePart, string transType)
    {
        var transition = new Transition();
        switch (transType)
        {
            case "fade": transition.Append(new FadeTransition()); break;
            case "push": transition.Append(new PushTransition()); break;
            case "wipe": transition.Append(new WipeTransition()); break;
            case "split": transition.Append(new SplitTransition()); break;
            case "uncover": transition.Append(new PullTransition()); break;
            case "cover": transition.Append(new CoverTransition()); break;
            case "random": transition.Append(new RandomTransition()); break;
            case "newsflash": transition.Append(new NewsflashTransition()); break;
            case "dissolve": transition.Append(new DissolveTransition()); break;
            case "checker": transition.Append(new CheckerTransition()); break;
            case "comb": transition.Append(new CombTransition()); break;
            case "cut": transition.Append(new CutTransition()); break;
            case "zoom": transition.Append(new ZoomTransition()); break;
            case "blinds": transition.Append(new BlindsTransition()); break;
            case "circle": transition.Append(new CircleTransition()); break;
            case "diamond": transition.Append(new DiamondTransition()); break;
            case "plus": transition.Append(new PlusTransition()); break;
            case "strips": transition.Append(new StripsTransition()); break;
            case "wedge": transition.Append(new WedgeTransition()); break;
            case "wheel": transition.Append(new WheelTransition()); break;
            default: transition.Append(new FadeTransition()); break;
        }
        slidePart.Slide!.Transition = transition;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    string? Arg(string longForm, string shortForm, string[] args)
    {
        if (string.IsNullOrEmpty(longForm) && string.IsNullOrEmpty(shortForm))
        {
            return Positional(args);
        }
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
        var optionsWithValues = new HashSet<string> { "--input", "-i", "--output", "-o", "--content", "-c", "--content-file", "-cf", "--style", "-s", "--range", "-r", "--order", "--slide", "--title", "--author", "--subject", "--keywords" };
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (optionsWithValues.Contains(arg)) { i++; continue; }
            if (!arg.StartsWith("-")) return arg;
        }
        return null;
    }

    List<string> ArgsMulti(string longForm, string shortForm, string[] args)
    {
        var result = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == longForm || (!string.IsNullOrEmpty(shortForm) && args[i] == shortForm))
            {
                i++;
                while (i < args.Length && !args[i].StartsWith("-"))
                {
                    result.Add(args[i]);
                    i++;
                }
                i--;
            }
        }
        // Also collect positional args if no -i flag found
        if (result.Count == 0)
        {
            var pos = Positional(args);
            if (pos != null) result.Add(pos);
        }
        return result;
    }

    int Fail(string msg)
    {
        Console.Error.WriteLine("ERROR: " + msg);
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: ppt <command> [options]");
        Console.Error.WriteLine("Commands: read, create, modify, merge, split, remove, reorder, notes, extract-notes, set-properties, export");
        return 1;
    }
}

// ── Data Classes ────────────────────────────────────────────────────────────

class StyleTheme
{
    public string Name { get; set; } = "default";
    public string HeadingColor { get; set; } = "2E74B5";
    public string QuoteTextColor { get; set; } = "666666";
    public string QuoteBorderColor { get; set; } = "CCCCCC";
    public string CodeBackground { get; set; } = "F5F5F5";
    public string TableHeaderFill { get; set; } = "D9E2F3";
    public string TableBorderColor { get; set; } = "999999";
    public int Heading1Size { get; set; } = 44;
    public int Heading2Size { get; set; } = 32;
    public int Heading3Size { get; set; } = 28;
    public int Heading4Size { get; set; } = 24;
    public int Heading5Size { get; set; } = 20;
    public int Heading6Size { get; set; } = 18;
    public int BodySize { get; set; } = 18;
    public int CodeSize { get; set; } = 14;
    public int BulletSize { get; set; } = 18;
    public int HeadingSpacingBefore { get; set; } = 0;
    public int HeadingSpacingAfter { get; set; } = 12;
    public int ParagraphSpacingAfter { get; set; } = 8;
    public bool HeadingBold { get; set; } = true;
    public bool TableBorders { get; set; } = true;
}

record ShapeDef(string Name, long X, long Y, long Width, long Height);
record LayoutDef(string Name, List<ShapeDef> Shapes);
