#!/usr/bin/env dotnet
using System.Diagnostics;

var tempRoot = Path.Combine(Path.GetTempPath(), "dotnet-file-app-tests", Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(tempRoot);
    var scriptPath = Path.Combine(tempRoot, "hello.cs");
    File.WriteAllText(scriptPath, """
        Console.WriteLine("hello from file app");
        """.Replace("\n", Environment.NewLine));

    var result = RunDotnet(tempRoot, "run", "--file", scriptPath);
    Require(result.ExitCode == 0, result.ToDebugString());
    Require(result.StdOut.Contains("hello from file app", StringComparison.Ordinal), result.ToDebugString());

    Console.WriteLine("PASS test_run_file_app");
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
