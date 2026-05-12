#!/usr/bin/env dotnet

using System;
using System.Diagnostics;
using System.IO;

var skillRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : FindSkillRoot();
var scriptPath = Path.Combine(skillRoot, "scripts", "docx.cs");
var testDir = Path.Combine(Path.GetTempPath(), "docx_smoke_" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(testDir);

try
{
    var docxPath = Path.Combine(testDir, "created.docx");
    var txtPath = Path.Combine(testDir, "created.txt");
    var htmlPath = Path.Combine(testDir, "created.html");

    RunTool("create", "--output", docxPath, "--content", "Hello\nWorld").RequireSuccess();
    Require(File.Exists(docxPath), "create should write a DOCX file");

    var read = RunTool("read", docxPath);
    read.RequireSuccess();
    Require(read.StdOut.Contains("Hello", StringComparison.Ordinal), "read should include document text");

    RunTool("modify", docxPath, "--content", "Appended").RequireSuccess();
    var reread = RunTool("read", docxPath);
    reread.RequireSuccess();
    Require(reread.StdOut.Contains("Appended", StringComparison.Ordinal), "modify should append text");

    RunTool("convert", docxPath, "--format", "txt", "--output", txtPath).RequireSuccess();
    Require(File.ReadAllText(txtPath).Contains("Hello", StringComparison.Ordinal), "txt conversion should include content");

    RunTool("convert", docxPath, "--format", "html", "--output", htmlPath).RequireSuccess();
    Require(File.ReadAllText(htmlPath).Contains("<!DOCTYPE html>", StringComparison.Ordinal), "html conversion should write HTML");

    RunTool("create", docxPath, "--content", "bad").RequireFailure("--output required");

    Console.WriteLine("PASS docx smoke");
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

CommandResult RunTool(params string[] toolArgs)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = skillRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    startInfo.ArgumentList.Add("run");
    startInfo.ArgumentList.Add("--file");
    startInfo.ArgumentList.Add(scriptPath);
    startInfo.ArgumentList.Add("--");
    foreach (var arg in toolArgs) startInfo.ArgumentList.Add(arg);

    using var process = Process.Start(startInfo)!;
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return new CommandResult(process.ExitCode, stdout, stderr, toolArgs);
}

static string FindSkillRoot()
{
    var candidates = new[]
    {
        Directory.GetCurrentDirectory(),
        Path.Combine(Directory.GetCurrentDirectory(), "docx"),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "docx")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."))
    };

    foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (File.Exists(Path.Combine(candidate, "SKILL.md")) &&
            File.Exists(Path.Combine(candidate, "scripts", "docx.cs")))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException("Could not locate the docx skill root.");
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
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
