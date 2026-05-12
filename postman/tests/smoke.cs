#!/usr/bin/env dotnet

using System;
using System.IO;
using System.Threading.Tasks;

var skillPath = args.Length > 0 ? args[0] : Path.Combine("..", "..", "postman");
var scriptPath = Path.Combine(skillPath, "scripts", "postman.cs");
var failures = 0;

Console.WriteLine("Smoke test for postman skill");
Console.WriteLine($"Script: {scriptPath}");
Console.WriteLine();

await Run("helper self-test", async () =>
{
    return await DotnetRun(scriptPath, "self-test", clearApiKey: false) == 0 ? 0 : 1;
});

await Run("missing required arguments fail clearly", async () =>
{
    var result = await DotnetRun(scriptPath, "list", clearApiKey: false);
    return result == 0 ? 1 : 0;
});

Console.WriteLine();
Console.WriteLine(failures == 0 ? "All smoke tests PASSED" : $"{failures} test(s) FAILED");
return failures;

async Task Run(string desc, Func<Task<int>> action)
{
    Console.WriteLine($"Testing: {desc}");
    try
    {
        var result = await action();
        if (result == 0)
        {
            Console.WriteLine("  PASS");
        }
        else
        {
            Console.WriteLine($"  FAIL (exit {result})");
            failures++;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        failures++;
    }
}

async Task<int> DotnetRun(string script, string commandArgs, bool clearApiKey)
{
    var psi = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "dotnet",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    psi.ArgumentList.Add("run");
    psi.ArgumentList.Add("--file");
    psi.ArgumentList.Add(script);
    foreach (var arg in SplitArgs(commandArgs))
    {
        psi.ArgumentList.Add(arg);
    }
    if (clearApiKey)
    {
        psi.Environment["POSTMAN_API_KEY"] = "";
    }

    using var proc = System.Diagnostics.Process.Start(psi);
    if (proc == null) return -1;

    var stdout = proc.StandardOutput.ReadToEndAsync();
    var stderr = proc.StandardError.ReadToEndAsync();
    await proc.WaitForExitAsync();

    Console.Write(await stdout);
    Console.Error.Write(await stderr);
    return proc.ExitCode;
}

string[] SplitArgs(string args)
{
    return args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
