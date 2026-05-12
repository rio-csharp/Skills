#pragma warning disable IL2026, IL3050

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(string))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
partial class TestJsonContext : JsonSerializerContext { }

static class Program
{
    static int _passed = 0;
    static int _skipped = 0;
    static string _createdNotebookId = "";
    static string _duplicatedDocId = "";

    static async Task<int> Main(string[] args)
    {
        var requireLive = args.Contains("--require-live");
        var noCleanup = args.Contains("--no-cleanup");
        var skillRoot = FindSkillRoot();
        var scriptPath = Path.Combine(skillRoot, "scripts", "siyuan.cs");
        var baseUrl = Environment.GetEnvironmentVariable("SIYUAN_URL") ?? "http://127.0.0.1:6806";
        var token = Environment.GetEnvironmentVariable("SIYUAN_TOKEN") ?? "";

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);

        try
        {
            Console.WriteLine("SiYuan live integration test");
            Console.WriteLine($"Target: {baseUrl}");

            if (!await CanReachSiyuan(http))
            {
                var message = "SiYuan is not reachable or SIYUAN_TOKEN is not accepted; skipping live integration test.";
                if (requireLive)
                {
                    throw new InvalidOperationException(message);
                }

                Console.WriteLine($"SKIP: {message}");
                return 0;
            }

            var notebookName = $"siyuan-skill-test-{DateTime.Now:yyyyMMddHHmmss}";
            Section("Notebook commands");

            var createNotebook = RunTool("create-nb", notebookName);
            Require(createNotebook.ExitCode == 0, "create-nb exits 0", createNotebook.ToDebugString());

            var notebooks = await Api(http, "/notebook/lsNotebooks");
            _createdNotebookId = notebooks.GetProperty("notebooks").EnumerateArray()
                .First(nb => nb.GetProperty("name").GetString() == notebookName)
                .GetProperty("id").GetString() ?? "";
            Require(_createdNotebookId.Length > 0, "created notebook appears in API list");

            var list = RunTool("ls");
            Require(list.StdOut.Contains(notebookName, StringComparison.Ordinal), "ls shows created notebook", list.ToDebugString());

            var renamedNotebook = notebookName + "-renamed";
            Require(RunTool("rename-nb", _createdNotebookId, renamedNotebook).ExitCode == 0, "rename-nb exits 0");
            notebookName = renamedNotebook;

            var conf = RunTool("get-nb-conf", _createdNotebookId);
            Require(conf.ExitCode == 0 && conf.StdOut.Contains(renamedNotebook, StringComparison.Ordinal), "get-nb-conf shows renamed notebook", conf.ToDebugString());

            Require(RunTool("close-nb", _createdNotebookId).ExitCode == 0, "close-nb exits 0");
            Require(RunTool("open-nb", _createdNotebookId).ExitCode == 0, "open-nb exits 0");

            Section("Document commands");

            var markdown = "# Test Doc\n\nOriginal content.\n\n## Section\n\nMore content.";
            var createDoc = RunToolWithInput(markdown, "create", _createdNotebookId, "Test Doc");
            Require(createDoc.ExitCode == 0, "create exits 0", createDoc.ToDebugString());

            var docId = FindDocIdFromListing(_createdNotebookId, "Test Doc");
            if (docId.Length == 0)
            {
                docId = await WaitForDocId(http, _createdNotebookId, "Test Doc");
            }
            Require(docId.Length > 0, "created doc appears in notebook listing or blocks table");

            Require(RunTool("cat", docId).StdOut.Contains("Original content", StringComparison.Ordinal), "cat returns markdown content");
            Require(RunTool("info", docId).StdOut.Contains("Test Doc", StringComparison.Ordinal), "info returns doc metadata");
            Require(RunTool("get-doc", docId).StdOut.Contains("blockCount", StringComparison.Ordinal), "get-doc returns document JSON");
            Require(RunTool("outline", docId).ExitCode == 0, "outline exits 0");
            Require(RunTool("path", docId).StdOut.Contains("Test Doc", StringComparison.Ordinal), "path returns hpath");
            Require(RunTool("full-path", docId).StdOut.Contains("Test Doc", StringComparison.Ordinal), "full-path returns hpath");
            Require(RunTool("breadcrumb", docId).ExitCode == 0, "breadcrumb exits 0");
            Require(RunTool("tree", _createdNotebookId).StdOut.Contains("Test Doc", StringComparison.Ordinal), "tree shows created doc");
            Require(RunTool("docs", _createdNotebookId).StdOut.Contains("Test Doc", StringComparison.Ordinal), "docs shows created doc");
            Require(RunTool("search", "Original").ExitCode == 0, "search exits 0");
            Require(RunTool("search-docs", "Test").ExitCode == 0, "search-docs exits 0");

            var updatedMarkdown = "# Updated Doc\n\nUpdated content.\n\n## New Section\n\nMore content.";
            Require(RunToolWithInput(updatedMarkdown, "update-md", docId).ExitCode == 0, "update-md exits 0");
            Require(RunTool("cat", docId).StdOut.Contains("Updated content", StringComparison.Ordinal), "cat sees updated content");

            Require(RunTool("rename", docId, "Renamed Doc").ExitCode == 0, "rename exits 0");
            Require(RunTool("cat", docId).StdOut.Contains("Renamed Doc", StringComparison.Ordinal), "cat sees renamed title");

            var duplicate = RunTool("duplicate", docId);
            Require(duplicate.ExitCode == 0, "duplicate exits 0", duplicate.ToDebugString());
            var duplicateRows = await Api(http, "/query/sql", new { stmt = $"SELECT id FROM blocks WHERE type='d' AND box='{_createdNotebookId}' AND id <> '{docId}' LIMIT 1" });
            _duplicatedDocId = FirstStringProperty(duplicateRows, "id");
            if (_duplicatedDocId.Length > 0)
            {
                Require(RunTool("rm", _duplicatedDocId).ExitCode == 0, "rm removes duplicated doc");
                _duplicatedDocId = "";
            }
            else
            {
                Skip("rm duplicated doc", "duplicate ID was not discoverable");
            }

            Section("Block and attribute commands");

            var insert = RunTool("insert-block", docId, "markdown", "Inserted block");
            Require(insert.ExitCode == 0, "insert-block exits 0", insert.ToDebugString());

            var blockRows = await Api(http, "/query/sql", new { stmt = $"SELECT id FROM blocks WHERE root_id='{docId}' AND content LIKE '%Inserted block%' LIMIT 1" });
            var insertedBlockId = FirstStringProperty(blockRows, "id");
            if (insertedBlockId.Length > 0)
            {
                Require(RunTool("move-block", insertedBlockId, "--parent", docId).ExitCode == 0, "move-block exits 0");
            }
            else
            {
                Skip("move-block", "inserted block ID was not discoverable");
            }

            Require(RunTool("attrs", docId).ExitCode == 0, "attrs exits 0");
            Require(RunTool("set-attrs", docId, "custom-skill-test=true").ExitCode == 0, "set-attrs exits 0");
            Require(RunTool("set-attrs-batch", "custom-batch-test=true", "--where", $"root_id='{docId}' AND type='p'").ExitCode == 0, "set-attrs-batch exits 0");

            Section("Read-only utility commands");

            Require(RunTool("tags").ExitCode == 0, "tags exits 0");
            Require(RunTool("recent").ExitCode == 0, "recent exits 0");
            Require(RunTool("templates").ExitCode == 0, "templates exits 0");
            Require(RunTool("backlinks", docId).ExitCode == 0, "backlinks exits 0");
            // history: SiYuan getDocHistoryContent requires historyPath field; when no history exists for a doc it returns error
            Require(RunTool("sql", $"SELECT id, content FROM blocks WHERE id='{docId}' LIMIT 1").StdOut.Contains(docId, StringComparison.Ordinal), "sql returns expected doc");
            Require(RunTool("raw", "/query/sql", $$"""{"stmt":"SELECT id FROM blocks WHERE id='{{docId}}' LIMIT 1"}""").StdOut.Contains(docId, StringComparison.Ordinal), "raw can call query/sql");
            Require(RunTool("export", docId, "md").ExitCode == 0, "export md exits 0");

            Console.WriteLine();
            Console.WriteLine($"PASS test_siyuan_integration: {_passed} passed, {_skipped} skipped");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        finally
        {
            if (!noCleanup)
            {
                if (_duplicatedDocId.Length > 0)
                {
                    _ = TryApi(http, "/filetree/removeDocByID", new { id = _duplicatedDocId }).GetAwaiter().GetResult();
                }

                if (_createdNotebookId.Length > 0)
                {
                    _ = TryApi(http, "/notebook/closeNotebook", new { notebook = _createdNotebookId }).GetAwaiter().GetResult();
                    _ = TryApi(http, "/notebook/removeNotebook", new { notebook = _createdNotebookId }).GetAwaiter().GetResult();
                    Console.WriteLine($"Cleanup: removed notebook {_createdNotebookId}");
                }
            }
        }
    }

