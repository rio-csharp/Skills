#!/usr/bin/env dotnet
#:package itext7@9.6.0
#:package itext7.bouncy-castle-adapter@9.6.0
#:package PdfPig@0.1.14
#:package SixLabors.ImageSharp@3.1.12

using System.Diagnostics;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

var skillDir = FindSkillRoot();
var scriptCs = Path.Combine(skillDir, "scripts", "pdf.cs");
var extractImagesPy = Path.Combine(skillDir, "scripts", "extract_images.py");
var testDir = Path.Combine(Path.GetTempPath(), "pdf_smoke_" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(testDir);

try
{
    var testPdf = Path.Combine(testDir, "test.pdf");
    var secondPdf = Path.Combine(testDir, "second.pdf");
    CreatePdf(testPdf, 3, "Test Page");
    CreatePdf(secondPdf, 2, "Second Page");

    var passed = 0;

    Pass("info", () =>
    {
        var infoJson = Path.Combine(testDir, "info.json");
        RunTool("info", "-i", testPdf, "-o", infoJson).RequireSuccess();
        Require(File.ReadAllText(infoJson).Contains("\"pages\": 3", StringComparison.Ordinal), "info JSON should include page count");
    }, ref passed);

    Pass("text", () =>
    {
        var textOut = Path.Combine(testDir, "text.txt");
        RunTool("text", "-i", testPdf, "-o", textOut).RequireSuccess();
        Require(File.ReadAllText(textOut).Contains("Test Page 1", StringComparison.Ordinal), "text output should include PDF text");
    }, ref passed);

    Pass("pages", () =>
    {
        var pagesOut = Path.Combine(testDir, "pages.pdf");
        RunTool("pages", "-i", testPdf, "--range", "1-2", "-o", pagesOut).RequireSuccess();
        Require(PageCount(pagesOut) == 2, "pages command should extract two pages");
    }, ref passed);

    Pass("merge", () =>
    {
        var mergeOut = Path.Combine(testDir, "merged.pdf");
        RunTool("merge", "-i", testPdf, secondPdf, "-o", mergeOut).RequireSuccess();
        Require(PageCount(mergeOut) == 5, "merge should preserve all input pages");
    }, ref passed);

    Pass("split", () =>
    {
        var splitDir = Path.Combine(testDir, "split");
        RunTool("split", "-i", testPdf, "--range", "2-3", "-o", splitDir).RequireSuccess();
        var splitPdf = Path.Combine(splitDir, "test_pages.pdf");
        Require(PageCount(splitPdf) == 2, "split should write selected pages");
    }, ref passed);

    Pass("rotate", () =>
    {
        var rotateOut = Path.Combine(testDir, "rotated.pdf");
        RunTool("rotate", "-i", testPdf, "--angle", "90", "-o", rotateOut).RequireSuccess();
        using var doc = new PdfDocument(new PdfReader(rotateOut));
        Require(doc.GetPage(1).GetRotation() == 90, "rotate should set page rotation");
    }, ref passed);

    Pass("watermark", () =>
    {
        var watermarked = Path.Combine(testDir, "watermarked.pdf");
        RunTool("watermark", "-i", testPdf, "--text", "CONFIDENTIAL", "-o", watermarked).RequireSuccess();
        Require(PageCount(watermarked) == 3, "watermark should preserve page count");
    }, ref passed);

    Pass("compress", () =>
    {
        var compressed = Path.Combine(testDir, "compressed.pdf");
        RunTool("compress", "-i", testPdf, "--level", "medium", "-o", compressed).RequireSuccess();
        Require(PageCount(compressed) == 3, "compress should preserve page count");
    }, ref passed);

    Pass("encrypt/decrypt", () =>
    {
        var encrypted = Path.Combine(testDir, "encrypted.pdf");
        var decrypted = Path.Combine(testDir, "decrypted.pdf");
        RunTool("encrypt", "-i", testPdf, "--password", "test123", "-o", encrypted).RequireSuccess();
        RunTool("decrypt", "-i", encrypted, "--password", "test123", "-o", decrypted).RequireSuccess();
        Require(PageCount(decrypted) == 3, "decrypt should restore readable PDF");
    }, ref passed);

    Pass("metadata", () =>
    {
        var metaPdf = Path.Combine(testDir, "meta.pdf");
        var metaJson = Path.Combine(testDir, "meta.json");
        RunTool("metadata", "-i", testPdf, "--title", "Smoke Title", "--author", "Codex", "-o", metaPdf).RequireSuccess();
        RunTool("metadata", "-i", metaPdf, "-o", metaJson).RequireSuccess();
        var json = File.ReadAllText(metaJson);
        Require(json.Contains("\"title\": \"Smoke Title\"", StringComparison.Ordinal), "metadata title should persist");
        Require(json.Contains("\"author\": \"Codex\"", StringComparison.Ordinal), "metadata author should persist");
    }, ref passed);

    Pass("bookmarks", () =>
    {
        RunTool("bookmarks", "-i", testPdf, "--list").RequireSuccess();
    }, ref passed);

    Pass("img2pdf", () =>
    {
        var imagePath = Path.Combine(testDir, "image.png");
        using (var image = new Image<Rgba32>(120, 80))
        {
            image.SaveAsPng(imagePath);
        }
        var imagePdf = Path.Combine(testDir, "image.pdf");
        RunTool("img2pdf", "-i", imagePath, "-o", imagePdf).RequireSuccess();
        Require(PageCount(imagePdf) == 1, "img2pdf should create one page per image");
    }, ref passed);

    Pass("weave", () =>
    {
        var weaved = Path.Combine(testDir, "weaved.pdf");
        RunTool("weave", "-i", testPdf, "--donor", secondPdf, "--mapping", "1,2", "-o", weaved).RequireSuccess();
        Require(PageCount(weaved) == 5, "weave should insert donor pages");
    }, ref passed);

    Pass("stamp", () =>
    {
        var stamped = Path.Combine(testDir, "stamped.pdf");
        RunTool("stamp", "-i", testPdf, "--text", "Page {n} of {N}", "-o", stamped).RequireSuccess();
        Require(PageCount(stamped) == 3, "stamp should preserve page count");
    }, ref passed);

    Pass("unsupported commands fail honestly", () =>
    {
        RunTool("pdf2img", "-i", testPdf, "--output", Path.Combine(testDir, "images")).RequireFailure("not supported");
        RunTool("ocr", "-i", testPdf, "--output", Path.Combine(testDir, "ocr.txt")).RequireFailure("not supported");
    }, ref passed);

    Pass("images extract", () =>
    {
        var imgDir = Path.Combine(testDir, "imgs");
        Directory.CreateDirectory(imgDir);
        RunPy("images", "-i", testPdf, "-o", imgDir).RequireSuccess();
    }, ref passed);

    Pass("render pages", () =>
    {
        var renderDir = Path.Combine(testDir, "rendered");
        Directory.CreateDirectory(renderDir);
        RunPy("render", "-i", testPdf, "-o", renderDir).RequireSuccess();
        Require(Directory.GetFiles(renderDir, "*.png").Length == 3, "render should produce 3 PNG files");
    }, ref passed);

    Pass("create plain text", () =>
    {
        var plainPdf = Path.Combine(testDir, "plain.pdf");
        RunTool("create", "--output", plainPdf, "--content", "Hello\nWorld").RequireSuccess();
        Require(File.Exists(plainPdf), "create should write a PDF file");
        Require(PageCount(plainPdf) == 1, "plain text create should produce 1 page");
    }, ref passed);

    Pass("create from lines", () =>
    {
        var styledPdf = Path.Combine(testDir, "styled.pdf");
        var lines = "H1 Title\nP Normal paragraph.\nB Bold text.\nI Italic text.\nQUOTE A quote.\nBULLET Item 1\nBULLET Item 2\nNUMBER First\nNUMBER Second\nHR\nCODE code block\nTABLE A,B;1,2";
        RunTool("create", "--output", styledPdf, "--content", lines, "--from-lines").RequireSuccess();
        Require(File.Exists(styledPdf), "line-format create should write a PDF file");
        var textOut = Path.Combine(testDir, "styled.txt");
        RunTool("text", "-i", styledPdf, "-o", textOut).RequireSuccess();
        var text = File.ReadAllText(textOut);
        Require(text.Contains("Title", StringComparison.Ordinal), "line-format should include heading text");
        Require(text.Contains("Bold text.", StringComparison.Ordinal), "line-format should include bold text");
        Require(text.Contains("Italic text.", StringComparison.Ordinal), "line-format should include italic text");
        Require(text.Contains("A quote.", StringComparison.Ordinal), "line-format should include quote");
        Require(text.Contains("Item 1", StringComparison.Ordinal), "line-format should include bullet");
        Require(text.Contains("First", StringComparison.Ordinal), "line-format should include numbered");
    }, ref passed);

    Pass("create with theme", () =>
    {
        var modernPdf = Path.Combine(testDir, "modern.pdf");
        RunTool("create", "--output", modernPdf, "--content", "H1 Modern Theme", "--from-lines", "--style", "modern").RequireSuccess();
        Require(File.Exists(modernPdf), "theme create should write a PDF file");
    }, ref passed);

    Pass("create missing output fails", () =>
    {
        RunTool("create", "--content", "test").RequireFailure("--output required");
    }, ref passed);

    Pass("invalid arguments fail clearly", () =>
    {
        RunTool("pages", "-i", testPdf, "--range", "1-a", "-o", Path.Combine(testDir, "bad-pages.pdf")).RequireFailure("invalid page range");
        RunTool("text", "-i", testPdf, "--page", "x").RequireFailure("--page must be");
        RunTool("rotate", "-i", testPdf, "--angle", "45", "-o", Path.Combine(testDir, "bad-rotate.pdf")).RequireFailure("--angle must be");
        RunTool("weave", "-i", testPdf, "--donor", secondPdf, "--mapping", "1,2,a", "-o", Path.Combine(testDir, "bad-weave.pdf")).RequireFailure("--mapping must be");
        RunTool("stamp", "-i", testPdf, "--text", "x", "--font", "big", "-o", Path.Combine(testDir, "bad-stamp.pdf")).RequireFailure("--font must be");
    }, ref passed);

    Console.WriteLine($"PASS pdf smoke ({passed} checks)");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
finally
{
    try { Directory.Delete(testDir, recursive: true); } catch { }
}

CommandResult RunTool(params string[] arguments)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = skillDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    startInfo.ArgumentList.Add("run");
    startInfo.ArgumentList.Add("--file");
    startInfo.ArgumentList.Add(scriptCs);
    startInfo.ArgumentList.Add("--");
    foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

    using var process = Process.Start(startInfo)!;
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return new CommandResult(process.ExitCode, stdout, stderr, arguments);
}

CommandResult RunPy(params string[] arguments)
{
    var startInfo = new ProcessStartInfo("uv")
    {
        WorkingDirectory = skillDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    startInfo.ArgumentList.Add("run");
    startInfo.ArgumentList.Add("--with");
    startInfo.ArgumentList.Add("pymupdf");
    startInfo.ArgumentList.Add("python");
    startInfo.ArgumentList.Add(extractImagesPy);
    foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

    using var process = Process.Start(startInfo)!;
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return new CommandResult(process.ExitCode, stdout, stderr, arguments);
}
void CreatePdf(string path, int pages, string label)
{
    using var writer = new PdfWriter(path);
    using var pdf = new PdfDocument(writer);
    using var document = new Document(pdf);
    for (var i = 1; i <= pages; i++)
    {
        if (i > 1) document.Add(new AreaBreak());
        document.Add(new Paragraph($"{label} {i}"));
    }
}

int PageCount(string path)
{
    using var pdf = new PdfDocument(new PdfReader(path));
    return pdf.GetNumberOfPages();
}

void Pass(string name, Action action, ref int passed)
{
    Console.Write($"{name}... ");
    action();
    passed++;
    Console.WriteLine("PASS");
}

void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

string FindSkillRoot()
{
    var candidates = new[]
    {
        Directory.GetCurrentDirectory(),
        Path.Combine(Directory.GetCurrentDirectory(), "pdf"),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "pdf")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."))
    };

    foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (File.Exists(Path.Combine(candidate, "SKILL.md")) &&
            File.Exists(Path.Combine(candidate, "scripts", "pdf.cs")))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException("Could not locate the pdf skill root.");
}

sealed record CommandResult(int ExitCode, string StdOut, string StdErr, IReadOnlyList<string> Arguments)
{
    public void RequireSuccess()
    {
        if (ExitCode != 0) throw new InvalidOperationException(ToDebugString());
    }

    public void RequireFailure(string expectedError)
    {
        if (ExitCode == 0) throw new InvalidOperationException("Expected failure but command succeeded." + Environment.NewLine + ToDebugString());
        if (!StdErr.Contains(expectedError, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected stderr to contain '{expectedError}'." + Environment.NewLine + ToDebugString());
        }
    }

    public string ToDebugString() =>
        $"ExitCode: {ExitCode}{Environment.NewLine}Args: {string.Join(" ", Arguments)}{Environment.NewLine}STDOUT:{Environment.NewLine}{StdOut}{Environment.NewLine}STDERR:{Environment.NewLine}{StdErr}";
}
