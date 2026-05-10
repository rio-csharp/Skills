#!/usr/bin/env dotnet
using System.Diagnostics;
using System.IO.Compression;

var tempRoot = Path.Combine(Path.GetTempPath(), "skill-creator-file-tests", Guid.NewGuid().ToString("N"));
try
{
    var skillCreatorRoot = GetSkillCreatorRoot();
    var skillPath = Path.Combine(tempRoot, "archive-skill");
    Directory.CreateDirectory(skillPath);
    File.WriteAllText(Path.Combine(skillPath, "SKILL.md"), """
        ---
        name: archive-skill
        description: Package a validated skill. Use when the user asks to create a distributable skill archive.
        ---

        # Archive Skill
        """.Replace("\n", Environment.NewLine));
    Directory.CreateDirectory(Path.Combine(skillPath, "__pycache__"));
    File.WriteAllText(Path.Combine(skillPath, "__pycache__", "ignored.pyc"), "cache");

    var packageScript = Path.Combine(skillCreatorRoot, "scripts", "package.cs");
    var outputRoot = Path.Combine(tempRoot, "dist");
    var result = RunDotnet(skillCreatorRoot, "run", "--file", packageScript, "--", skillPath, outputRoot);

    Require(result.ExitCode == 0, result.ToDebugString());
    var archivePath = Path.Combine(outputRoot, "archive-skill.skill");
    Require(File.Exists(archivePath), "Package output not found.");

    using var archive = ZipFile.OpenRead(archivePath);
    Require(archive.Entries.Any(entry => entry.FullName == "archive-skill/SKILL.md"), "Archive missing SKILL.md");
    Require(!archive.Entries.Any(entry => entry.FullName.Contains("__pycache__", StringComparison.Ordinal)), "Archive should exclude __pycache__");

    Console.WriteLine("PASS test_package");
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
        if (File.Exists(Path.Combine(candidateRoot, "tests", "test_package.cs")) &&
            File.Exists(Path.Combine(candidateRoot, "scripts", "package.cs")))
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
