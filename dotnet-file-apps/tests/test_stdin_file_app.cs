#!/usr/bin/env dotnet
using System.Diagnostics;

var tempRoot = Path.Combine(Path.GetTempPath(), "dotnet-file-app-tests", Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(tempRoot);
    var result = RunDotnetWithInput(tempRoot, "Console.WriteLine(\"stdin file app\");", "run", "-");

    Require(result.ExitCode == 0, result.ToDebugString());
    Require(result.StdOut.Contains("stdin file app", StringComparison.Ordinal), result.ToDebugString());

    Console.WriteLine("PASS test_stdin_file_app");
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

static CommandResult RunDotnetWithInput(string workingDirectory, string stdin, params string[] arguments)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)!;
    process.StandardInput.Write(stdin);
    process.StandardInput.Close();
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
