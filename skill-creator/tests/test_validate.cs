#!/usr/bin/env dotnet
using System.Diagnostics;

var tempRoot = Path.Combine(Path.GetTempPath(), "skill-creator-file-tests", Guid.NewGuid().ToString("N"));
try
{
    var skillCreatorRoot = GetSkillCreatorRoot();
    var skillPath = Path.Combine(tempRoot, "broken-skill");
    Directory.CreateDirectory(skillPath);
    File.WriteAllText(Path.Combine(skillPath, "SKILL.md"), """
        ---
        name: broken-skill
        description: Handle a narrow workflow. Use when the user asks for this specific workflow with local files.
        ---

        # Broken Skill

        Read [references/missing.md](references/missing.md) first.
        """.Replace("\n", Environment.NewLine));

    var validateScript = Path.Combine(skillCreatorRoot, "scripts", "validate.cs");
    var result = RunDotnet(skillCreatorRoot, "run", "--file", validateScript, "--", skillPath);

    Require(result.ExitCode == 1, result.ToDebugString());
    Require((result.StdOut + result.StdErr).Contains("local link target does not exist", StringComparison.Ordinal), result.ToDebugString());

    var thinScriptSkillPath = Path.Combine(tempRoot, "thin-script-skill");
    Directory.CreateDirectory(Path.Combine(thinScriptSkillPath, "scripts"));
    File.WriteAllText(Path.Combine(thinScriptSkillPath, "SKILL.md"), """
        ---
        name: thin-script-skill
        description: Handle a narrow scripted workflow. Use when the user asks to run this specific local helper workflow.
        ---

        # Thin Script Skill

        Use this skill for the workflow.
        """.Replace("\n", Environment.NewLine));
    File.WriteAllText(Path.Combine(thinScriptSkillPath, "scripts", "helper.cs"), """
        #!/usr/bin/env dotnet
        Console.WriteLine("ok");
        """.Replace("\n", Environment.NewLine));

    var warningResult = RunDotnet(skillCreatorRoot, "run", "--file", validateScript, "--", thinScriptSkillPath);
    var warningOutput = warningResult.StdOut + warningResult.StdErr;
    Require(warningResult.ExitCode == 0, warningResult.ToDebugString());
    Require(warningOutput.Contains("dotnet run --file", StringComparison.Ordinal), warningResult.ToDebugString());
    Require(warningOutput.Contains("Validation section", StringComparison.Ordinal), warningResult.ToDebugString());

    Console.WriteLine("PASS test_validate");
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
        if (File.Exists(Path.Combine(candidateRoot, "tests", "test_validate.cs")) &&
            File.Exists(Path.Combine(candidateRoot, "scripts", "validate.cs")))
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
