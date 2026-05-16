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
