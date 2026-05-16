#!/usr/bin/env dotnet
#:package DocumentFormat.OpenXml@3.3.0

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

var skillDir = FindSkillRoot();
var scriptCs = Path.Combine(skillDir, "scripts", "ppt.cs");
var testDir = Path.Combine(Path.GetTempPath(), "ppt_smoke_" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(testDir);

try
{
    var passed = 0;

    Pass("create plain text", () =>
    {
        var plainPptx = Path.Combine(testDir, "plain.pptx");
        RunTool("create", "--output", plainPptx, "--content", "Hello\nWorld").RequireSuccess();
        Require(File.Exists(plainPptx), "create should write a PPTX file");
    }, ref passed);

    Pass("create from lines", () =>
    {
        var styledPptx = Path.Combine(testDir, "styled.pptx");
        var lines = "H1 Title\nP Normal paragraph.\nB Bold text.\nI Italic text.\nQUOTE A quote.\nBULLET Item 1\nBULLET Item 2\nNUMBER First\nNUMBER Second\nHR\nCODE code block\nTABLE A,B;1,2";
        RunTool("create", "--output", styledPptx, "--content", lines, "--from-lines").RequireSuccess();
        Require(File.Exists(styledPptx), "line-format create should write a PPTX file");

        // Verify structure by reading internal text
        var readOut = RunTool("read", "-i", styledPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("Title", StringComparison.Ordinal), "line-format should include heading text");
        Require(readOut.StdOut.Contains("Bold text.", StringComparison.Ordinal), "line-format should include bold text");
        Require(readOut.StdOut.Contains("Italic text.", StringComparison.Ordinal), "line-format should include italic text");
        Require(readOut.StdOut.Contains("A quote.", StringComparison.Ordinal), "line-format should include quote");
        // Note: read only previews first 5 text snippets, so bullets/numbered may not appear in preview
    }, ref passed);

    Pass("create with theme", () =>
    {
        var modernPptx = Path.Combine(testDir, "modern.pptx");
        RunTool("create", "--output", modernPptx, "--content", "H1 Modern Theme", "--from-lines", "--style", "modern").RequireSuccess();
        Require(File.Exists(modernPptx), "theme create should write a PPTX file");
    }, ref passed);

    Pass("create multi-slide", () =>
    {
        var multiPptx = Path.Combine(testDir, "multi.pptx");
        var lines = "H1 Slide 1\nP Content 1\nNEWSLIDE\nH1 Slide 2\nP Content 2\nNEWSLIDE\nH1 Slide 3\nP Content 3";
        RunTool("create", "--output", multiPptx, "--content", lines, "--from-lines").RequireSuccess();

        var readOut = RunTool("read", "-i", multiPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("slides: 3", StringComparison.Ordinal), "multi-slide should produce 3 slides");
    }, ref passed);

    Pass("read existing pptx", () =>
    {
        var readPptx = Path.Combine(testDir, "read_test.pptx");
        RunTool("create", "--output", readPptx, "--content", "H1 Hello\nP World", "--from-lines").RequireSuccess();
        var readOut = RunTool("read", "-i", readPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("slides: 1", StringComparison.Ordinal), "read should show 1 slide");
        Require(readOut.StdOut.Contains("Hello", StringComparison.Ordinal), "read should show heading text");
    }, ref passed);

    Pass("create missing output fails", () =>
    {
        RunTool("create", "--content", "test").RequireFailure("--output required");
    }, ref passed);

    Pass("invalid command fails", () =>
    {
        RunTool("unknown", "-i", "test.pptx").RequireFailure("Unknown command");
    }, ref passed);

    Pass("create with layout", () =>
    {
        var layoutPptx = Path.Combine(testDir, "layout.pptx");
        var lines = "LAYOUT title-only\nH1 Full Title\nNEWSLIDE\nLAYOUT two-content\nH1 Two Columns\nP Left content";
        RunTool("create", "--output", layoutPptx, "--content", lines, "--from-lines").RequireSuccess();
        Require(File.Exists(layoutPptx), "layout create should write a PPTX file");

        var readOut = RunTool("read", "-i", layoutPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("slides: 2", StringComparison.Ordinal), "layout should produce 2 slides");
        Require(readOut.StdOut.Contains("Full Title", StringComparison.Ordinal), "should include title text");
    }, ref passed);

    Pass("create with background color", () =>
    {
        var bgPptx = Path.Combine(testDir, "bg.pptx");
        var lines = "BGCOLOR #336699\nH1 Blue Background";
        RunTool("create", "--output", bgPptx, "--content", lines, "--from-lines").RequireSuccess();
        Require(File.Exists(bgPptx), "bg create should write a PPTX file");
    }, ref passed);

    Pass("create with shapes", () =>
    {
        var shapePptx = Path.Combine(testDir, "shapes.pptx");
        var lines = "LAYOUT blank\nSHAPE rect 10%,10%,30%,20%\nFILL #FF0000\nSTROKE #000000,2\nSHAPE ellipse 50%,10%,20%,20%\nFILL #00FF00\nSHAPE line 10%,50%,40%,50%\nSTROKE #0000FF,3\nSHAPE arrow 50%,50%,80%,50%\nSTROKE #0000FF,3";
        RunTool("create", "--output", shapePptx, "--content", lines, "--from-lines").RequireSuccess();
        Require(File.Exists(shapePptx), "shape create should write a PPTX file");
    }, ref passed);

    Pass("modify existing pptx", () =>
    {
        var basePptx = Path.Combine(testDir, "modify_base.pptx");
        var outPptx = Path.Combine(testDir, "modify_out.pptx");
        RunTool("create", "--output", basePptx, "--content", "H1 Original\nP Content", "--from-lines").RequireSuccess();
        RunTool("modify", "-i", basePptx, "-o", outPptx, "--content", "NEWSLIDE\nH1 Appended\nP New content", "--from-lines").RequireSuccess();

        var readOut = RunTool("read", "-i", outPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("slides: 2", StringComparison.Ordinal), "modify should produce 2 slides");
        Require(readOut.StdOut.Contains("Appended", StringComparison.Ordinal), "should include appended text");
    }, ref passed);

    Pass("merge presentations", () =>
    {
        var aPptx = Path.Combine(testDir, "merge_a.pptx");
        var bPptx = Path.Combine(testDir, "merge_b.pptx");
        var outPptx = Path.Combine(testDir, "merge_out.pptx");
        RunTool("create", "--output", aPptx, "--content", "H1 A", "--from-lines").RequireSuccess();
        RunTool("create", "--output", bPptx, "--content", "H1 B", "--from-lines").RequireSuccess();
        RunTool("merge", "-i", aPptx, bPptx, "-o", outPptx).RequireSuccess();

        var readOut = RunTool("read", "-i", outPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("slides: 2", StringComparison.Ordinal), "merge should produce 2 slides");
    }, ref passed);

    Pass("remove slides", () =>
    {
        var basePptx = Path.Combine(testDir, "remove_base.pptx");
        var outPptx = Path.Combine(testDir, "remove_out.pptx");
        var lines = "H1 Slide 1\nNEWSLIDE\nH1 Slide 2\nNEWSLIDE\nH1 Slide 3";
        RunTool("create", "--output", basePptx, "--content", lines, "--from-lines").RequireSuccess();
        RunTool("remove", "-i", basePptx, "-o", outPptx, "--range", "2").RequireSuccess();

        var readOut = RunTool("read", "-i", outPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("slides: 2", StringComparison.Ordinal), "remove should produce 2 slides");
    }, ref passed);

    Pass("reorder slides", () =>
    {
        var basePptx = Path.Combine(testDir, "reorder_base.pptx");
        var outPptx = Path.Combine(testDir, "reorder_out.pptx");
        var lines = "H1 Slide 1\nNEWSLIDE\nH1 Slide 2\nNEWSLIDE\nH1 Slide 3";
        RunTool("create", "--output", basePptx, "--content", lines, "--from-lines").RequireSuccess();
        RunTool("reorder", "-i", basePptx, "-o", outPptx, "--order", "3,2,1").RequireSuccess();

        var readOut = RunTool("read", "-i", outPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("slides: 3", StringComparison.Ordinal), "reorder should preserve 3 slides");
    }, ref passed);

    Pass("add and extract notes", () =>
    {
        var basePptx = Path.Combine(testDir, "notes_base.pptx");
        var outPptx = Path.Combine(testDir, "notes_out.pptx");
        var notesTxt = Path.Combine(testDir, "notes.txt");
        RunTool("create", "--output", basePptx, "--content", "H1 Hello", "--from-lines").RequireSuccess();
        RunTool("notes", "-i", basePptx, "-o", outPptx, "--slide", "1", "--content", "Speaker note here").RequireSuccess();
        RunTool("extract-notes", "-i", outPptx, "-o", notesTxt).RequireSuccess();
        Require(File.Exists(notesTxt), "extract-notes should write a text file");
        var content = File.ReadAllText(notesTxt);
        Require(content.Contains("Speaker note here", StringComparison.Ordinal), "notes should contain the note text");
    }, ref passed);

    Pass("set properties", () =>
    {
        var basePptx = Path.Combine(testDir, "props_base.pptx");
        var outPptx = Path.Combine(testDir, "props_out.pptx");
        RunTool("create", "--output", basePptx, "--content", "H1 Hello", "--from-lines").RequireSuccess();
        RunTool("set-properties", "-i", basePptx, "-o", outPptx, "--title", "My Title", "--author", "Me").RequireSuccess();
        Require(File.Exists(outPptx), "set-properties should write a PPTX file");
    }, ref passed);

    Pass("create with chart", () =>
    {
        var chartPptx = Path.Combine(testDir, "chart.pptx");
        var lines = "H1 Sales\nCHART bar Sales;Q1,Q2,Q3,Q4;10,20,30,40\nNEWSLIDE\nH1 Share\nCHART pie Share;A,B,C;40,35,25\nNEWSLIDE\nH1 Trend\nCHART line Trend;Jan,Feb,Mar;5,10,15\nNEWSLIDE\nH1 Growth\nCHART area Growth;X,Y,Z;1,2,3";
        RunTool("create", "--output", chartPptx, "--content", lines, "--from-lines").RequireSuccess();
        Require(File.Exists(chartPptx), "chart create should write a PPTX file");

        var readOut = RunTool("read", "-i", chartPptx);
        readOut.RequireSuccess();
        Require(readOut.StdOut.Contains("slides: 4", StringComparison.Ordinal), "chart should produce 4 slides");
    }, ref passed);

    Pass("create with animation and transition", () =>
    {
        var animPptx = Path.Combine(testDir, "anim.pptx");
        var lines = "H1 Slide 1\nP Content\nANIMATE fade\nTRANSITION push";
        RunTool("create", "--output", animPptx, "--content", lines, "--from-lines").RequireSuccess();
        Require(File.Exists(animPptx), "animation create should write a PPTX file");
    }, ref passed);

    Console.WriteLine($"PASS ppt smoke ({passed} checks)");
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
        Path.Combine(Directory.GetCurrentDirectory(), "ppt"),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ppt")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."))
    };

    foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (File.Exists(Path.Combine(candidate, "SKILL.md")) &&
            File.Exists(Path.Combine(candidate, "scripts", "ppt.cs")))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException("Could not locate the ppt skill root.");
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
