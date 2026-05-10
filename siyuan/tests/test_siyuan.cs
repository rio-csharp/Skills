#!/usr/bin/env dotnet
#pragma warning disable IL2026, IL3050

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var skillRoot = FindSkillRoot();
var scriptPath = Path.Combine(skillRoot, "scripts", "siyuan.cs");
var baseUrl = Environment.GetEnvironmentVariable("SIYUAN_URL") ?? "http://127.0.0.1:6806";
var token = Environment.GetEnvironmentVariable("SIYUAN_TOKEN") ?? "";

using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);

var createdNotebookId = "";
var createdDocId = "";

try
{
    var help = RunDotnet(skillRoot, ["run", "--file", scriptPath, "--", "help"], null);
    Require(help.ExitCode == 0, help.ToDebugString());
    Require(help.StdOut.Contains("SiYuan CLI helper", StringComparison.Ordinal), help.ToDebugString());
    Require(help.StdOut.Contains("Usage: dotnet run --file", StringComparison.Ordinal), help.ToDebugString());

    var commandHelp = RunDotnet(skillRoot, ["run", "--file", scriptPath, "--", "help", "search"], null);
    Require(commandHelp.ExitCode == 0, commandHelp.ToDebugString());
    Require(commandHelp.StdOut.Contains("search", StringComparison.Ordinal), commandHelp.ToDebugString());

    var unknown = RunDotnet(skillRoot, ["run", "--file", scriptPath, "--", "does-not-exist"], null);
    Require(unknown.ExitCode == 1, unknown.ToDebugString());
    Require(unknown.StdErr.Contains("Unknown command", StringComparison.Ordinal), unknown.ToDebugString());

    if (!await CanReachSiyuan())
    {
        Console.WriteLine("SKIP: live CLI payload checks skipped because SiYuan is not reachable or SIYUAN_TOKEN is not accepted.");
        Console.WriteLine("PASS test_siyuan");
        return 0;
    }

    var notebookName = $"siyuan-smoke-{DateTime.Now:yyyyMMddHHmmss}";
    var createNotebook = RunTool("create-nb", notebookName);
    Require(createNotebook.ExitCode == 0, $"create-nb failed: {createNotebook.ToDebugString()}");

    var notebooks = await Api("/notebook/lsNotebooks");
    createdNotebookId = notebooks.GetProperty("notebooks").EnumerateArray()
        .First(nb => nb.GetProperty("name").GetString() == notebookName)
        .GetProperty("id").GetString() ?? "";
    Require(createdNotebookId.Length > 0, "smoke notebook was not discoverable after create-nb");

    var markdown = "# Smoke Doc\n\nPayload check content.";
    var createDoc = RunToolWithInput(markdown, "create", createdNotebookId, "Smoke Doc");
    Require(createDoc.ExitCode == 0, $"create failed: {createDoc.ToDebugString()}");

    var docs = await Api("/query/sql", new { stmt = $"SELECT id FROM blocks WHERE type='d' AND box='{createdNotebookId}' AND content='Smoke Doc' LIMIT 1" });
    createdDocId = docs.EnumerateArray().FirstOrDefault().ValueKind == JsonValueKind.Object
        ? docs.EnumerateArray().First().GetProperty("id").GetString() ?? ""
        : "";
    Require(createdDocId.Length > 0, "smoke document was not discoverable after create");

    // ls: no body -> {}
    var ls = RunTool("ls");
    Require(ls.ExitCode == 0, $"ls failed: {ls.ToDebugString()}");
    Require(ls.StdOut.Contains(notebookName, StringComparison.Ordinal), $"ls output: {ls.ToDebugString()}");

    // info: anonymous { id }
    var info = RunTool("info", createdDocId);
    Require(info.ExitCode == 0, $"info failed: {info.ToDebugString()}");
    Require(info.StdOut.Contains("Smoke Doc", StringComparison.Ordinal), $"info output: {info.ToDebugString()}");

    // search: anonymous { query, method, page, pageSize, orderBy, groupBy }
    var search = RunTool("search", "Payload");
    Require(search.ExitCode == 0, $"search failed: {search.ToDebugString()}");

    // sql: anonymous { stmt }
    var sql = RunTool("sql", $"SELECT id FROM blocks WHERE id='{createdDocId}' LIMIT 1");
    Require(sql.ExitCode == 0, $"sql failed: {sql.ToDebugString()}");
    Require(sql.StdOut.Contains(createdDocId, StringComparison.Ordinal), $"sql output: {sql.ToDebugString()}");

    // create: Dictionary<string, object>
    Require(createDoc.StdOut.Contains("Created:", StringComparison.Ordinal), $"create output: {createDoc.ToDebugString()}");

    // set-attrs: anonymous { id, attrs = Dictionary<string,string> }
    var setAttrs = RunTool("set-attrs", createdDocId, "smoke-attr=true");
    Require(setAttrs.ExitCode == 0, $"set-attrs failed: {setAttrs.ToDebugString()}");

    Console.WriteLine("PASS test_siyuan");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
finally
{
    if (createdDocId.Length > 0)
    {
        _ = TryApi("/filetree/removeDocByID", new { id = createdDocId }).GetAwaiter().GetResult();
    }

    if (createdNotebookId.Length > 0)
    {
        _ = TryApi("/notebook/closeNotebook", new { notebook = createdNotebookId }).GetAwaiter().GetResult();
        _ = TryApi("/notebook/removeNotebook", new { notebook = createdNotebookId }).GetAwaiter().GetResult();
    }
}

CommandResult RunTool(params string[] toolArgs) =>
    RunDotnet(skillRoot, ["run", "--file", scriptPath, "--", .. toolArgs], null);

CommandResult RunToolWithInput(string stdin, params string[] toolArgs) =>
    RunDotnet(skillRoot, ["run", "--file", scriptPath, "--", .. toolArgs], stdin);

async Task<bool> CanReachSiyuan()
{
    try
    {
        _ = await Api("/notebook/lsNotebooks");
        return true;
    }
    catch
    {
        return false;
    }
}

async Task<JsonElement> Api(string endpoint, object? body = null)
{
    var json = body is null ? "{}" : JsonSerializer.Serialize(body);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await http.PostAsync($"/api{endpoint}", content);
    var responseBody = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
    if (!result.TryGetProperty("code", out var codeElement))
    {
        throw new InvalidOperationException($"Unexpected SiYuan response from {endpoint}: {responseBody}");
    }

    var code = codeElement.GetInt32();
    if (code != 0)
    {
        var message = result.TryGetProperty("msg", out var msg) ? msg.GetString() : "unknown";
        throw new HttpRequestException($"API error [{code}] from {endpoint}: {message}");
    }

    return result.GetProperty("data");
}

async Task<bool> TryApi(string endpoint, object body)
{
    try
    {
        _ = await Api(endpoint, body);
        return true;
    }
    catch
    {
        return false;
    }
}

static string FindSkillRoot()
{
    var candidates = new[]
    {
        Directory.GetCurrentDirectory(),
        Path.Combine(Directory.GetCurrentDirectory(), "siyuan"),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "siyuan")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."))
    };

    foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (File.Exists(Path.Combine(candidate, "SKILL.md")) &&
            File.Exists(Path.Combine(candidate, "scripts", "siyuan.cs")))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException("Could not locate the siyuan skill root.");
}

static CommandResult RunDotnet(string workingDirectory, IReadOnlyList<string> arguments, string? stdin)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = stdin is not null,
        UseShellExecute = false
    };

    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)!;
    if (stdin is not null)
    {
        process.StandardInput.Write(stdin);
        process.StandardInput.Close();
    }

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
