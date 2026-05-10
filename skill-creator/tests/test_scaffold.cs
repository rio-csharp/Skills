#!/usr/bin/env dotnet
using System.Diagnostics;

var tempRoot = Path.Combine(Path.GetTempPath(), "skill-creator-file-tests", Guid.NewGuid().ToString("N"));
try
{
    var skillCreatorRoot = GetSkillCreatorRoot();
    var outputRoot = Path.Combine(tempRoot, "skills");
    var scaffoldScript = Path.Combine(skillCreatorRoot, "scripts", "scaffold.cs");

    var result = RunDotnet(skillCreatorRoot, "run", "--file", scaffoldScript, "--", "Demo Skill", "--path", outputRoot, "--resources", "scripts,references");

    Require(result.ExitCode == 0, result.ToDebugString());
    Require(result.StdOut.Contains("Created skill at", StringComparison.Ordinal), result.ToDebugString());
    var skillMarkdownPath = Path.Combine(outputRoot, "demo-skill", "SKILL.md");
    Require(File.Exists(skillMarkdownPath), "Missing SKILL.md");
    Require(File.Exists(Path.Combine(outputRoot, "demo-skill", "scripts", ".gitkeep")), "Missing scripts/.gitkeep");
    Require(File.Exists(Path.Combine(outputRoot, "demo-skill", "references", ".gitkeep")), "Missing references/.gitkeep");

    var skillMarkdown = File.ReadAllText(skillMarkdownPath);
    Require(skillMarkdown.Contains("## Common Commands Or Procedure", StringComparison.Ordinal), "Missing operational commands section");
    Require(skillMarkdown.Contains("## Safety", StringComparison.Ordinal), "Missing safety section");
    Require(skillMarkdown.Contains("## Validation", StringComparison.Ordinal), "Missing validation section");
    Require(skillMarkdown.Contains("dotnet run --file", StringComparison.Ordinal), "Missing C# file app guidance");
    Require(!skillMarkdown.Contains("TODO", StringComparison.Ordinal), "Scaffold should not emit TODO placeholders");

    Console.WriteLine("PASS test_scaffold");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
finally
{
    if (Directory.Exists(tempRoot))
    {
        Directory.Delete(tempRoot, recursive: true);
    }
}

static string GetSkillCreatorRoot()
{
    var candidates = new[]
    {
        Directory.GetCurrentDirectory(),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."))
    };

    foreach (var candidateRoot in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (File.Exists(Path.Combine(candidateRoot, "tests", "test_scaffold.cs")) &&
            File.Exists(Path.Combine(candidateRoot, "scripts", "scaffold.cs")))
        {
            return candidateRoot;
        }
    }

    throw new InvalidOperationException("Could not locate skill-creator root.");
}

static CommandResult RunDotnet(string workingDirectory, params string[] arguments)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)!;
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return new CommandResult(process.ExitCode, stdout, stderr, arguments);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed record CommandResult(int ExitCode, string StdOut, string StdErr, IReadOnlyList<string> Arguments)
{
    public string ToDebugString() =>
        $"ExitCode: {ExitCode}{Environment.NewLine}Args: {string.Join(" ", Arguments)}{Environment.NewLine}STDOUT:{Environment.NewLine}{StdOut}{Environment.NewLine}STDERR:{Environment.NewLine}{StdErr}";
}