    static async Task<bool> CanReachSiyuan(HttpClient http)
    {
        try
        {
            _ = await Api(http, "/notebook/lsNotebooks");
            return true;
        }
        catch
        {
            return false;
        }
    }

    static CommandResult RunTool(params string[] toolArgs) =>
        RunDotnet(FindSkillRoot(), ["run", "--file", Path.Combine(FindSkillRoot(), "scripts", "siyuan.cs"), "--", .. toolArgs], null);

    static CommandResult RunToolWithInput(string stdin, params string[] toolArgs) =>
        RunDotnet(FindSkillRoot(), ["run", "--file", Path.Combine(FindSkillRoot(), "scripts", "siyuan.cs"), "--", .. toolArgs], stdin);

static string SerializeAnonymous(object? body)
    {
        if (body is null) return "{}";
        if (body is JsonElement je) return je.GetRawText();
        if (body is string s) return s;
        if (body is System.Collections.IDictionary dict)
        {
            var pairs = new List<string>();
            foreach (System.Collections.DictionaryEntry e in dict)
                pairs.Add($"\"{e.Key}\":{SerializeValue(e.Value)}");
            return "{" + string.Join(",", pairs) + "}";
        }
        var type = body.GetType();
        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var values = new List<string>();
        foreach (var prop in props)
            values.Add($"\"{prop.Name}\":{SerializeValue(prop.GetValue(body))}");
        return "{" + string.Join(",", values) + "}";
    }

