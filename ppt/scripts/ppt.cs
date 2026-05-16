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
using P = DocumentFormat.OpenXml.Presentation;

var exitCode = new PptTool().Run(args);
return exitCode;

class PptTool
{
    string _currentColor = "";
    string _currentSize = "";
    string _currentFont = "";
    string _currentAlign = "";
    StyleTheme _theme = Themes["default"];
    uint _shapeId = 1;

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

    public int Run(string[] args)
    {
        if (args.Length == 0) return Fail("No command. Try: ppt read --input file.pptx");
        var cmd = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        return cmd switch
        {
            "read" => CmdRead(rest),
            "create" => CmdCreate(rest),
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

        // OpenXml SDK v3.3.0 AddNewPart fails on newly-created PresentationDocument,
        // so create a blank pptx via System.IO.Packaging first, then open with OpenXml SDK.
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
        "BULLET","NUMBER","HR","BR","TABLE","IMG","COLOR","SIZE","FONT","ALIGN","NEWSLIDE"
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
                default:
                    AddParagraphToSlide(currentSlide, line);
                    break;
            }
        }
    }

    // ── Slide & Shape Helpers ───────────────────────────────────────────────

    SlidePart CreateSlide(PresentationPart presPart, SlideLayoutPart layoutPart)
    {
        var slidePart = presPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));
        slidePart.AddPart(layoutPart);

        // Add background
        var csd = slidePart.Slide.CommonSlideData!;
        csd.Background = new Background(new BackgroundStyleReference { Index = 1001 });

        uint maxId = presPart.Presentation.SlideIdList!.Elements<SlideId>().Select(s => s.Id?.Value ?? 0u).DefaultIfEmpty(0u).Max();
        var slideId = new SlideId { Id = maxId + 256, RelationshipId = presPart.GetIdOfPart(slidePart) };
        presPart.Presentation.SlideIdList.Append(slideId);

        _shapeId = 1;
        return slidePart;
    }

    Shape GetOrCreateContentShape(SlidePart slidePart)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var existing = shapeTree.Elements<Shape>().FirstOrDefault(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value == "ContentShape");
        if (existing != null) return existing;

        var shape = new Shape();
        shape.NonVisualShapeProperties = new NonVisualShapeProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = "ContentShape" },
            new A.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties()
        );
        shape.ShapeProperties = new ShapeProperties(
            new A.Transform2D(
                new A.Offset { X = 500000, Y = 1500000 },
                new A.Extents { Cx = 11192000, Cy = 5200000 }
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

    Shape GetOrCreateTitleShape(SlidePart slidePart)
    {
        var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
        var existing = shapeTree.Elements<Shape>().FirstOrDefault(s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value == "TitleShape");
        if (existing != null) return existing;

        var shape = new Shape();
        shape.NonVisualShapeProperties = new NonVisualShapeProperties(
            new A.NonVisualDrawingProperties { Id = _shapeId++, Name = "TitleShape" },
            new A.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties()
        );
        shape.ShapeProperties = new ShapeProperties(
            new A.Transform2D(
                new A.Offset { X = 500000, Y = 300000 },
                new A.Extents { Cx = 11192000, Cy = 900000 }
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
        var shape = level <= 2 ? GetOrCreateTitleShape(slidePart) : GetOrCreateContentShape(slidePart);
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
                // Typeface set via LatinFont below
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
                if (r == 0 && _theme.TableBorders)
                {
                    cell.TableCellProperties!.Append(new A.SolidFill(new A.RgbColorModelHex { Val = _theme.TableHeaderFill }));
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
                    cx = (long)(pct / 100f * 11192000);
            }
            else if (float.TryParse(parts[1], out var mm))
            {
                cx = (long)(mm * 360000);
            }
        }
        if (parts.Length > 2 && float.TryParse(parts[2], out var hmm))
        {
            cy = (long)(hmm * 360000);
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
            picture.ShapeProperties = new ShapeProperties(new A.Transform2D(
                new A.Offset { X = 500000, Y = 3000000 },
                new A.Extents { Cx = cx, Cy = cy }
            ));
            shapeTree.Append(picture);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: could not add image {imgPath}: {ex.Message}");
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
        var optionsWithValues = new HashSet<string> { "--input", "-i", "--output", "-o", "--content", "-c", "--content-file", "-cf", "--style", "-s" };
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (optionsWithValues.Contains(arg)) { i++; continue; }
            if (!arg.StartsWith("-")) return arg;
        }
        return null;
    }

    int Fail(string msg)
    {
        Console.Error.WriteLine("ERROR: " + msg);
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage: ppt <command> [options]");
        Console.Error.WriteLine("Commands: read, create");
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
