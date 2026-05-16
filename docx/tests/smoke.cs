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
    var styledPath = Path.Combine(testDir, "styled.docx");
    var tablePath = Path.Combine(testDir, "table.docx");
    var imagePath = Path.Combine(testDir, "test.png");
    var imageDocPath = Path.Combine(testDir, "image.docx");

    // --- Basic operations ---
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

    // --- Line-format creation ---
    var lines = "H1 Title\nP Normal paragraph.\nB Bold text.\nI Italic text.\nQUOTE A quote.\nBULLET Item 1\nBULLET Item 2\nNUMBER First\nNUMBER Second\nHR\nCODE code block\nTABLE A,B;1,2";
    RunTool("create", "--output", styledPath, "--content", lines, "--from-lines").RequireSuccess();
    Require(File.Exists(styledPath), "line-format create should write a DOCX file");

    var styledRead = RunTool("read", styledPath);
    styledRead.RequireSuccess();
    Require(styledRead.StdOut.Contains("[Heading1]", StringComparison.Ordinal), "line-format should produce Heading1");
    Require(styledRead.StdOut.Contains("Bold text.", StringComparison.Ordinal), "line-format should include bold text");
    Require(styledRead.StdOut.Contains("Italic text.", StringComparison.Ordinal), "line-format should include italic text");
    Require(styledRead.StdOut.Contains("A quote.", StringComparison.Ordinal), "line-format should include quote");
    Require(styledRead.StdOut.Contains("Item 1", StringComparison.Ordinal), "line-format should include bullet");
    Require(styledRead.StdOut.Contains("First", StringComparison.Ordinal), "line-format should include numbered");

    // --- Set properties ---
    RunTool("set-properties", styledPath, "--title", "Test Doc", "--author", "Test Author").RequireSuccess();
    var propRead = RunTool("read", styledPath);
    propRead.RequireSuccess();
    Require(propRead.StdOut.Contains("Test Doc", StringComparison.Ordinal), "set-properties should set title");
    Require(propRead.StdOut.Contains("Test Author", StringComparison.Ordinal), "set-properties should set author");

    // --- Insert table ---
    File.Copy(docxPath, tablePath, true);
    RunTool("insert-table", tablePath, "--rows", "2", "--cols", "2", "--data", "X,Y\n1,2", "--header").RequireSuccess();
    var tableRead = RunTool("read", tablePath);
    tableRead.RequireSuccess();
    Require(tableRead.StdOut.Contains("X", StringComparison.Ordinal), "table should include header cell");
    Require(tableRead.StdOut.Contains("1", StringComparison.Ordinal), "table should include data cell");

    // --- Insert image ---
    CreateTestPng(imagePath, 10, 10);
    File.Copy(docxPath, imageDocPath, true);
    RunTool("insert-image", imageDocPath, "--image", imagePath, "--width", "50", "--height", "50").RequireSuccess();
    var imgRead = RunTool("read", imageDocPath);
    imgRead.RequireSuccess();
    Require(File.Exists(imageDocPath) && new FileInfo(imageDocPath).Length > 0, "image insert should produce valid docx");

    // --- Line-format modify ---
    RunTool("modify", docxPath, "--content", "H2 Modified\nB Strong text.", "--from-lines").RequireSuccess();
    var modRead = RunTool("read", docxPath);
    modRead.RequireSuccess();
    Require(modRead.StdOut.Contains("[Heading2]", StringComparison.Ordinal), "line-format modify should produce Heading2");

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

void CreateTestPng(string path, int width, int height)
{
    var pngHeader = new byte[]
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, (byte)width, 0x00, 0x00, 0x00, (byte)height,
        0x08, 0x02, 0x00, 0x00, 0x00,
        0x90, 0x68, 0xD0, 0x3D,
        0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54,
        0x08, 0xD7, 0x63, 0xF8, 0x0F, 0x00, 0x00, 0x01,
        0x01, 0x00, 0x05, 0x18, 0xD8, 0xB4,
        0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
        0xAE, 0x42, 0x60, 0x82
    };
    File.WriteAllBytes(path, pngHeader);
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