    static string SerializeValue(object? value)
    {
        if (value is null) return "null";
        if (value is string s) return $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")}\"";
        if (value is JsonElement je) return je.GetRawText();
        if (value is int or long or bool or double) return value.ToString()?.ToLower() ?? "null";
        if (value is System.Collections.IDictionary nestedDict)
        {
            var pairs = new List<string>();
            foreach (System.Collections.DictionaryEntry e in nestedDict)
                pairs.Add($"\"{e.Key}\":{SerializeValue(e.Value)}");
            return "{" + string.Join(",", pairs) + "}";
        }
        if (value is System.Collections.IEnumerable arr && value.GetType() != typeof(string))
        {
            var items = new List<string>();
            foreach (var item in arr)
                items.Add(SerializeValue(item));
            return "[" + string.Join(",", items) + "]";
        }
        return $"\"{value}\"";
    }

    static async Task<JsonElement> Api(HttpClient http, string endpoint, object? body = null)
    {
        var json = body is null ? "{}" : SerializeAnonymous(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await http.PostAsync($"/api{endpoint}", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize(responseBody, TestJsonContext.Default.JsonElement);
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

    static async Task<bool> TryApi(HttpClient http, string endpoint, object body)
    {
        try
        {
            _ = await Api(http, endpoint, body);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static async Task<string> WaitForDocId(HttpClient http, string notebookId, string title)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var docs = await Api(http, "/query/sql", new { stmt = $"SELECT id, content, hpath FROM blocks WHERE type='d' AND box='{notebookId}' ORDER BY updated DESC LIMIT 20" });
            foreach (var row in docs.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object) continue;
                var contentMatches = row.TryGetProperty("content", out var content) && content.GetString() == title;
                var pathMatches = row.TryGetProperty("hpath", out var hpath) && (hpath.GetString() ?? "").Contains(title, StringComparison.Ordinal);
                if ((contentMatches || pathMatches) && row.TryGetProperty("id", out var id))
                {
                    return id.GetString() ?? "";
                }
            }

            await Task.Delay(500);
        }

        return "";
    }

    static string FirstStringProperty(JsonElement rows, string propertyName)
    {
        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object && row.TryGetProperty(propertyName, out var value))
            {
                return value.GetString() ?? "";
            }
        }

        return "";
    }

    static string FindDocIdFromListing(string notebookId, string title)
    {
        var listed = RunTool("docs", notebookId);
        if (listed.ExitCode != 0) return "";

        foreach (var line in listed.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(title, StringComparison.Ordinal)) continue;
            var idStart = line.LastIndexOf('[');
            var idEnd = line.LastIndexOf(']');
            if (idStart >= 0 && idEnd > idStart)
            {
                var candidate = line[(idStart + 1)..idEnd].Trim();
                if (candidate.Length > 0) return candidate;
            }
        }

        return "";
    }

    static void Section(string name) => Console.WriteLine($"{Environment.NewLine}== {name} ==");

    static void Require(bool condition, string name, string detail = "")
    {
        if (!condition)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail)
                ? $"FAIL: {name}"
                : $"FAIL: {name}{Environment.NewLine}{detail}");
        }

        _passed++;
        Console.WriteLine($"PASS: {name}");
    }

    static void Skip(string name, string reason)
    {
        _skipped++;
        Console.WriteLine($"SKIP: {name} ({reason})");
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

    static CommandResult RunDotnet(string workingDirectory, string[] arguments, string? stdin)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = stdin is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
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

    sealed record CommandResult(int ExitCode, string StdOut, string StdErr, IReadOnlyList<string> Arguments)
    {
        public string ToDebugString() =>
            $"ExitCode: {ExitCode}{Environment.NewLine}Args: dotnet {string.Join(" ", Arguments)}{Environment.NewLine}STDOUT:{Environment.NewLine}{StdOut}{Environment.NewLine}STDERR:{Environment.NewLine}{StdErr}";
    }
}
